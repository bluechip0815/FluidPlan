using System.Text;
using System.Text.Json;
using PLCGen;

namespace PLCGen
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting PLC code generation...");

            string modelPath = Path.Combine("..", "FluidPlan", "model.json");
            string outputPath = Path.Combine("..", "FluidPlan", "F_InitializeModel.TcPOU");

            try
            {
                var modelDto = LoadJson<SimulationModelDto>(modelPath);
                if (modelDto == null || modelDto.Elements == null)
                {
                    Console.WriteLine("Error: Failed to deserialize model.json or it contains no elements.");
                    return;
                }

                string generatedCode = GeneratePlcCode(modelDto);
                File.WriteAllText(outputPath, generatedCode);

                Console.WriteLine($"Successfully generated PLC code to '{outputPath}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private static T? LoadJson<T>(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"Error: Input file not found at '{path}'");
                throw new FileNotFoundException("Input model file not found.", path);
            }
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });
        }

        private static string GeneratePlcCode(SimulationModelDto dto)
        {
            var sb = new StringBuilder();
            var nameToIndexMap = new Dictionary<string, int>();
            for (int i = 0; i < dto.Elements.Count; i++)
            {
                nameToIndexMap[dto.Elements[i].Name] = i + 1;
            }

            sb.AppendLine("(*====================================================================");
            sb.AppendLine("    FUNCTION F_InitializeModel");
            sb.AppendLine("    Description: Automatically generated from model.json.");
            sb.AppendLine($"                 Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine("====================================================================*)");
            sb.AppendLine("FUNCTION F_InitializeModel : BOOL");
            sb.AppendLine("VAR");
            sb.AppendLine("END_VAR");
            sb.AppendLine();
            sb.AppendLine("// Clear arrays first");
            sb.AppendLine("MEMSET(ADR(g_arElementConfigs), 0, SIZEOF(g_arElementConfigs));");
            sb.AppendLine("MEMSET(ADR(g_arConnections), 0, SIZEOF(g_arConnections));");
            sb.AppendLine();
            sb.AppendLine("// --- DEFINE ELEMENTS from model.json ---");

            int elementIndex = 1;
            foreach (var element in dto.Elements)
            {
                sb.AppendLine();
                sb.AppendLine($"// Element {elementIndex}: {element.Name}");
                string baseElementPath = $"g_arElementConfigs[{elementIndex}]";

                sb.AppendLine($"{baseElementPath}.sName := '{element.Name}';");

                switch (element.Type.ToLower())
                {
                    case "supply":
                        sb.AppendLine($"{baseElementPath}.eType := E_ElementType.Supply;");
                        sb.AppendLine($"{baseElementPath}.fInitialPressure := {ParameterHelper.GetPressure(element):F4};");
                        break;

                    case "pipe":
                        sb.AppendLine($"{baseElementPath}.eType := E_ElementType.Pipe;");
                        double diameter = ParameterHelper.GetDiameter(element);
                        double length = ParameterHelper.GetLength(element);
                        double area = Math.PI / 4 * diameter * diameter;
                        double volume = area * length;
                        sb.AppendLine($"{baseElementPath}.fVolume := {volume:E6}; // {length*100}cm length, {diameter*100}cm diameter");
                        sb.AppendLine($"{baseElementPath}.fConnectionArea := {area:E6}; // {diameter*100}cm diameter");
                        break;

                    case "valve":
                        sb.AppendLine($"{baseElementPath}.eType := E_ElementType.Valve;");
                        double valveDiameter = ParameterHelper.GetDiameter(element);
                        double valveArea = Math.PI / 4 * valveDiameter * valveDiameter;
                        sb.AppendLine($"{baseElementPath}.fConnectionArea := {valveArea:E6}; // {valveDiameter*100}cm diameter");
                        break;

                    case "throttle":
                        sb.AppendLine($"{baseElementPath}.eType := E_ElementType.Throttle;");
                        double throttleDiameter = ParameterHelper.GetDiameter(element);
                        double throttleArea = Math.PI / 4 * throttleDiameter * throttleDiameter;
                        sb.AppendLine($"{baseElementPath}.fConnectionArea := {throttleArea:E6}; // {throttleDiameter*100}cm diameter");
                        break;

                    case "checkvalve":
                        sb.AppendLine($"{baseElementPath}.eType := E_ElementType.CheckValve;");
                        double checkValveDiameter = ParameterHelper.GetDiameter(element);
                        double checkValveArea = Math.PI / 4 * checkValveDiameter * checkValveDiameter;
                        sb.AppendLine($"{baseElementPath}.fConnectionArea := {checkValveArea:E6}; // {checkValveDiameter*100}cm diameter");
                        break;

                    case "regulator":
                        sb.AppendLine($"{baseElementPath}.eType := E_ElementType.Regulator;");
                        double regulatorPortDiameter = ParameterHelper.GetDiameter(element, "portDiameter");
                        if (regulatorPortDiameter == 0) regulatorPortDiameter = ParameterHelper.GetDiameter(element, "diameter");
                        if (regulatorPortDiameter == 0) {
                            regulatorPortDiameter = 0.02; // 2cm default
                            Console.WriteLine($"Warning: Regulator '{element.Name}' has no 'portDiameter' or 'diameter' specified. Defaulting to 2cm.");
                        }
                        double regulatorArea = Math.PI / 4 * regulatorPortDiameter * regulatorPortDiameter;
                        sb.AppendLine($"{baseElementPath}.fConnectionArea := {regulatorArea:E6}; // {regulatorPortDiameter*100}cm diameter");

                        double targetPressure = ParameterHelper.GetDouble(element, "pressure", 0.0);
                        double kp = ParameterHelper.GetDouble(element, "kp", 0.5);
                        double ki = ParameterHelper.GetDouble(element, "ki", 5.0);

                        sb.AppendLine($"{baseElementPath}.stRegulatorParams.fTargetPressure := {targetPressure:F2};");
                        sb.AppendLine($"{baseElementPath}.stRegulatorParams.fKp := {kp:F2};");
                        sb.AppendLine($"{baseElementPath}.stRegulatorParams.fKi := {ki:F2};");
                        break;

                    case "tank":
                        sb.AppendLine($"{baseElementPath}.eType := E_ElementType.Tank;");
                        double tankVolume = ParameterHelper.GetVolume(element);
                        double portDiameter = ParameterHelper.GetDiameter(element, "portDiameter");
                        double portArea = Math.PI / 4 * portDiameter * portDiameter;
                        sb.AppendLine($"{baseElementPath}.fVolume := {tankVolume:E6}; // {tankVolume * 1000}l");
                        sb.AppendLine($"{baseElementPath}.fConnectionArea := {portArea:E6}; // {portDiameter*100}cm diameter");
                        break;

                    case "epu":
                        sb.AppendLine($"{baseElementPath}.eType := E_ElementType.EPU;");
                        double epuPortDiameter = ParameterHelper.GetDiameter(element, "portDiameter");
                        if (epuPortDiameter == 0) epuPortDiameter = ParameterHelper.GetDiameter(element, "diameter");
                        if (epuPortDiameter == 0) {
                            epuPortDiameter = 0.02; // 2cm default
                            Console.WriteLine($"Warning: EPU '{element.Name}' has no 'portDiameter' or 'diameter' specified. Defaulting to 2cm.");
                        }
                        double epuArea = Math.PI / 4 * epuPortDiameter * epuPortDiameter;
                        sb.AppendLine($"{baseElementPath}.fConnectionArea := {epuArea:E6}; // {epuPortDiameter*100}cm diameter");

                        double timeConstant = ParameterHelper.GetDouble(element, "timeConstant", 1.1);
                        double maxDpDt = ParameterHelper.GetDouble(element, "maxDpDt", 35.0);
                        double naturalFreq = ParameterHelper.GetDouble(element, "naturalFrequency", 20.0);
                        double dampingRatio = ParameterHelper.GetDouble(element, "dampingRatio", 0.7);

                        sb.AppendLine($"{baseElementPath}.stEpuParams.fTimeConstant := {timeConstant:F2};");
                        sb.AppendLine($"{baseElementPath}.stEpuParams.fMaxDpDt := {maxDpDt:F2};");
                        sb.AppendLine($"{baseElementPath}.stEpuParams.fNaturalFrequency := {naturalFreq:F2};");
                        sb.AppendLine($"{baseElementPath}.stEpuParams.fDampingRatio := {dampingRatio:F2};");
                        break;

                    default:
                        sb.AppendLine($"// UNSUPPORTED ELEMENT TYPE: {element.Type}");
                        break;
                }
                elementIndex++;
            }

            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("// --- DEFINE CONNECTIONS from model.json ---");
            sb.AppendLine("// Connections are defined by the indices of the elements in the g_arElementConfigs array.");

            int connectionIndex = 1;
            foreach (var connectionStr in dto.Connections)
            {
                 var names = connectionStr.Split(new[] { ',', '>' }, StringSplitOptions.RemoveEmptyEntries).Select(n => n.Trim()).ToArray();
                 if (names.Length == 2)
                 {
                    if (nameToIndexMap.TryGetValue(names[0], out int indexA) && nameToIndexMap.TryGetValue(names[1], out int indexB))
                    {
                        bool isDirectional = connectionStr.Contains('>');
                        string comment = isDirectional ? $"{names[0]} > {names[1]}" : $"{names[0]}, {names[1]}";
                        sb.AppendLine($"g_arConnections[{connectionIndex}].iElementA := {indexA}; g_arConnections[{connectionIndex}].iElementB := {indexB}; g_arConnections[{connectionIndex}].bIsDirectional := {isDirectional.ToString().ToUpper()}; // {comment}");
                        connectionIndex++;
                    }
                 }
            }

            sb.AppendLine();
            sb.AppendLine("F_InitializeModel := TRUE;");

            return sb.ToString();
        }
    }
}
