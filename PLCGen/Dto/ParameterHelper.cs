using System.Text.RegularExpressions;

namespace PLCGen
{
    public static class ParameterHelper
    {
        public record ParamSet { public double value = 0.0; public string unit = ""; }

        public static double GetDiameter(ElementDto dto, string key = "diameter") {
            ParamSet v = GetParam(dto, key, "0.0");
            double val = v.value;
            if (v.unit.Equals("cm")) val /= 100;
            if (v.unit.Equals("mm")) val /= 1000;
            if (val == 0.0) Console.WriteLine($"Warning: Element '{dto.Name}' has a '{key}' of 0.");
            return val;
        }
        public static double GetPressure(ElementDto dto)
        {
            ParamSet v = GetParam(dto, "pressure", "0.0");
            double val = v.value;
            if (v.unit.Equals("mbar")) val /= 1000;
            return val;
        }
        public static double GetVolume(ElementDto dto)
        {
            ParamSet v = GetParam(dto, "volume", "0.0");
            double val = v.value;
            if (v.unit.Equals("l")) val /= 1000;
            if (val == 0.0) Console.WriteLine($"Warning: Element '{dto.Name}' has a volume of 0.");
            return val;
        }
        public static double GetLength(ElementDto dto)
        {
            ParamSet v = GetParam(dto, "length", "0.0");
            double val = v.value;
            if (v.unit.Equals("mm")) val /= 1000;
            if (v.unit.Equals("cm")) val /= 100;
            if (val == 0.0) Console.WriteLine($"Warning: Element '{dto.Name}' has a length of 0.");
            return val;
        }
        public static ParamSet GetParam(ElementDto dto, string key, string? defaultValue = null)
        {
            if (!dto.Parameters.TryGetValue(key, out var input) || string.IsNullOrWhiteSpace(input))
            {
                if (defaultValue != null)
                {
                    Console.WriteLine($"Warning: Element '{dto.Name}' is missing parameter '{key}'. Using default value: {defaultValue}.");
                    input = defaultValue;
                }
                else
                {
                    // Return empty if no value and no default
                    return new();
                }
            }

            var match = Regex.Match(input, @"^([-+]?\d*\.?\d+)\s*([a-zA-Z]*)$");

            if (match.Success)
            {
                string number = match.Groups[1].Value;
                string unit = match.Groups[2].Value;

                if (double.TryParse(number, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double value))
                {
                    return new() { value = value, unit = unit };
                }
            }
            // Return default if parsing fails
            return new() { value = 0.0, unit = "" };
        }
        public static double GetDouble(ElementDto dto, string key, double defaultValue = 0.0)
        {
            if (!dto.Parameters.TryGetValue(key, out var raw))
            {
                 Console.WriteLine($"Warning: Element '{dto.Name}' is missing parameter '{key}'. Using default value: {defaultValue}.");
                return defaultValue;
            }

            return double.TryParse(raw, System.Globalization.NumberStyles.Any,
                                   System.Globalization.CultureInfo.InvariantCulture,
                                   out double value)
                ? value
                : defaultValue;
        }
    }
}
