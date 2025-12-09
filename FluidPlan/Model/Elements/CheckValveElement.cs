namespace FluidSimu
{
    public interface IDirectionalElement
    {
        void SetDirection(IPneumaticElement inlet/*, IPneumaticElement outlet*/);
        void RegisterNeighbor(IPneumaticElement neighbor); // Add this
    }

    public class CheckValveElement : BaseElement, IDirectionalElement
    {
        private readonly double Area;
        private readonly double _openingDeltaP;

        private IPneumaticElement? _inlet;
        private readonly List<IPneumaticElement> _neighbors = new();
        public CheckValveElement(ElementDto dto, int num) : base(dto, num)
        {
            Type = PneumaticType.checkvalve;
            Area = Math.PI / 4 * Diameter * Diameter;
            _openingDeltaP = ParameterHelper.GetDouble(dto, "openingdeltap", 0.0);
        }
        // --- Neighbor management methods ---
        public void RegisterNeighbor(IPneumaticElement element)
        {
            if (!_neighbors.Contains(element))
                _neighbors.Add(element);
        }
        // The '>' syntax in the connection string sets the inlet.
        public void SetDirection(IPneumaticElement inlet/*, IPneumaticElement outlet*/)
        {
            _inlet = inlet;
            // The 'outlet' parameter here is the check valve itself, which we ignore.
        }
        public new void CalcFlow(PneumaticModel model, List<IPneumaticElement> elements, int startIndex)
        {
            DoStep(model, null); // Call DoStep just once.
        }
        public override double CalcPressure(PneumaticModel model)
        {
            return 0.0;
        }
        protected override void DoStep(PneumaticModel model, IPneumaticElement? otherNode)
        {
            // Ensure the inlet has been set by a ">" connection
            if (_inlet == null)
                return;

            // Find the outlet: it's the neighbor that is NOT the inlet.
            var outlet = _neighbors.FirstOrDefault(n => n.Id != _inlet.Id);

            // If we don't have two distinct neighbors, the valve is not fully connected.
            if (outlet == null)
                return;

            // Get pressures from the actual connected elements
            double pFrom = _inlet.Pressure;
            double pTo = outlet.Pressure;

            // Directional and cracking pressure checks
            if (pFrom <= pTo + _openingDeltaP)
            {
                LastFlow = 0;
                return; // Valve is closed
            }

            // If open, calculate flow between INLET and OUTLET
            var effectiveDiameter = Math.Min(_inlet.Diameter, outlet.Diameter);
            var effectiveArea = Math.PI / 4 * Math.Pow(effectiveDiameter, 2);
            double q = FlowPhysics.ComputeSmoothedVolumeFlow(_inlet.Pressure, outlet.Pressure, effectiveArea, FlowCoefficient, LastFlow, model.DeltaT);
            LastFlow = q;
            double pMean = 0.5 * (_inlet.Pressure + outlet.Pressure);
            double qCharge = FlowPhysics.VolumeFlowToChargeFlow(q, pMean);
            double currentQ = qCharge * model.DeltaT;

            // Apply charge to the connected elements, NOT the valve itself
            model.AddCharge(new ChargeData() { Id = _inlet.Id, dQ = -currentQ });
            model.AddCharge(new ChargeData() { Id = outlet.Id, dQ = +currentQ });
        }
    }
}