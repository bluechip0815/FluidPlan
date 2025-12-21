using FluidSimu;

namespace FluidPlan.Model
{
    /// <summary>
    /// Repräsentiert einen physikalischen Knotenpunkt im Netzwerk.
    /// Sein Zweck ist es, in jedem Zeitschritt den Gleichgewichtsdruck zu finden,
    /// bei dem die Summe aller zu- und abfließenden Ströme Null ist.
    /// </summary>
    public class Junction
    {
        public int Id { get; }
        public List<Connection> ConnectedPorts { get; } = new();

        // Der Druck am Knotenpunkt. Wird in jedem Zeitschritt neu berechnet.
        public double Pressure { get; private set; }

        public string Info()
        {
            string text = "";
            foreach (Connection conn in ConnectedPorts)
            {
                if (text != "")
                    text += ", ";
                text += conn.Info();

            }
            return $"Node #{Id}: " + text;
        }
        public Junction(int id)
        {
            Id = id;
        }

        public void AddConnection(Connection connection)
        {
            ConnectedPorts.Add(connection);
        }

        /// <summary>
        /// Der einfache iterative Solver zur Bestimmung des Knotendrucks.
        /// </summary>
        public void SolvePressure(PneumaticModel model)
        {
            if (ConnectedPorts.Count <= 1)
            {
                Pressure = ConnectedPorts.FirstOrDefault()?.Item.Pressure ?? 0;
                return;
            }

            // --- SCHRITT 0: ADAPTIVEN DÄMPFUNGSFAKTOR BERECHNEN ---
            // Finde die maximale Druckdifferenz über den gesamten Knotenpunkt.
            var pressures = ConnectedPorts.Select(c => GetPressureFromPort(c.Item, c.Port)).ToList();
            double maxPressureDiff = pressures.Max() - pressures.Min();

            // Definiere die Grenzen für unsere Dämpfung.
            const double maxDamping = 0.1;   // Für große Druckdifferenzen
            const double minDamping = 0.005; // Für winzige Druckdifferenzen (Stabilität)

            // Definiere eine Skala. Sagen wir, eine Differenz von 1.0 bar ist "groß".
            const double pressureScale = 1.0;

            // Lineare Interpolation: Skaliere die Druckdifferenz auf einen Wert zwischen 0 und 1.
            double t = Math.Min(1.0, maxPressureDiff / pressureScale);

            // Berechne den Dämpfungsfaktor für diesen Zeitschritt.
            double adaptiveDamping = minDamping + (maxDamping - minDamping) * t;

            // --- Schritt 1: Startschätzung für den Druck ---
            // Der Durchschnittsdruck aller verbundenen Elemente ist ein guter Startwert.
            double pGuess = ConnectedPorts.Average(c => GetPressureFromPort(c.Item, c.Port));

            const int maxIterations = 10; // Feste Anzahl an Iterationen als Kompromiss

            // --- Schritt 2: Iterative Anpassung ---
            for (int i = 0; i < maxIterations; i++)
            {
                double sumOfChargeFlows = 0;

                // Berechne die Summe der Ladungsströme für den aktuellen pGuess
                foreach (var connection in ConnectedPorts)
                {
                    double pElement = GetPressureFromPort(connection.Item, connection.Port);
                    double area = connection.Item.Area; // Annahme: Der Port hat die Fläche des Elements

                    // Wichtig: Die Flussrichtung bestimmt den Quell-Druck für die Ladungsberechnung
                    //double flow = FlowPhysics.ComputeVolumeFlow(pElement, pGuess, area, connection.Item.FlowCoefficient);

                    // Verwende die geglättete Berechnung anstelle der "rohen" Berechnung
                    double flow = FlowPhysics.ComputeSmoothedVolumeFlow(
                        pElement,
                        pGuess,
                        area,
                        connection.Item.FlowCoefficient,
                        connection.LastVolumeFlow, // Verwende den letzten bekannten Fluss
                        model.DeltaT
                    );

                    double pSource = (flow > 0) ? pElement : pGuess;
                    sumOfChargeFlows += FlowPhysics.VolumeFlowToChargeFlow(flow, pSource);
                }

                // --- Schritt 3: Anpassung der Druckschätzung ---
                if (Math.Abs(sumOfChargeFlows) < 1e-6)
                    break; // Konvergenz erreicht

                // Simple Anpassung: Wenn Summe > 0 (Netto-Zufluss), muss der Gegendruck (pGuess) steigen.
                // Der Dämpfungsfaktor stabilisiert die Berechnung.
                pGuess += sumOfChargeFlows * adaptiveDamping;
                if (pGuess < 0) pGuess = 0;
            }

            this.Pressure = pGuess;
        }

        // --- Private Hilfsfunktionen ---

        private static double GetPressureFromPort(IPneumaticElement element, int port)
        {
            if (element is ValveElement v && port == 2)
            {
                return v.PressurePort2;
            }
            return element.Pressure;
        }
    }
}