namespace FluidSimu
{
    public class CheckValveElement : BaseElement
    {
        private readonly double Area;
        private readonly double _openingDeltaP;
        public CheckValveElement(ElementDto dto, int num) : base(dto, num)
        {
            Type = PneumaticType.checkvalve;
            double diameter = ParameterHelper.GetDiameter(dto);
            Area = Math.PI / 4 * diameter * diameter;
            _openingDeltaP = ParameterHelper.GetDouble(dto, "openingdeltap", 0.0);
            ValidConnectorNames.AddRange(new[] { "1", "2" });
        }

        public override void CalcFlow(PneumaticModel model)
        {
            DoStep(model); // Call DoStep just once.
        }
        public override double CalcPressure(PneumaticModel model)
        {
            return 0.0;
        }
        protected override void DoStep(PneumaticModel model, IPneumaticElement? _=null)
        {
            // A check valve must have both an inlet ("1") and an outlet ("2") to function.
            if (!Connections.TryGetValue("1", out var inlet) || !Connections.TryGetValue("2", out var outlet))
            {
                // If not fully connected, do nothing.
                return;
            }

            // Get pressures from the actual connected elements
            double pFrom = inlet.Pressure;
            double pTo = outlet.Pressure;

            // Directional and cracking pressure checks
            if (pFrom <= pTo + _openingDeltaP)
            {
                LastFlow = 0;
                return; // Valve is closed
            }

            // If open, calculate flow between INLET and OUTLET
            double q = FlowPhysics.ComputeSmoothedVolumeFlow(pFrom, pTo, Area, FlowCoefficient, LastFlow, model.DeltaT);
            LastFlow = q;
            double pMean = 0.5 * (pFrom + pTo);
            double qCharge = FlowPhysics.VolumeFlowToChargeFlow(q, pMean);
            double currentQ = qCharge * model.DeltaT;

            // Apply charge to the connected elements, NOT the valve itself
            model.AddCharge(new ChargeData() { Id = inlet.Id, dQ = -currentQ });
            model.AddCharge(new ChargeData() { Id = outlet.Id, dQ = +currentQ });
        }
    }
}