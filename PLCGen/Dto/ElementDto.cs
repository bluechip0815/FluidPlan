using System.Text.Json.Serialization;

namespace PLCGen
{
    public class ElementDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("description")]
        public string? Description { get; set; }
        [JsonPropertyName("comment")]
        public string? Comment { get; set; }

        [JsonPropertyName("visible")]
        public bool Visible { get; set; }

        [JsonPropertyName("flowCoefficient")]
        public double FlowCoefficient { get; set; } = 1.0;

        [JsonPropertyName("parameters")]
        public Dictionary<string, string> Parameters { get; set; } = new();
    }
}
