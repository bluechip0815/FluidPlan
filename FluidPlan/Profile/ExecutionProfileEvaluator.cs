using FluidPlan.Dto;

namespace FluidSimu
{
    public class ExecutionProfileEvaluator
    {
        public ExecutionProfileEvaluator(ExecutionProfileDto _profile)
        {
            // Optional: Timelines nach Zeit sortieren
            foreach (var tl in _profile.ValveTimelines.Values)
                tl.Sort((a, b) => a.TimeSeconds.CompareTo(b.TimeSeconds));

            foreach (var tl in _profile.EpuTimelines.Values)
                tl.Sort((a, b) => a.TimeSeconds.CompareTo(b.TimeSeconds));
        }
    }
}