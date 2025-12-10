namespace FluidSimu
{
    public interface IDirectionalElement
    {
        void SetDirection(IPneumaticElement inlet/*, IPneumaticElement outlet*/);
        void RegisterNeighbor(IPneumaticElement neighbor); // Add this
    }

    public class CheckValveElement : BaseElement, IDirectionalElement
    {
        private readonly double _openingDeltaP;

        private IPneumaticElement? _inlet;
        private readonly List<IPneumaticElement> _neighbors = new();
        public CheckValveElement(ElementDto dto, int num) : base(dto, num)
        {
            Type = PneumaticType.checkvalve;
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
            // 1. Connectivity Checks
            if (_inlet == null) return;
            var outlet = _neighbors.FirstOrDefault(n => n.Id != _inlet.Id);
            if (outlet == null) return;

            // 2. Directional Logic (Is the "Diode" forward biased?)
            //    We check pressure diff manually because CheckValves have a "Cracking Pressure"
            if (_inlet.Pressure <= outlet.Pressure + _openingDeltaP)
            {
                LastFlow = 0;
                return; // Valve is closed
            }

            // 3. Flow Calculation
            //    We pass the 'outlet' as the neighbor.
            //    CalculateAndApplyFlow handles the areas and density.
            //    We rely on the physics that if P_inlet > P_outlet, q will be positive.
            CalculateAndApplyFlow(model, outlet);
        }
    }
}