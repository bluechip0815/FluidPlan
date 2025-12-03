namespace FluidSimu
{
    public static partial class FlowPhysics
    {
        public const double Rho = 1.2;
        public const double DeltaPCritical = 0.5;

        public static double ComputeVolumeFlow(double pUp, double pDown, double area, double flowCoefficient)
        {
            double dp = pUp - pDown;
            if (Math.Abs(dp) < 1e-9 || area <= 0.0)
                return 0.0;

            double sign = Math.Sign(dp);
            double absDp = Math.Abs(dp);
            double q;

            double kLinear = flowCoefficient / Math.Sqrt(DeltaPCritical);

            if (absDp < DeltaPCritical)
            {
                // Now this connects smoothly to the sqrt curve
                q = kLinear * area * absDp;
            }
            else
            {
                q = flowCoefficient * area * Math.Sqrt(absDp);
            }

            return sign * q;
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
