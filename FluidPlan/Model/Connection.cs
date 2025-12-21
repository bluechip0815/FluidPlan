using FluidSimu;

namespace FluidPlan.Model
{
    /// <summary>
    /// Repräsentiert einen einzelnen Anschluss (einen Port eines Elements),
    /// der mit einem Knotenpunkt (Junction) verbunden ist.
    /// </summary>
    public class Connection
    {
        public IPneumaticElement Item { get; }
        public int Port { get; }
        // Zustand für die Flussglättung
        public double LastVolumeFlow { get; set; } = 0.0;
        public Connection(IPneumaticElement item, int port)
        {
            Item = item;
            Port = port;
        }
        public string Info()
        {
            return $"{Item.Name}.{Port}";
        }

    }
}