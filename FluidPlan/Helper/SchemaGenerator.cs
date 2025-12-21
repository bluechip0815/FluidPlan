// Create a new file: SchemaGenerator.cs
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace FluidSimu
{
    public static class SchemaGenerator
    {
        public static void CreateSchemaImageEx(SimulationModelDto model, string outputFolder, string iconsFolderPath = "schema_icons")
        {

            string safeFileName = FileNameSanitizer.Sanitize(model.ModelName);
            string outputImagePath = Path.Combine(outputFolder, $"{safeFileName}_schema.png");
            // --- END: Use the Sanitizer ---

            Console.WriteLine($"\nGenerating schematic image for '{model.ModelName}'...");

            // --- 1. Generate the DOT language string ---
            var dotBuilder = new StringBuilder();
            dotBuilder.AppendLine("digraph PneumaticSchema {");
            dotBuilder.AppendLine("    rankdir=LR; // Layout from Left to Right");
            dotBuilder.AppendLine("    node [shape=box, fixedsize=true, width=1, height=1, labelloc=b];");

            // Define the nodes with their custom icons
            foreach (var element in model.Elements)
            {
                // Find the icon for the element type
                string iconName = $"{element.Type.ToLower()}.png";
                string iconPath = Path.Combine(iconsFolderPath, iconName);

                if (!File.Exists(iconPath))
                {
                    // Fallback if no icon is found
                    dotBuilder.AppendLine($"    {element.Name} [label=\"{element.Name}\"];");
                }
                else
                {
                    // Use the custom image for the node
                    dotBuilder.AppendLine($"    {element.Name} [label=\"{element.Name}\", image=\"{Path.GetFullPath(iconPath)}\", labelloc=b];");
                }
            }

            // Define the connections (edges)
            foreach (var connectionList in model.Connections.Values)
            {
                var elementNames = connectionList
                    .Select(c => c.Split('.')[0].Trim())
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct()
                    .ToList();

                if (elementNames.Count > 1)
                {
                    // Intelligente Hub-Auswahl
                    var elementsOnNode = model.Elements.Where(e => elementNames.Contains(e.Name)).ToList();

                    // Priorität 1: Finde eine "Senke" (Tank, Exhaust)
                    var sink = elementsOnNode.FirstOrDefault(e =>
                        e.Type.Equals("Tank", StringComparison.OrdinalIgnoreCase) ||
                        e.Type.Equals("Exhaust", StringComparison.OrdinalIgnoreCase));

                    if (sink != null)
                    {
                        // Alle anderen Elemente zeigen auf die Senke
                        foreach (var element in elementsOnNode)
                        {
                            if (element.Name != sink.Name)
                            {
                                dotBuilder.AppendLine($"    {element.Name} -> {sink.Name};");
                            }
                        }
                    }
                    else
                    {
                        // Priorität 2: Finde eine "Quelle" (Supply, Epu)
                        var source = elementsOnNode.FirstOrDefault(e =>
                            e.Type.Equals("Supply", StringComparison.OrdinalIgnoreCase) ||
                            e.Type.Equals("Epu", StringComparison.OrdinalIgnoreCase));

                        if (source != null)
                        {
                            // Die Quelle zeigt auf alle anderen Elemente
                            foreach (var element in elementsOnNode)
                            {
                                if (element.Name != source.Name)
                                {
                                    dotBuilder.AppendLine($"    {source.Name} -> {element.Name};");
                                }
                            }
                        }
                        else
                        {
                            // Standard-Fallback: Erster Knoten ist der Hub
                            string hubElement = elementNames[0];
                            for (int i = 1; i < elementNames.Count; i++)
                            {
                                dotBuilder.AppendLine($"    {hubElement} -> {elementNames[i]};");
                            }
                        }
                    }
                }
            }

            dotBuilder.AppendLine("}");

            string dotString = dotBuilder.ToString();
            string dotFilePath = Path.ChangeExtension(outputImagePath, ".dot");
            File.WriteAllText(dotFilePath, dotString);

            // --- 2. Call the Graphviz 'dot' command-line tool ---
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "dot.exe";
                    // -Tpng specifies the output format
                    // -o specifies the output file path
                    process.StartInfo.Arguments = $"-Tpng \"{dotFilePath}\" -o \"{outputImagePath}\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardError = true;

                    process.Start();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        Console.WriteLine($"Schematic successfully saved to: {outputImagePath}");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("ERROR: Graphviz 'dot.exe' failed.");
                        Console.WriteLine("Please ensure Graphviz is installed and 'dot.exe' is in your system's PATH.");
                        Console.WriteLine($"Graphviz error output: {error}");
                        Console.ResetColor();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: Could not execute 'dot.exe'.");
                Console.WriteLine("Please ensure Graphviz is installed and in your system's PATH.");
                Console.WriteLine($"Exception: {ex.Message}");
                Console.ResetColor();
            }
        }
        public static void CreateSchemaImage(SimulationModelDto modelDto, string outputFolder, string iconsFolderPath = "schema_icons")
        {

            string safeFileName = FileNameSanitizer.Sanitize(modelDto.ModelName);
            string outputImagePath = Path.Combine(outputFolder, $"{safeFileName}_schema.png");

            Console.WriteLine($"\nGenerating schematic image for '{modelDto.ModelName}'...");

            // --- 1. Generate the DOT language string ---
            // --- Graphen-Topologie analysieren ---
            var nodeDistances = CalculateNodeDistances(modelDto);

            // --- Schritt 2: Generate the DOT language string ---
            var dotBuilder = new StringBuilder();
            dotBuilder.AppendLine("digraph PneumaticSchema {");
            dotBuilder.AppendLine("    rankdir=LR;");
            dotBuilder.AppendLine("    node [shape=box, fixedsize=true, width=1, height=1, labelloc=b];");

            // Define nodes with custom icons
            foreach (var element in modelDto.Elements)
            {
                string iconName = $"{element.Type.ToLower()}.png";

                // --- HIER IST DIE INTELLIGENZ FÜR DAS CHECKVALVE ---
                if (element.Type.Equals("CheckValve", StringComparison.OrdinalIgnoreCase))
                {
                    // Finde die Junctions, an die dieses Ventil angeschlossen ist
                    var (j1, j2) = FindJunctionsForElement(modelDto, element.Name);

                    // Hole die berechneten "Entfernungen" von der Quelle
                    nodeDistances.TryGetValue(j1, out int dist1);
                    nodeDistances.TryGetValue(j2, out int dist2);

                    // Wenn Port 2 "näher" an der Quelle ist als Port 1, ist es umgekehrt eingebaut.
                    if (j2 != -1 && j1 != -1 && dist2 < dist1)
                    {
                        iconName = "checkvalve_reversed.png";
                    }
                }

                // Define the nodes with their custom icons
                // Find the icon for the element type
                string iconPath = Path.Combine(iconsFolderPath, iconName);
                if (!File.Exists(iconPath))
                {
                    // Fallback if no icon is found
                    dotBuilder.AppendLine($"    {element.Name} [label=\"{element.Name}\"];");
                }
                else
                {
                    // Use the custom image for the node
                    dotBuilder.AppendLine($"    {element.Name} [label=\"{element.Name}\", image=\"{Path.GetFullPath(iconPath)}\", labelloc=b];");
                }
            }

            // Define the connections (edges)
            foreach (var connectionList in modelDto.Connections.Values)
            {
                var elementNames = connectionList
                    .Select(c => c.Split('.')[0].Trim())
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct()
                    .ToList();

                if (elementNames.Count > 1)
                {
                    // Intelligente Hub-Auswahl
                    var elementsOnNode = modelDto.Elements.Where(e => elementNames.Contains(e.Name)).ToList();

                    // Priorität 1: Finde eine "Senke" (Tank, Exhaust)
                    var sink = elementsOnNode.FirstOrDefault(e =>
                        e.Type.Equals("Tank", StringComparison.OrdinalIgnoreCase) ||
                        e.Type.Equals("Exhaust", StringComparison.OrdinalIgnoreCase));

                    if (sink != null)
                    {
                        // Alle anderen Elemente zeigen auf die Senke
                        foreach (var element in elementsOnNode)
                        {
                            if (element.Name != sink.Name)
                            {
                                dotBuilder.AppendLine($"    {element.Name} -> {sink.Name};");
                            }
                        }
                    }
                    else
                    {
                        // Priorität 2: Finde eine "Quelle" (Supply, Epu)
                        var source = elementsOnNode.FirstOrDefault(e =>
                            e.Type.Equals("Supply", StringComparison.OrdinalIgnoreCase) ||
                            e.Type.Equals("Epu", StringComparison.OrdinalIgnoreCase));

                        if (source != null)
                        {
                            // Die Quelle zeigt auf alle anderen Elemente
                            foreach (var element in elementsOnNode)
                            {
                                if (element.Name != source.Name)
                                {
                                    dotBuilder.AppendLine($"    {source.Name} -> {element.Name};");
                                }
                            }
                        }
                        else
                        {
                            // Standard-Fallback: Erster Knoten ist der Hub
                            string hubElement = elementNames[0];
                            for (int i = 1; i < elementNames.Count; i++)
                            {
                                dotBuilder.AppendLine($"    {hubElement} -> {elementNames[i]};");
                            }
                        }
                    }
                }
            }


            dotBuilder.AppendLine("}");

            string dotString = dotBuilder.ToString();
            string dotFilePath = Path.ChangeExtension(outputImagePath, ".dot");
            File.WriteAllText(dotFilePath, dotString);

            // --- 2. Call the Graphviz 'dot' command-line tool ---
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "dot.exe";
                    // -Tpng specifies the output format
                    // -o specifies the output file path
                    process.StartInfo.Arguments = $"-Tpng \"{dotFilePath}\" -o \"{outputImagePath}\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardError = true;

                    process.Start();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        Console.WriteLine($"Schematic successfully saved to: {outputImagePath}");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("ERROR: Graphviz 'dot.exe' failed.");
                        Console.WriteLine("Please ensure Graphviz is installed and 'dot.exe' is in your system's PATH.");
                        Console.WriteLine($"Graphviz error output: {error}");
                        Console.ResetColor();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: Could not execute 'dot.exe'.");
                Console.WriteLine("Please ensure Graphviz is installed and in your system's PATH.");
                Console.WriteLine($"Exception: {ex.Message}");
                Console.ResetColor();
            }
        }
        // Diese Methode findet die Junctions für ein Element, ohne das Simulationsmodell zu benötigen.
        private static (int j1, int j2) FindJunctionsForElement(SimulationModelDto modelDto, string elementName)
        {
            int port1Junction = -1, port2Junction = -1;
            foreach (var entry in modelDto.Connections)
            {
                foreach (var connectionString in entry.Value)
                {
                    var parts = connectionString.Split('.');
                    if (parts.Length > 1 && parts[0].Trim().Equals(elementName, StringComparison.OrdinalIgnoreCase))
                    {
                        int.TryParse(entry.Key, out int junctionId);
                        string portId = parts[1].Trim().ToLower();
                        if (portId == "1" || portId == "in") port1Junction = junctionId;
                        if (portId == "2" || portId == "out") port2Junction = junctionId;
                    }
                }
            }
            return (port1Junction, port2Junction);
        }

        // Diese Methode führt die Graphenanalyse durch (Breitensuche).
        private static Dictionary<int, int> CalculateNodeDistances(SimulationModelDto modelDto)
        {
            var distances = new Dictionary<int, int>();
            var queue = new Queue<int>();

            // Finde alle Quellen (Supply, EPU) und füge ihre Junctions als Startpunkte hinzu
            foreach (var element in modelDto.Elements)
            {
                if (element.Type.ToLower() == "supply" || element.Type.ToLower() == "epu")
                {
                    var (j1, _) = FindJunctionsForElement(modelDto, element.Name);
                    if (j1 != -1 && !distances.ContainsKey(j1))
                    {
                        queue.Enqueue(j1);
                        distances[j1] = 0;
                    }
                }
            }

            while (queue.Count > 0)
            {
                int currentJunctionId = queue.Dequeue();
                string currentJunctionKey = currentJunctionId.ToString();

                if (!modelDto.Connections.TryGetValue(currentJunctionKey, out var connections)) continue;

                // Finde alle Elemente an der aktuellen Junction
                foreach (var connStr in connections)
                {
                    string elementName = connStr.Split('.')[0].Trim();
                    var (j1, j2) = FindJunctionsForElement(modelDto, elementName);

                    // Finde die Junction am "anderen Ende" des Elements
                    int otherJunctionId = (j1 == currentJunctionId) ? j2 : j1;

                    if (otherJunctionId != -1 && !distances.ContainsKey(otherJunctionId))
                    {
                        distances[otherJunctionId] = distances[currentJunctionId] + 1;
                        queue.Enqueue(otherJunctionId);
                    }
                }
            }
            return distances;
        }
    }
}