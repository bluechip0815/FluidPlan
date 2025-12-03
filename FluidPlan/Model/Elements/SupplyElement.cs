namespace FluidSimu
{
    public class SupplyElement : BaseElement
    {
        private readonly double Area;
        public SupplyElement(ElementDto dto, int num, bool Exhaust) : base(dto, num)
        {
            Type = Exhaust ? PneumaticType.exhaust : PneumaticType.supply;
            Pressure = Exhaust ? 0 : ParameterHelper.GetPressure(dto);

            // A supply needs a connection diameter to calculate flow. Let's use a sane default.
            double diameter = ParameterHelper.GetDiameter(dto);
            if (diameter == 0) diameter = 0.02; // Default to 2cm if not specified
            Area = Math.PI / 4 * diameter * diameter;
        }
        protected override void DoStep(PneumaticModel model, IPneumaticElement otherNode)
        {
            // Pressure "From" is always the supply's constant pressure.
            double pFrom = this.Pressure;
            // Pressure "To" is the neighbor's current pressure.
            double pTo = otherNode.Pressure;

            // Calculate the volume flow based on the pressure difference.
            double q = FlowPhysics.ComputeVolumeFlow(pFrom, pTo, this.Area, FlowCoefficient);

            // Calculate the charge flow.
            double pMean = 0.5 * (pFrom + pTo);
            double qCharge = FlowPhysics.VolumeFlowToChargeFlow(q, pMean);
            // Calculate the total charge transferred during this time step.
            double currentQ = qCharge * model.DeltaT;
            // The neighbor GAINS charge from the supply.
            model.AddCharge(new ChargeData() { Id = otherNode.Id, dQ = +currentQ });
            // The supply itself (an infinite source) does not lose charge. We add nothing for this.Id.
        }
        public override double CalcPressure(PneumaticModel model)
        {
            return 0;
        }
    }
}