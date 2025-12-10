namespace FluidSimu
{

    public class ThrottleElement : BaseElement
    {
        private readonly double KvFactor;
        public ThrottleElement(ElementDto dto, int num) : base(dto, num)
        {
            Type = PneumaticType.throttle;
            Pressure = ParameterHelper.GetPressure(dto);
            // z.B. ein Drossel-Koeffizient (0..1 oder größer)
            KvFactor = ParameterHelper.GetDouble(dto, "kv", 1.0);
        }
        protected override void DoStep(PneumaticModel model, IPneumaticElement otherNode)
        {
            // 1. Calculate the restricted area of the throttle itself.
            //    (Base Area calculated from Diameter) * (Throttle Factor)
            double throttledArea = this.Area * KvFactor;

            // 2. Delegate to the centralized physics engine.
            //    This will automatically:
            //    - Compare throttledArea vs. otherNode.Area (take the minimum).
            //    - Handle smoothing.
            //    - Use Upstream Pressure for charge calculation.
            CalculateAndApplyFlow(model, otherNode, throttledArea);
        }
    }
}