namespace FluidSimu
{
    /// <summary>
    /// Represents the physical connection port of a pneumatic element.
    /// This class holds the geometric properties that determine flow resistance.
    /// </summary>
    public class Port
    {
        /// <summary>
        /// The cross-sectional area of the port in square meters.
        /// This is the primary property used for flow calculations.
        /// </summary>
        public double Area { get; }

        public Port(double diameter=0)
        {
            if (diameter <= 0)
            {
                // A zero or negative diameter port has zero area and allows no flow.
                Area = 0;
            }
            else
            {
                Area = Math.PI / 4 * diameter * diameter;
            }
        }
    }
}
