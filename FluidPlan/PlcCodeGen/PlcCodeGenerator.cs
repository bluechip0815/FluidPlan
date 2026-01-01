using System.Text;
using FluidPlan.Dto;  // Assuming your DTOs are here
using FluidPlan.Helper; // Assuming ParameterHelper is here

namespace FluidSimu
{
    public static class PlcCodeGenerator
    {
        public static void Generate(string modelPath, string outputPath)
        {
            // 1. Load Model using existing infrastructure
            // Note: We reuse the exact same loading logic as the simulation
            var modelDto = ModelLoader.LoadJson<SimulationModelDto>(modelPath);
            
            Console.WriteLine($"Generating PLC code for model: {modelDto.ModelName}");

            var sb = new StringBuilder();

            // --- HEADER ---
            sb.AppendLine("(*====================================================================");
            sb.AppendLine($"    FUNCTION F_InitializeModel");
            sb.AppendLine($"    Generated from: {Path.GetFileName(modelPath)}");
            sb.AppendLine($"    Date: {DateTime.Now}");
            sb.AppendLine("====================================================================*)");
            sb.AppendLine("FUNCTION F_InitializeModel : BOOL");
            sb.AppendLine("VAR");
            sb.AppendLine("    // No local vars needed");
            sb.AppendLine("END_VAR");
            sb.AppendLine("");
            sb.AppendLine("// 1. Reset Arrays");
            sb.AppendLine("MEMSET(ADR(GVL.g_arElementConfigs), 0, SIZEOF(GVL.g_arElementConfigs));");
            sb.AppendLine("MEMSET(ADR(GVL.g_arConnections), 0, SIZEOF(GVL.g_arConnections));");
            sb.AppendLine("");

            // --- ELEMENTS ---
            // Create a mapping from Name -> Index for connection generation later
            var nameToIndex = new Dictionary<string, int>();
            int idx = 1;

            foreach (var el in modelDto.Elements)
            {
                nameToIndex[el.Name] = idx;
                
                sb.AppendLine($"// Element {idx}: {el.Name} ({el.Type})");
                string prefix = $"GVL.g_arElementConfigs[{idx}]";

                sb.AppendLine($"{prefix}.sName := '{el.Name}';");

                // Map Element Types
                // Using ToLower() for robust comparison
                var culture = System.Globalization.CultureInfo.InvariantCulture;

                switch (el.Type.ToLower())
                {
                    // --- SUPPLY / EXHAUST ---
                    case "supply":
                        sb.AppendLine($"{prefix}.eType := E_ElementType.Supply;");
                        sb.AppendLine($"{prefix}.fInitialPressure := {ParameterHelper.GetPressure(el).ToString("F4", culture)};");
                        break;

                    case "exhaust":
                        sb.AppendLine($"{prefix}.eType := E_ElementType.Exhaust;");
                        // Exhaust acts like a supply at 0 bar (Atmosphere)
                        sb.AppendLine($"{prefix}.fInitialPressure := 0.0;");
                        break;

                    // --- PASSIVE ELEMENTS ---
                    case "pipe":
                        sb.AppendLine($"{prefix}.eType := E_ElementType.Pipe;");
                        sb.AppendLine($"{prefix}.fVolume := {ParameterHelper.GetVolume(el).ToString("E6", culture)};");

                        // Calculate Area from Diameter
                        double pDia = ParameterHelper.GetDiameter(el);
                        double pArea = Math.PI * Math.Pow(pDia / 2.0, 2);
                        sb.AppendLine($"{prefix}.fConnectionArea := {pArea.ToString("E6", culture)};");
                        break;

                    case "tank":
                        sb.AppendLine($"{prefix}.eType := E_ElementType.Tank;");
                        sb.AppendLine($"{prefix}.fVolume := {ParameterHelper.GetVolume(el).ToString("E6", culture)};");
                        sb.AppendLine($"{prefix}.fInitialPressure := {ParameterHelper.GetPressure(el).ToString("F4", culture)};");

                        // Tanks often define 'portDiameter' separately, or fallback to 'diameter'
                        double tDia = ParameterHelper.GetDouble(el, "portDiameter", 0.0);
                        if (tDia <= 0) tDia = ParameterHelper.GetDiameter(el);

                        double tArea = Math.PI * Math.Pow(tDia / 2.0, 2);
                        sb.AppendLine($"{prefix}.fConnectionArea := {tArea.ToString("E6", culture)};");
                        break;

                    // --- VALVES ---
                    case "valve":
                        sb.AppendLine($"{prefix}.eType := E_ElementType.Valve;");
                        double vDia = ParameterHelper.GetDiameter(el);
                        double vArea = Math.PI * Math.Pow(vDia / 2.0, 2);
                        sb.AppendLine($"{prefix}.fConnectionArea := {vArea.ToString("E6", culture)};");
                        sb.AppendLine($"{prefix}.fFlowCoefficient := {el.FlowCoefficient.ToString("F2", culture)};");

                        // Optional: Map Valve switching times if present in JSON
                        // sb.AppendLine($"{prefix}.stValveParams.fOpenTime_s := 0.05;"); 
                        break;

                    case "throttle":
                        sb.AppendLine($"{prefix}.eType := E_ElementType.Throttle;");
                        double thDia = ParameterHelper.GetDiameter(el);
                        double thArea = Math.PI * Math.Pow(thDia / 2.0, 2);
                        sb.AppendLine($"{prefix}.fConnectionArea := {thArea.ToString("E6", culture)};");
                        sb.AppendLine($"{prefix}.fFlowCoefficient := {el.FlowCoefficient.ToString("F2", culture)};");
                        break;

                    case "checkvalve":
                        sb.AppendLine($"{prefix}.eType := E_ElementType.CheckValve;");
                        double cvDia = ParameterHelper.GetDiameter(el);
                        double cvArea = Math.PI * Math.Pow(cvDia / 2.0, 2);
                        sb.AppendLine($"{prefix}.fConnectionArea := {cvArea.ToString("E6", culture)};");
                        sb.AppendLine($"{prefix}.fFlowCoefficient := {el.FlowCoefficient.ToString("F2", culture)};");
                        break;

                    // --- ACTIVE CONTROL ---
                    case "regulator":
                        sb.AppendLine($"{prefix}.eType := E_ElementType.Regulator;");

                        // Physical connection size
                        double rDia = ParameterHelper.GetDiameter(el);
                        double rArea = Math.PI * Math.Pow(rDia / 2.0, 2);
                        sb.AppendLine($"{prefix}.fConnectionArea := {rArea.ToString("E6", culture)};");

                        // Controller Parameters
                        // Note: Using GetDouble generic helper to extract 'kp', 'ki' from params
                        double targetP = ParameterHelper.GetDouble(el, "pressure", 0.0);
                        double kp = ParameterHelper.GetDouble(el, "kp", 0.5);
                        double ki = ParameterHelper.GetDouble(el, "ki", 5.0);

                        sb.AppendLine($"{prefix}.stRegulatorParams.fTargetPressure := {targetP.ToString("F2", culture)};");
                        sb.AppendLine($"{prefix}.stRegulatorParams.fKp := {kp.ToString("F2", culture)};");
                        sb.AppendLine($"{prefix}.stRegulatorParams.fKi := {ki.ToString("F2", culture)};");
                        break;

                    case "epu":
                        sb.AppendLine($"{prefix}.eType := E_ElementType.EPU;");

                        double eDia = ParameterHelper.GetDiameter(el);
                        double eArea = Math.PI * Math.Pow(eDia / 2.0, 2);
                        sb.AppendLine($"{prefix}.fConnectionArea := {eArea.ToString("E6", culture)};");

                        // EPU Parameters
                        double maxDpDt = ParameterHelper.GetDouble(el, "maxDpDt", 35.0);
                        double tConst = ParameterHelper.GetDouble(el, "timeConstant", 0.1);
                        double freq = ParameterHelper.GetDouble(el, "naturalFrequency", 20.0);
                        double damp = ParameterHelper.GetDouble(el, "dampingRatio", 0.7);

                        sb.AppendLine($"{prefix}.stEpuParams.fMaxDpDt := {maxDpDt.ToString("F1", culture)};");
                        sb.AppendLine($"{prefix}.stEpuParams.fTimeConstant := {tConst.ToString("F3", culture)};");
                        sb.AppendLine($"{prefix}.stEpuParams.fNaturalFrequency := {freq.ToString("F1", culture)};");
                        sb.AppendLine($"{prefix}.stEpuParams.fDampingRatio := {damp.ToString("F2", culture)};");
                        break;

                    default:
                        sb.AppendLine($"// WARNING: Unknown Element Type '{el.Type}' for element '{el.Name}'");
                        break;
                }
                sb.AppendLine("");
                idx++;
            }

            // --- CONNECTIONS ---
            int connIdx = 1;
            sb.AppendLine("// --- CONNECTIONS ---");
            sb.AppendLine("// Mapping JSON Junctions (Nodes) to PLC Pairs");
            foreach (var kvp in modelDto.Connections)
            {
                string junctionId = kvp.Key;
                List<string> connectedPorts = kvp.Value;

                // 1. Identify all unique elements at this junction
                //    Input ex: ["R1.Out", "V1.In"] -> ["R1", "V1"]
                var participatingElements = connectedPorts
                    .Select(p => p.Split('.')[0].Trim()) // Remove .1, .In, etc
                    .Distinct()
                    .Where(name => nameToIndex.ContainsKey(name)) // Filter invalid names
                    .ToList();

                // We need at least 2 elements to form a connection
                if (participatingElements.Count < 2)
                {
                    sb.AppendLine($"// Junction {junctionId}: Skipped (Single or Empty connection)");
                    continue;
                }

                // 2. Generate Pairwise Connections
                //    Standard case: 2 elements -> 1 connection (A-B)
                //    T-Junction case: 3 elements -> 2 connections (A-B, B-C) 
                //    This allows pressure to propagate A<->B<->C
                for (int i = 0; i < participatingElements.Count - 1; i++)
                {
                    string nameA = participatingElements[i];
                    string nameB = participatingElements[i + 1];

                    int idA = nameToIndex[nameA];
                    int idB = nameToIndex[nameB];

                    sb.AppendLine($"// Connection {connIdx}: Junction {junctionId} ({nameA} <-> {nameB})");
                    sb.AppendLine($"GVL.g_arConnections[{connIdx}].iElementA := {idA};");
                    sb.AppendLine($"GVL.g_arConnections[{connIdx}].iElementB := {idB};");

                    connIdx++;
                }
            }
            sb.AppendLine("F_InitializeModel := TRUE;");
            
            // Write File
            File.WriteAllText(outputPath, sb.ToString());
            Console.WriteLine($"Success. Output written to {outputPath}");
        }
    }
}