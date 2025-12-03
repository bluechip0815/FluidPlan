namespace FluidSimu
{
    public class TankElement : BaseElement
    {
        private readonly double Area;
        public TankElement(ElementDto dto, int num) : base(dto, num)
        {
            Type = PneumaticType.tank;

            Pressure = ParameterHelper.GetPressure(dto);
            Volume = ParameterHelper.GetVolume(dto);
            double diameter = ParameterHelper.GetDiameter(dto);

            Area = Math.PI / 4 * diameter * diameter;
        }
        protected override void DoStep(PneumaticModel model, IPneumaticElement otherNode)
        {
            // Drücke an den Enden [bar]
            double pFrom = Pressure;
            double pTo = otherNode.Pressure;

            // Volumenstrom [m³/s], positiv von From -> To
            double q = FlowPhysics.ComputeVolumeFlow(pFrom, pTo, Area, FlowCoefficient);

            // Mittlerer Druck für die Ladungsdefinition
            double pMean = 0.5 * (pFrom + pTo);

            // Ladungsstrom [m³·bar / s]
            double qCharge = FlowPhysics.VolumeFlowToChargeFlow(q, pMean);

            // Änderung der Ladung während dt
            double CurrentQ = qCharge * model.DeltaT;

            // From verliert Ladung, To gewinnt
            // KORRIGIERTE LOGIK:
            // Der Nachbar (To) gewinnt Ladung.
            model.AddCharge(new ChargeData() { Id = otherNode.Id, dQ = +CurrentQ });
            // Der Tank selbst (From) verliert Ladung.
            model.AddCharge(new ChargeData() { Id = Id, dQ = -CurrentQ });
        }
    }
}