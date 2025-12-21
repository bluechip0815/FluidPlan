namespace FluidSimu
{
    public class Connector
    {
        private readonly List<IPneumaticElement> _nodes = new();
        public List<IPneumaticElement> Elements => _nodes;
        public Connector(List<IPneumaticElement> l, int node)
        {
            // Sanity check: A connection needs at least 2 points to allow flow.
            if (l.Count < 2)
                throw new ArgumentOutOfRangeException($"Connection #{node} needs at least 2 elements!");

            // Optional: Ensure no duplicates (e.g., connecting a Valve to itself)
            if (l.Distinct().Count() != l.Count)
                throw new ArgumentException($"Connection #{node} contains duplicate elements!");

            _nodes = l;

        }
        public string Info()
        {
            return string.Join(", ", _nodes.Select(e => e.Name));
        }
    }
}