using System.Text.Json.Serialization;

namespace PLCGen
{
    // --- Gemeinsame DTOs für JSON ---
    public class SimulationModelDto
    {
        [JsonPropertyName("modelName")]
        public string ModelName { get; set; } = "";
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
        [JsonPropertyName("elements")]
        public List<ElementDto> Elements { get; set; } = new();
        [JsonPropertyName("connections")]
        public List<string> Connections { get; set; } = new();
    }
}
