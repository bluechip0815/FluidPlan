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
            double portDiameter = ParameterHelper.GetDiameter(dto, "portDiameter");
            if (portDiameter == 0.0) portDiameter = ParameterHelper.GetDiameter(dto, "diameter");

            Area = Math.PI / 4 * portDiameter * portDiameter;
            ValidConnectorNames.Add("1");
        }

        // DoStep logic is identical to PipeElement's new logic.
        protected override void DoStep(PneumaticModel model, IPneumaticElement otherNode)
        {
            double pFrom = Pressure;
            double pTo = otherNode.Pressure;

            // Volumenstrom [m³/s], positiv von From -> To
            double q = FlowPhysics.ComputeSmoothedVolumeFlow(pFrom, pTo, Area, FlowCoefficient, LastFlow, model.DeltaT);
            LastFlow = q;
            double pMean = 0.5 * (pFrom + pTo);
            double qCharge = FlowPhysics.VolumeFlowToChargeFlow(q, pMean);
            double CurrentQ = qCharge * model.DeltaT;

            model.AddCharge(new ChargeData() { Id = otherNode.Id, dQ = +CurrentQ });
            model.AddCharge(new ChargeData() { Id = Id, dQ = -CurrentQ });
        }
    }
}