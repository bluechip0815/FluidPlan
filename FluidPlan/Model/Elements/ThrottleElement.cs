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
            double diameter = ParameterHelper.GetDiameter(dto);

            Area = Math.PI / 4 * diameter * diameter;

            // z.B. ein Drossel-Koeffizient (0..1 oder größer)
            KvFactor = ParameterHelper.GetDouble(dto, "kv", 1.0);
        }
        protected override void DoStep(PneumaticModel model, IPneumaticElement otherNode)
        {
            double pFrom = otherNode.Pressure;
            double pTo = Pressure;

            double effectiveArea = Area * KvFactor;

            double q = FlowPhysics.ComputeSmoothedVolumeFlow(pFrom, pTo, effectiveArea, FlowCoefficient, LastFlow, model.DeltaT);
            LastFlow = q;
            double pMean = 0.5 * (pFrom + pTo);
            double qCharge = FlowPhysics.VolumeFlowToChargeFlow(q, pMean);
            double CurrentQ = qCharge * model.DeltaT;

            model.AddCharge(new ChargeData() { Id = otherNode.Id, dQ = -CurrentQ });
            model.AddCharge(new ChargeData() { Id = Id, dQ = +CurrentQ });
        }
    }
}