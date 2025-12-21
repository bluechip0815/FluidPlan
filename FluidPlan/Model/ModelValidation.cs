using FluidPlan.Model;

namespace FluidSimu
{
    public static class ModelValidation
    {
        private static void CheckUnused(HashSet<string> definedElementNames, HashSet<string> usedInConnectionNames)
        {
            // 2. Check for defined elements that are never used in any connection.
            var unusedElementNames = definedElementNames.Except(usedInConnectionNames);
            if (unusedElementNames.Any())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("WARNING: The following elements are defined but not used in any connection:");
                foreach (var unusedName in unusedElementNames)
                {
                    Console.WriteLine($"  - '{unusedName}'");
                }
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine("Model validation successful: All elements are defined and used.");
            }
        }

        public static void ValidateModel(PneumaticModel model, SimulationModelDto dto)
        {
            Console.WriteLine("\nValidating model integrity...");
            HashSet<string> definedElementNames = [.. model.Elements.Select(e => e.Name)];
            HashSet<string> usedInConnectionNames = new HashSet<string>();
            List<string> errors = new List<string>();

            // 1. Check if elements used in connections are actually defined.
            foreach (var connectionEntry in dto.Connections)
            {
                var connectionId = connectionEntry.Key;
                var namesInConnection = connectionEntry.Value;

                if (namesInConnection == null || !namesInConnection.Any())
                {
                    errors.Add($"Connector '{connectionId}' is empty.");
                    continue;
                }

                foreach (var name in namesInConnection)
                {
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    var elementName = name.Split('.')[0].Trim(); // Handle "V1.1" format
                    usedInConnectionNames.Add(elementName); // Track all used elements
                    if (!definedElementNames.Contains(elementName))
                    {
                        errors.Add($"Element '{elementName}' used in connection '{connectionId}' is not defined in the 'elements' list.");
                    }
                }
            }
            ShowErrors(errors);
            CheckUnused(definedElementNames, usedInConnectionNames);
        }
        public static void DumpJunctions(IReadOnlyDictionary<int, Junction> _junctions)
        {
            Console.WriteLine($"\nConnection between elements");
            foreach (var c in _junctions.Keys)
            {
                Junction? con;
                if (_junctions.TryGetValue(c, out con))
                {
                    Console.WriteLine(con.Info());
                }
                else
                    Console.WriteLine("failed!");
            }
        }
        public static void DumpModel(List<IPneumaticElement> _elements, string name)
        {
            Console.WriteLine($"\nCreate elements for model '{name}'");
            foreach (var e in _elements)
                Console.WriteLine(e.ToString());
        }
        private static void ShowErrors(List<string> errors)
        {
            // If there are any errors, report them and stop execution.
            if (errors.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("FATAL: Model validation failed. Halting execution.");
                foreach (var error in errors)
                {
                    Console.WriteLine($"  - {error}");
                }
                Console.ResetColor();
                throw new InvalidOperationException("Model configuration is invalid. Please check the JSON file.");
            }
        }
    }
}