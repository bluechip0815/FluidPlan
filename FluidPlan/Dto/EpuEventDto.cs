using System.Text.Json.Serialization;

namespace FluidPlan.Dto
{
    public class EpuEventDto
    {
        [JsonPropertyName("timeSeconds")]
        public double TimeSeconds { get; set; }
        // setpoint
        [JsonPropertyName("targetPressure")]
        public double TargetPressure { get; set; }
    }
}
