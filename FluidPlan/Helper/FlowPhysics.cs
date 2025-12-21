using FluidPlan.Dto;

namespace FluidSimu
{
    public static class FlowPhysics
    {

        private const double BarToPascal = 100000.0; // Conversion factor
        public static double CriticalPressureDelta { get; private set; } = 0.5;
        public static double SmoothingTimeConstant { get; private set; } = 0.0001;
        public static double Rho { get; private set; } = 1.2; // Density of Air [kg/m^3]
        /// Initialisiert die statischen Physik-Parameter aus der Konfigurationsdatei.
        /// Muss einmal beim Start der Simulation aufgerufen werden.
        /// </summary>
        public static void Initialize(PhysicsParametersDto? parameters)
        {
            // Wenn keine Parameter in der JSON-Datei angegeben sind, werden die Standardwerte beibehalten.
            if (parameters == null)
            {
                Console.WriteLine("INFO: No custom physics parameters found in profile. Using default values.");
                return;
            }

            // Werte aus der DTO-Klasse in die statischen Felder übernehmen.
            CriticalPressureDelta = parameters.CriticalPressureDelta;
            SmoothingTimeConstant = parameters.SmoothingTimeConstant;
            Rho = parameters.AirDensityRho;

            Console.WriteLine("INFO: Custom physics parameters loaded successfully:");
            Console.WriteLine($"  - Smoothing Time Constant: {SmoothingTimeConstant} s");
            Console.WriteLine($"  - Air Density (Rho): {Rho} kg/m^3");
        }

        public static double ComputeVolumeFlow(double pUp, double pDown, double area, double flowCoefficient)
        {
            double dp = pUp - pDown;

            // 1. Check for equilibrium
            if (Math.Abs(dp) < 1e-9 || area <= 0.0)
                return 0.0;

            double sign = Math.Sign(dp);
            double absDpBar = Math.Abs(dp);

            // 2. Convert Bar to Pascal for Physics Calculation
            // Bernoulli Equation: v = Sqrt(2 * DeltaP / Rho)
            // We use the absolute pressure difference in Pascals.
            double dpPascal = absDpBar * BarToPascal;

            // 3. Calculate Velocity [m/s]
            // This naturally produces the ~408 factor properly derived from physics.
            double velocity = Math.Sqrt((2.0 * dpPascal) / Rho);

            // 4. Calculate Volume Flow [m^3/s]
            // Q = Area * Velocity * FlowCoefficient (Cd)
            double q = area * velocity * flowCoefficient;

            // Optional: Choked Flow Check (Speed of Sound limit)
            // Air cannot move faster than approx 340 m/s at room temp.
            if (velocity > 340.0)
            {
                q = area * 340.0 * flowCoefficient;
            }

            return sign * q;
        }
        public static double ComputeSmoothedVolumeFlow(double pUp, double pDown, double area, double flowCoefficient, double lastFlow, double timeStep)
        {
            // 1. Calculate the "raw" instantaneous flow based on current pressures
            double rawFlow = ComputeVolumeFlow(pUp, pDown, area, flowCoefficient);

            // 2. Apply a simple low-pass filter (exponential smoothing)
            // The 'alpha' value determines how much weight is given to the new rawFlow.
            // A smaller alpha makes the flow smoother.
            // We can tie this to the timeStep to make it somewhat independent of the simulation frequency.
            double alpha = 1.0 - Math.Exp(-timeStep / SmoothingTimeConstant); // 5ms time constant for smoothing

            double smoothedFlow = alpha * rawFlow + (1.0 - alpha) * lastFlow;

            return smoothedFlow;
        }

        public static double VolumeFlowToChargeFlow(double volumeFlow, double meanPressureBar)
        {
            return volumeFlow * meanPressureBar;
        }
        /// <summary>
        /// Calculates a smooth valve opening percentage (0.0 to 1.0) based on an S-curve.
        /// </summary>
        /// <param name="timeSinceActuation_ms">Time elapsed since the valve was commanded to open/close.</param>
        /// <param name="transitionTime_ms">The total time the valve takes to fully open or close.</param>
        /// <returns>An opening factor from 0.0 (closed) to 1.0 (fully open).</returns>
        public static double GetValveTransitionAlpha(double timeSinceActuation_ms, double transitionTime_ms = 20.0)
        {
            if (timeSinceActuation_ms <= 0.0)
                return 0.0;

            if (timeSinceActuation_ms >= transitionTime_ms)
                return 1.0; // CORRECTED: Return 1.0 for fully open, not 100.0

            // S-Kurve: 0.5 * (1 - cos(pi * t / t_open))
            double alpha = 0.5 * (1.0 - Math.Cos(Math.PI * timeSinceActuation_ms / transitionTime_ms));

            return alpha;
        }
    }

}
