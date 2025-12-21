namespace FluidSimu
{
    /// <summary>
    /// Einfache Modellierung eines Proportional-Druckreglers / -ventils.
    ///
    /// Idee:
    /// 1) Erste Ordnung (T-Verhalten):
    ///    p_target(t + Δt) = p_ist(t) + (Δt / T) * (p_soll(t) - p_ist(t))
    ///
    ///    - T  = Zeitkonstante der Regelstrecke (z.B. 0.1 s)
    ///    - Δt = Zeitschritt in Sekunden
    ///
    ///    Damit läuft p_ist(t) mit Verzögerung auf p_soll(t) zu.
    ///
    /// 2) Begrenzung der maximalen Druckänderung (physikalische Approximation):
    ///
    ///    Δp      = p_target - p_ist
    ///    Δp_max  = (dp/dt)_max * Δt
    ///    Δp_eff  = clamp(Δp, -Δp_max, +Δp_max)
    ///    p_neu   = p_ist + Δp_eff
    ///
    ///    - (dp/dt)_max in bar/s (z.B. 30 bar/s)
    ///    - Δp_eff wird auf eine maximal zulässige Druckänderung pro Zeitschritt begrenzt.
    ///
    /// Ergebnis:
    /// - p_ist nähert sich p_soll mit einem T-Verhalten,
    ///   wird aber durch eine maximale Änderungsrate begrenzt.
    /// - Gut geeignet als einfache, stabile Approximation für Proportionalventile.
    /// </summary>
    public class PropValvePT1
    {
        /// <summary>
        /// Aktualisiert den Ist-Druck pIst um einen Zeitschritt Δt (in Sekunden),
        /// basierend auf einem T-Verhalten erster Ordnung und einer begrenzten Druckänderungsrate.
        ///
        /// Formeln:
        ///   p_target = pIst + (dt / timeConstant) * (pSoll - pIst)
        ///   deltaP   = p_target - pIst
        ///   deltaPMax = maxDpDt * dt
        ///   deltaPEff = clamp(deltaP, -deltaPMax, +deltaPMax)
        ///   pNeu     = pIst + deltaPEff
        ///
        /// Hinweise:
        /// - pIst und pSoll in bar.
        /// - dt in Sekunden.
        /// - timeConstant T in Sekunden (typisch 0.05 .. 0.5 s).
        /// - maxDpDt in bar/s (z.B. 20 .. 50 bar/s).
        /// </summary>
        /// <param name="pIst">Aktueller Ist-Druck in bar.</param>
        /// <param name="pSoll">Soll-Druck in bar.</param>
        /// <param name="dt">Zeitschritt Δt in Sekunden.</param>
        /// <param name="timeConstant">Zeitkonstante T der Druckregelung in Sekunden.</param>
        /// <param name="maxDpDt">Maximale Druckänderung (dp/dt)_max in bar pro Sekunde.</param>
        /// <returns>Neuer Ist-Druck pNeu in bar nach dem Zeitschritt Δt.</returns>
        public double TimeConstant { get; private set; } = 0.1;
        public double MaxDpDt { get; private set; } = 20.0;
        public PropValvePT1(double timeConstant=0.1, double maxDpDt = 25.0)
        {
            TimeConstant = timeConstant;
            MaxDpDt = maxDpDt;

            if (TimeConstant <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(TimeConstant), "timeConstant must be > 0.");

            if (MaxDpDt <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(MaxDpDt), "maxDpDt must be > 0.");
        }
        /// <summary>
        /// </summary>
        /// <param name="currentPressure">Aktueller Ist-Druck in bar.</param>
        /// <param name="setPoint">Soll-Druck in bar.</param>
        /// <param name="dt">Zeitschritt Δt in Millisekunden.</param>
        /// <returns>Neuer Ist-Druck pNeu in bar.</returns>

        public double UpdatePressure(double currentPressure, double setPoint, double dt)
        {
            if (dt <= 0.0)
                return currentPressure;


            // 1) Erste Ordnung: Ziel-Druck ohne Begrenzung
            //    p_target = pIst + (dt / T) * (pSoll - pIst)
            double pTarget = currentPressure + (dt / TimeConstant) * (setPoint- currentPressure);

            // 2) Begrenzung der Druckänderung
            //    deltaP    = p_target - pIst
            //    deltaPMax = maxDpDt * dt
            //    deltaPEff = clamp(deltaP, -deltaPMax, +deltaPMax)
            double deltaP = pTarget - currentPressure;
            double deltaPMax = MaxDpDt * dt;

            double deltaPEff = Clamp(deltaP, -deltaPMax, deltaPMax);

            // 3) Neuer Ist-Druck
            double pNeu = currentPressure + deltaPEff;

            return pNeu;
        }

        /// <summary>
        /// Hilfsfunktion: clamp(value, min, max)
        /// </summary>
        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
