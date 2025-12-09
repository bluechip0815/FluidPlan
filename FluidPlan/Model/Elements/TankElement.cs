namespace FluidSimu
{
    public class TankElement : BaseElement
    {
        private readonly double Area = 0.0;
        public TankElement(ElementDto dto, int num) : base(dto, num)
        {
            Type = PneumaticType.tank;
            Pressure = ParameterHelper.GetPressure(dto);
            Volume = ParameterHelper.GetVolume(dto);

            // A tank now has a specific port diameter for its connection.
            Area = Math.PI / 4 * Diameter * Diameter;
        }

        // DoStep logic is identical to PipeElement's new logic.
        protected override void DoStep(PneumaticModel model, IPneumaticElement otherNode)
        {
            // Volumenstrom [m³/s], positiv von From -> To
            var effectiveDiameter = Math.Min(Diameter, otherNode.Diameter);
            var effectiveArea = Math.PI / 4 * Math.Pow(effectiveDiameter, 2);
            double q = FlowPhysics.ComputeSmoothedVolumeFlow(Pressure, otherNode.Pressure, effectiveArea, FlowCoefficient, LastFlow, model.DeltaT);
            LastFlow = q;
            double pMean = 0.5 * (Pressure + otherNode.Pressure);
            double qCharge = FlowPhysics.VolumeFlowToChargeFlow(q, pMean);
            double CurrentQ = qCharge * model.DeltaT;

            model.AddCharge(new ChargeData() { Id = otherNode.Id, dQ = +CurrentQ });
            model.AddCharge(new ChargeData() { Id = Id, dQ = -CurrentQ });
        }
    }
}