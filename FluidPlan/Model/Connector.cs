namespace FluidSimu
{
    public class Connector
    {
        private readonly List<IPneumaticElement> _nodes = new();
        public List<IPneumaticElement> Elements => _nodes;

        public bool hasSupply { get; private set; } = false;
        public Connector(List<IPneumaticElement> l, int node)
        {
            if (l.Count < 2)
                throw new ArgumentOutOfRangeException($"Connetion #{node} needs more elements!");

            _nodes = l;
            int cnt = 0;
            int idx = -1;
            for (int i = 0; i < l.Count; i++)
            {
                // CORRECTION: Add PneumaticType.epu to the condition
                if (l[i].Type == PneumaticType.supply ||
                    l[i].Type == PneumaticType.exhaust ||
                    l[i].Type == PneumaticType.epu) // <-- ADD THIS
                {
                    cnt++;
                    idx = i;
                }
            }
            if (cnt > 1)
                throw new ArgumentOutOfRangeException($"Connetion #{node} with {cnt} supplys!");

            if (cnt == 1)
            {
                hasSupply = true;
                if (idx != 0)
                {
                    IPneumaticElement e = _nodes[idx];
                    _nodes.RemoveAt(idx);
                    _nodes.Insert(0, e);
                }
            }
        }
        public string Info()
        {
            string n = "";
            foreach (var o in _nodes)
            {
                if (n != "")
                    n += "; ";
                n += $"{o.Id} ({o.Name})";
            }
            return n;
        }
        internal void CalcFlow(PneumaticModel model)
        {
            if (hasSupply)
                FlowFixPoint(model);
            else
                FlowMultiPoints(model);
        }
        private void FlowFixPoint(PneumaticModel model)
        {
            List<IPneumaticElement> lst = new() { _nodes[0] };
            for (int idx = 1; idx < _nodes.Count; idx++)
            {
                _nodes[idx].CalcFlow(model/*, lst, 0*/);
            }
        }
        // In Connector.cs
        private void FlowMultiPoints(PneumaticModel model)
        {
            // Diese Logik stellt sicher, dass jedes Element (A) mit jedem anderen Element (B) interagiert.
            // Die Logik in A.CalcFlow wird dann entscheiden, ob die Interaktion sinnvoll ist.
            for (int i = 0; i < _nodes.Count; i++)
            {
                // Das aktuelle Element `_nodes[i]` wird seine Strömung mit allen
                // nachfolgenden Elementen in der Liste berechnen.
                _nodes[i].CalcFlow(model/*, _nodes, i + 1*/);
            }
        }
    }
    public record ChargeData
    {
        public int Id = 0;
        public double dQ = 0;
    }
    
}