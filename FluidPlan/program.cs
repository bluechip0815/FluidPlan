using FluidPlan.Dto;
using System.Text.Json;

namespace FluidSimu
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Check for Generator Flag
            if (args.Contains("--generate-plc"))
            {
                var modelFile = args.Length > 1 ? args[1] : "model.json";
                var outputFile = args.Length > 2 ? args[2] : "F_InitializeModel.TcPOU";

                try
                {
                    PlcCodeGenerator.Generate(modelFile, outputFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error generating PLC code: {ex.Message}");
                }
                return; // Exit after generation
            }
            // Delegate to the correct mode based on command-line arguments
            if (args.Contains("--interactive"))
            {
                RunInteractiveMode(args);
            }
            else
            {
                RunProfileBasedMode(args);
            }
        }
        public static void RunProfileBasedMode(string[] args)
        {
            var modelPath = args.Length > 0 ? args[0] : "model.json";
            var profilePath = args.Length > 1 ? args[1] : "executionProfile.json";
            var outputPath = "output";
            var logFileName = "simulation_result.csv";

            Console.WriteLine("Loading configuration...");
            var modelDto = ModelLoader.LoadJson<SimulationModelDto>(modelPath);
            var profileDto = ModelLoader.LoadJson<ExecutionProfileDto>(profilePath);
            FlowPhysics.Initialize(profileDto.PhysicsParameters);

            // 1. Create Model
            var model = PneumaticModel.FromDto(modelDto);
            outputPath = Path.Combine(outputPath, FileNameSanitizer.Sanitize(modelDto.ModelName));
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            File.Copy(modelPath, Path.Combine(outputPath, modelPath), true);
            File.Copy(profilePath, Path.Combine(outputPath, profilePath), true);

            SchemaGenerator.CreateSchemaImage(modelDto, outputPath);

            // 2. Inject Profile (Crucial Step: Connect Actuators)
            var profileAdapter = new ProfileAdapter(profileDto);
            // DIRECTLY APPLY PROFILE TO ELEMENTS
            double minRunTime = model.ApplyProfile(profileDto);

            // LOGIC SETUP
            double dt = profileDto.TimeStepSeconds;
            double tolerance = profileDto.SteadyTolerance; // e.g. 0.00001 bar
            // Safety break: Stop after xxx seconds even if not steady (prevents infinite loops)
            double hardTimeLimit = profileDto.HardTimeLimit;
            // Determine Minimum Run Time (script time + buffer)
            Console.WriteLine($"\nStarting Simulation (dt={profileDto.TimeStepSeconds}s, End={hardTimeLimit}s)...");

            // 4. Run with Logging
            using (var logger = new SimulationLogger(Path.Combine(outputPath, logFileName), model.Elements))
            {
                bool isSteady = false;

                // LOOP CONDITION:
                // 1. Always run until we pass the last scheduled event.
                // 2. Once past events, continue running ONLY IF pressure delta > tolerance.
                // 3. Stop if we hit hardTimeLimit.
                model.Reset(profileDto.TimeStepSeconds);
                while ((!isSteady && model.CurrentTime < hardTimeLimit) || model.CurrentTime < minRunTime)
                {
                    model.Execute();
                    logger.LogStep(model.CurrentTime);

                    // Check stability
                    isSteady = model.LastMaxPressureDelta < tolerance;

                    if (isSteady && model.CurrentTime > minRunTime)
                    {
                        Console.WriteLine($"\nSteady state reached at T={model.CurrentTime:F2}s (DeltaP: {model.LastMaxPressureDelta:E2})");
                        break;
                    }
                    // Console feedback
                    if (Math.Round(model.CurrentTime / dt) % 10 == 0)
                        Console.Write($"\rTime: {model.CurrentTime:F4}s | Max dP: {model.LastMaxPressureDelta:F6}");
                }
            }

            Console.WriteLine($"\nSimulation finished. Results saved to {outputPath}");
            ResultVisualizer.CreateCharts(
                Path.Combine(outputPath, logFileName),
                Path.Combine(outputPath, FileNameSanitizer.Sanitize(modelDto.ModelName) + "_chart.png"),
                modelDto);
        }
       
        /// <summary>
        /// NEW: Interactive simulation mode driven by console commands.
        /// </summary>
        public static void RunInteractiveMode(string[] args)
        {
            var modelPath = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "model.json";
            Console.WriteLine("--- FluidSimu Interactive Mode ---");
            Console.WriteLine($"Loading model '{modelPath}'...");

            var modelDto = ModelLoader.LoadJson<SimulationModelDto>(modelPath);
            var model = PneumaticModel.FromDto(modelDto);
            // Set the model to interactive mode
            model.IsInteractive = true;

            // Use a fixed timestep for interactive mode
            double dt = 0.001; // 1ms
            model.Reset(dt);

            var controllables = model.Elements.OfType<IControllable>().ToDictionary(c => ((IPneumaticElement)c).Name, c => c);
            var visibles = model.Elements.Where(e => e.IsVisible).ToList();

            Console.WriteLine("\n--- Simulation Control ---");
            Console.WriteLine("  <ElementName> <Value>   (e.g., 'V1 1' or 'EPU 3.5')");
            Console.WriteLine("  run <steps>             (e.g., 'run 1000' to simulate 1s)");
            Console.WriteLine("  status                  (shows current state of visible elements)");
            Console.WriteLine("  quit                    (to exit)");

            bool keepRunning = true;
            while (keepRunning)
            {
                Console.Write("\n> ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) continue;

                var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var command = parts[0].ToLower();

                switch (command)
                {
                    case "quit":
                        keepRunning = false;
                        break;
                    case "status":
                        PrintStatus(model, visibles);
                        break;
                    case "run":
                        if (parts.Length > 1 && int.TryParse(parts[1], out int steps) && steps > 0)
                        {
                            for (int i = 0; i < steps; i++) model.Execute();
                            Console.WriteLine($"Simulated {steps * dt:F4}s. Current Time: {model.CurrentTime:F4}s");
                            PrintStatus(model, visibles);
                        }
                        else
                            Console.WriteLine("Usage: run <number_of_steps>");
                        break;
                    default:
                        if (parts.Length > 1 && double.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double value))
                        {
                            if (controllables.TryGetValue(parts[0], out var controllable))
                            {
                                controllable.SetControlValue(value);
                                Console.WriteLine($"Set {parts[0]} to {value}.");
                            }
                            else
                                Console.WriteLine($"Error: Element '{parts[0]}' not found or not controllable.");
                        }
                        else
                            Console.WriteLine("Unknown command. Use 'ElementName Value' or a valid command.");
                        break;
                }
            }
            Console.WriteLine("\nInteractive simulation finished.");
        }
        private static void PrintStatus(PneumaticModel model, List<IPneumaticElement> visibles)
        {
            Console.WriteLine($"--- Status at T = {model.CurrentTime:F4}s ---");
            if (!visibles.Any())
            {
                Console.WriteLine("No elements marked as visible. Add '\"visible\": \"true\"' to element parameters in JSON.");
                return;
            }
            foreach (var element in visibles)
            {
                Console.WriteLine($"  {element.Name,-10}: {element.Pressure:F4} bar");
            }
        }
    }
}