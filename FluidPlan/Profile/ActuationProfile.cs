using FluidPlan.Dto;

namespace FluidSimu
{

    // --- Zeitlinien / Execution-Profile ---

    public sealed class ValveScheduleEntry
    {
        public double Time { get; init; }   // ab t0
        public bool IsOn { get; init; }
    }

    public sealed class EpuScheduleEntry
    {
        public double Time { get; init; }   // ab t0
        public double Value { get; init; }    // z.B. Druck-Delta, bar
    }

    public interface IActuationProfile
    {
        bool GetValveState(string valveId, double time);
        double GetEpuCommand(string epuId, double time);
    }
    // Adapter to make ExecutionProfileDto compatible with IActuationProfile
    public class ProfileAdapter : IActuationProfile
    {
        private readonly ExecutionProfileDto _dto;
        public ProfileAdapter(ExecutionProfileDto dto) { _dto = dto; }

        public bool GetValveState(string valveId, double time) =>
            _dto.GetValveState(valveId, time) > 0.5; // Threshold for 0/1

        public double GetEpuCommand(string epuId, double time) =>
            _dto.GetEpuPressureDelta(epuId, time);
    }

}
