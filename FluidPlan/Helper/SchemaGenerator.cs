// Create a new file: SchemaGenerator.cs
using System.Diagnostics;
using System.Text;

namespace FluidSimu
{
    public static class FileNameSanitizer
    {
        private static readonly HashSet<char> _invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars());

        /// <summary>
        /// Cleans a string to be used as a valid filename by replacing invalid characters.
        /// </summary>
        /// <param name="input">The raw string, e.g., from a model name.</param>
        /// <param name="replacement">The character to use for replacing invalid characters.</param>
        /// <returns>A sanitized, safe-to-use filename string.</returns>
        public static string Sanitize(string input, char replacement = '_')
        {
            // 1. Handle null or empty input
            if (string.IsNullOrWhiteSpace(input))
            {
                return "untitled_model";
            }

            var sb = new StringBuilder(input.Length);
            bool wasReplaced = false;

            // 2. Iterate and replace invalid characters
            foreach (char c in input)
            {
                if (_invalidChars.Contains(c))
                {
                    // Avoid multiple consecutive replacement characters (e.g., "My___File" from "My/*:File")
                    if (!wasReplaced)
                    {
                        sb.Append(replacement);
                        wasReplaced = true;
                    }
                }
                else
                {
                    sb.Append(c);
                    wasReplaced = false;
                }
            }

            // 3. Handle cases where the string is only invalid characters (e.g., "<>")
            string sanitized = sb.ToString();
            if (string.IsNullOrWhiteSpace(sanitized.Replace(replacement.ToString(), "")))
            {
                return "untitled_model";
            }

            // 4. Trim any leading/trailing replacement characters
            return sanitized.Trim(replacement);
        }
    }
    public static class SchemaGenerator
    {
        public static void CreateSchemaImage(SimulationModelDto model, string outputFolder, string iconsFolderPath = "schema_icons")
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
            foreach (var connection in model.Connections)
            {
                string processedEdge;
                if (connection.Contains('>'))
                {
                    // For directional connections like "R2 > CV1"
                    var parts = connection.Split('>').Select(p => p.Trim());
                    processedEdge = $"{string.Join(" -> ", parts)}";
                }
                else
                {
                    // For non-directional connections like "R1, Supply"
                    var parts = connection.Split(',').Select(p => p.Trim());
                    processedEdge = $"{string.Join(" -> ", parts)}";
                }
                dotBuilder.AppendLine($"    {processedEdge};");
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
    }
}