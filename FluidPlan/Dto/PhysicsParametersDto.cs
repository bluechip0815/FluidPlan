using System.Text.Json.Serialization;

namespace FluidPlan.Dto
{
    public class PhysicsParametersDto
    {
        [JsonPropertyName("smoothingTimeConstant")]
        public double SmoothingTimeConstant { get; set; } = 0.005; // Standardwert 5ms

        [JsonPropertyName("airDensityRho")]
        public double AirDensityRho { get; set; } = 1.2; // Standardwert für Luft

        [JsonPropertyName("criticalPressureDelta")]
        public double CriticalPressureDelta { get; set; } = 0.5;
    }

}
