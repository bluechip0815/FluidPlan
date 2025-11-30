using System.Text.Json.Serialization;

namespace FluidSimu
{
    public class ExecutionProfileDto
    {
        [JsonPropertyName("timeStepSeconds")]
        public double TimeStepSeconds { get; set; }

        [JsonPropertyName("steadyTolerance")]
        public double SteadyTolerance { get; set; }
        [JsonPropertyName("kNonlinear")]
        public double KNonlinear { get; set; }
        [JsonPropertyName("hardTimeLimit")]
        public double HardTimeLimit { get; set; }
        // valveId -> Timeline
        [JsonPropertyName("valveTimelines")]
        public Dictionary<string, List<ValveEventDto>> ValveTimelines { get; set; }
            = new();
        // epuId -> Timeline
        [JsonPropertyName("epuTimelines")]
        public Dictionary<string, List<EpuEventDto>> EpuTimelines { get; set; }
            = new();
        public double GetValveState(string valveId, double t)
        {
            if (!ValveTimelines.TryGetValue(valveId, out var timeline) ||
                timeline.Count == 0)
                return 0.0;

            // letztes Event mit time <= t
            double state = 0.0;
            foreach (var e in timeline)
            {
                if (e.TimeSeconds <= t)
                    state = e.State;
                else
                    break;
            }
            return state;
        }
        public double GetEpuPressureDelta(string epuId, double t)
        {
            if (!EpuTimelines.TryGetValue(epuId, out var timeline) ||
                timeline.Count == 0)
                return 0.0;

            double delta = 0.0;
            foreach (var e in timeline)
            {
                if (e.TimeSeconds <= t)
                    delta = e.TargetPressure;
                else
                    break;
            }
            return delta;
        }
        public double GetLastValveEventTime()
        {
            double maxTime = 0.0;

            // Check Valve Timelines
            foreach (var timeline in ValveTimelines.Values)
            {
                if (timeline.Any())
                {
                    double t = timeline.Max(x => x.TimeSeconds);
                    if (t > maxTime) maxTime = t;
                }
            }

            return maxTime;

        }
        public double GetLastEpuEventTime()
        {
            double maxTime = 0.0;

            // Check EPU Timelines
            foreach (var timeline in EpuTimelines.Values)
            {
                if (timeline.Any())
                {
                    double t = timeline.Max(x => x.TimeSeconds);
                    if (t > maxTime) maxTime = t;
                }
            }

            return maxTime;

        }
    }
    public class ValveEventDto
    {
        [JsonPropertyName("timeSeconds")]
        public double TimeSeconds { get; set; }

        // 0.0 .. 1.0 (oder diskret: 0/1)
        [JsonPropertyName("state")]
        public double State { get; set; }
    }
    public class EpuEventDto
    {
        [JsonPropertyName("timeSeconds")]
        public double TimeSeconds { get; set; }
        // setpoint
        [JsonPropertyName("targetPressure")]
        public double TargetPressure { get; set; }
    }
}
