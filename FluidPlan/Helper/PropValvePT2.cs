namespace FluidSimu
{
    public class PropValvePT2
    {
        private readonly double _naturalFrequency;
        private readonly double _dampingRatio;

        // The squared frequency is used repeatedly, so we pre-calculate it.
        private readonly double _wn2;
        private readonly double _two_zeta_wn;
        public double PressureVelocity { get; set; } = 0.0;
        public PropValvePT2(double naturalFrequency=20.0, double dampingRatio=0.7)
        {
            if (naturalFrequency <= 0) throw new ArgumentOutOfRangeException(nameof(naturalFrequency));
            if (dampingRatio < 0) throw new ArgumentOutOfRangeException(nameof(dampingRatio));

            _naturalFrequency = naturalFrequency;
            _dampingRatio = dampingRatio;

            _wn2 = _naturalFrequency * _naturalFrequency;
            _two_zeta_wn = 2 * _dampingRatio * _naturalFrequency;
        }

        /// <summary>
        /// Updates the state of the second-order system over one time step.
        /// </summary>
        /// <param name="currentValue">The current position/value of the system (e.g., pressure).</param>
        /// <param name="currentVelocity">The current rate of change of the value (e.g., pressure velocity).</param>
        /// <param name="targetValue">The setpoint the system is trying to reach.</param>
        /// <param name="dt">The simulation time step in seconds.</param>
        /// <returns>A tuple containing the new value and the new velocity.</returns>
        public (double newValue, double newVelocity) Update(
            double currentValue,
            double currentVelocity,
            double targetValue,
            double dt)
        {
            // The core of the PT2 model is the second-order ordinary differential equation:
            // d²x/dt² + 2ζωn * dx/dt + ωn² * x = ωn² * target
            // We rearrange to solve for the acceleration (d²x/dt²):
            // acceleration = ωn² * (target - x) - 2ζωn * (dx/dt)

            double error = targetValue - currentValue;
            double acceleration = _wn2 * error - _two_zeta_wn * currentVelocity;

            // Now, we integrate over the time step (using simple Euler integration)
            // 1. Update the velocity using the acceleration.
            double newVelocity = currentVelocity + acceleration * dt;

            // 2. Update the position (value) using the NEW velocity.
            double newValue = currentValue + newVelocity * dt;

            return (newValue, newVelocity);
        }
    }
}
