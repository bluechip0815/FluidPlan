using System.Text.Json.Serialization;

namespace FluidPlan.Dto
{
    public class ValveEventDto
    {
        public ValveEventDto()
        {
        }

        [JsonPropertyName("timeSeconds")]
        public double TimeSeconds { get; set; }

        // 0.0 .. 1.0 (oder diskret: 0/1)
        [JsonPropertyName("state")]
        public double State { get; set; }
    }
}
