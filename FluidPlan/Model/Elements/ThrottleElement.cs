namespace FluidSimu
{

    public class ThrottleElement : BaseElement
    {
        private readonly double KvFactor;
        private readonly double Area;
        public ThrottleElement(ElementDto dto, int num) : base(dto, num)
        {
            Type = PneumaticType.throttle;

            Pressure = ParameterHelper.GetPressure(dto);

            Area = Math.PI / 4 * Diameter * Diameter;

            // z.B. ein Drossel-Koeffizient (0..1 oder größer)
            KvFactor = ParameterHelper.GetDouble(dto, "kv", 1.0);
        }
        protected override void DoStep(PneumaticModel model, IPneumaticElement otherNode)
        {
            var effectiveDiameter = Math.Min(Diameter, otherNode.Diameter);
            var effectiveArea = Math.PI / 4 * Math.Pow(effectiveDiameter, 2) * KvFactor;
            double q = FlowPhysics.ComputeSmoothedVolumeFlow(otherNode.Pressure, Pressure, effectiveArea, FlowCoefficient, LastFlow, model.DeltaT);
            LastFlow = q;
            double pMean = 0.5 * (otherNode.Pressure + Pressure);
            double qCharge = FlowPhysics.VolumeFlowToChargeFlow(q, pMean);
            double CurrentQ = qCharge * model.DeltaT;

            model.AddCharge(new ChargeData() { Id = otherNode.Id, dQ = -CurrentQ });
            model.AddCharge(new ChargeData() { Id = Id, dQ = +CurrentQ });
        }
    }
}