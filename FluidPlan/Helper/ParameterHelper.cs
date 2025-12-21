using FluidSimu;
using System.Text.RegularExpressions;

namespace FluidPlan.Helper
{
    public static class ParameterHelper
    {
        public record ParamSet { public double value = 0.0; public string unit = ""; }
        public static double GetDiameter(ElementDto dto, string key = "diameter") {
            ParamSet v = GetParam(dto, key, 0.0);
            double val = v.value;
            if (v.unit.Equals("cm")) val /= 100;
            if (v.unit.Equals("mm")) val /= 1000;
            return val;
        }
        public static double GetPressure(ElementDto dto)
        {
            // Standard bar
            ParamSet v = GetParam(dto, "pressure", 0.0);
            double val = v.value;
            if (v.unit.Equals("mbar"))
                val *= 1000;
            return val;
        }
        public static double GetVolume(ElementDto dto)
        {
            // Standard m³
            ParamSet v = GetParam(dto, "volume", 0.0);
            double val = v.value;
            if (v.unit.Equals("l"))
                val /= 1000;
            return val;
        }
        public static double GetLength(ElementDto dto)
        {
            // Standard m
            ParamSet v = GetParam(dto, "length", 0.0);
            double val = v.value;
            if (v.unit.Equals("mm"))
                val /= 1000;
            if (v.unit.Equals("cm"))
                val /= 100;
            return val;
        }
        public static ParamSet GetParam(ElementDto dto, string key, double defaultValue = 0.0)
        {
            string? input = "";
            if (!dto.Parameters.TryGetValue(key, out input))
                return new();

            var match = Regex.Match(input, @"^([-+]?\d*\.?\d+)\s*([a-zA-Z]*)$");

            if (match.Success)
            {
                string number = match.Groups[1].Value;
                string unit = match.Groups[2].Value;   // may be empty

                double value = 0;
                double.TryParse(number, System.Globalization.NumberStyles.Any,
                                      System.Globalization.CultureInfo.InvariantCulture,
                                      out value);

                return new() { value = value, unit = unit };
            }
            return new();
        }
        public static double GetDouble(ElementDto dto, string key, double defaultValue = 0.0)
        {
            if (!dto.Parameters.TryGetValue(key, out var raw))
                return defaultValue;

            return double.TryParse(raw, System.Globalization.NumberStyles.Any,
                                   System.Globalization.CultureInfo.InvariantCulture,
                                   out double value)
                ? value
                : defaultValue;
        }

        public static int GetInt(ElementDto dto, string key, int defaultValue = 0)
        {
            if (!dto.Parameters.TryGetValue(key, out var raw))
                return defaultValue;

            return int.TryParse(raw, out int value) ? value : defaultValue;
        }

        public static bool GetBool(ElementDto dto, string key, bool defaultValue = false)
        {
            if (!dto.Parameters.TryGetValue(key, out var raw))
                return defaultValue;

            if (bool.TryParse(raw, out bool result))
                return result;

            // Many people use 0/1 in JSON → support this
            if (raw == "1") return true;
            if (raw == "0") return false;

            return defaultValue;
        }

        public static string GetString(ElementDto dto, string key, string defaultValue = "")
        {
            return dto.Parameters.TryGetValue(key, out var raw) ? raw : defaultValue;
        }
    }

}
