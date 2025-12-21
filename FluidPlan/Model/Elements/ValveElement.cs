using FluidPlan.Dto;
using FluidPlan.Helper;

namespace FluidSimu
{
    public class ValveElement : BaseElement, IControllable
    {
        // Port 1 verwendet die geerbten Eigenschaften: this.Pressure, this.Volume
        public double PressurePort2 { get; protected set; }
        protected double VolumePort2 { get; private set; }
        // Getrennte "LastFlow" Zustände für jeden Flusspfad ---
        protected double _lastFlowInternal = 0.0;
        // Bestehende Eigenschaften für die Ventilsteuerung
        // Ventil-Schaltzeit selbst(intern) liegt oft im Bereich von etwa 10–50 ms, je nach Typ.
        //private readonly double SwtichingTime;
        // The state (0 or 1) the valve is moving TOWARDS.
        private double _commandedState = 0.0;
        // The simulation time when the last command was received.
        private double _lastStateChangeTime = -1.0;
        // The actual, current opening percentage of the valve (0.0 to 1.0).
        protected double _currentOpeningFactor = 0.0; // protected damit Ableitungen es sehen
        public override double LoggableValue => _currentOpeningFactor;
        public ValveElement(ElementDto dto, int id, int charge) : base(dto, id, charge)
        {
            Type = PneumaticType.valve;
            
            double length = ParameterHelper.GetLength(dto);
            if (length == 0)
                length = 0.03; // 3cm
            
            double portVolume = this.Area * length;
            if (portVolume < 1e-9)
                portVolume = 1e-9;

            this.Volume = portVolume; // Geerbtes Volume für Port 1
            this.VolumePort2 = portVolume;

            // Startdruck für beide Ports setzen
            this.Pressure = ParameterHelper.GetPressure(dto);
            this.PressurePort2 = this.Pressure;
        }
        // Das ist die einzige Aufgabe dieser Methode:
        // Den Öffnungsgrad für den aktuellen Zeitschritt berechnen.
        public override void UpdateInternalState(PneumaticModel model)
        {
            UpdateCurrentOpening(model.CurrentTime);
        }
        /// <summary>
        /// Implements IControllable to set the commanded state (0 or 1).
        /// </summary>
        public void SetControlValue(double value)
        {
            // A new command is issued. The transition will start on the next Execute step.
            if (Math.Abs(value - _commandedState) > 1e-6)
            {
                _commandedState = value;
                _lastStateChangeTime = -1; // Reset to signal a new transition should start
            }
        }
        // Method to inject the schedule after creation
        // Internal storage for the schedule
        private List<ValveEventDto> _schedule = new();
        public virtual void SetSchedule(List<ValveEventDto> timeline)
        {
            // Sort to ensure efficiency
            _schedule = timeline.OrderBy(x => x.TimeSeconds).ToList();
        }
        private void UpdateCurrentOpening(double simulationTime)
        {
            // === SCHRITT 1: Finde den Soll-Zustand laut Zeitplan ===
            double targetState = 0.0; // Standardmäßig geschlossen
            if (_schedule.Any())
            {
                // Finde den letzten Befehl, dessen Zeit abgelaufen ist
                foreach (var item in _schedule)
                {
                    if (item.TimeSeconds <= simulationTime)
                    {
                        targetState = item.State;
                    }
                    else
                    {
                        break; // Zukünftige Befehle ignorieren
                    }
                }
            }

            // === SCHRITT 2: Prüfe, ob ein neuer Befehl erteilt wurde ===
            // (und behandle den allerersten Befehl)
            if (Math.Abs(targetState - _commandedState) > 1e-6 || _lastStateChangeTime < 0)
            {
                // Sonderfall: Wenn der allererste Befehl kommt (t=0.5s) und der Zielzustand
                // derselbe ist wie der Startzustand (beide 0), müssen wir nichts tun.
                if (_lastStateChangeTime < 0 && Math.Abs(targetState) < 1e-6)
                {
                    _commandedState = 0.0;
                    _currentOpeningFactor = 0.0;
                    return; // Bleibt geschlossen, keine Transition nötig
                }

                // Ein echter Zustandswechsel hat stattgefunden
                _commandedState = targetState;
                // Finde die exakte Zeit des letzten Befehls für den Transition-Start
                _lastStateChangeTime = _schedule.LastOrDefault(e => e.TimeSeconds <= simulationTime)?.TimeSeconds ?? simulationTime;
            }

            // === SCHRITT 3: Berechne den aktuellen Öffnungsgrad ===
            if (_lastStateChangeTime < 0)
            {
                // Fall: Vor dem allerersten Befehl im Zeitplan.
                _currentOpeningFactor = 0.0;
            }
            else
            {
                // Wir befinden uns in oder nach einer Transition
                double timeSinceCommand_ms = (simulationTime - _lastStateChangeTime) * 1000.0;
                double alpha = FlowPhysics.GetValveTransitionAlpha(timeSinceCommand_ms);

                // KORRIGIERTE LOGIK:
                if (_commandedState > 0.5) // Ziel ist "Öffnen"
                {
                    // Eine öffnende Transition geht von 0.0 nach 1.0
                    _currentOpeningFactor = alpha;
                }
                else // Ziel ist "Schließen"
                {
                    // Eine schließende Transition geht von 1.0 nach 0.0
                    _currentOpeningFactor = 1.0 - alpha;
                }
            }
        }
        // === PHASE 2 LOGIK ===
        public virtual double CalculateInternalFlow(PneumaticModel model)
        {
            if (_currentOpeningFactor <= 1e-9)
            {
                _lastFlowInternal = 0;
                    return 0;
            }
            // Finde die Knotenpunkte (Junctions), an die dieses Ventil angeschlossen ist.
            // Die IDs sind in Connector1 und Connector2 gespeichert.
            if (!model.Junctions.TryGetValue(this.Connector1, out var junction1) ||
                !model.Junctions.TryGetValue(this.Connector2, out var junction2))
            {
                // Ventil ist nicht an beiden Seiten korrekt angeschlossen, kein Fluss möglich.
                _lastFlowInternal = 0;
                return 0;
            }

            // NEU: Verwende die Drücke der externen Junctions, nicht die internen Drücke des Ventils!
            double p1 = junction1.Pressure; // Druck VOR dem Ventil
            double p2 = junction2.Pressure; // Druck NACH dem Ventil

            // Der effektive Querschnitt wird durch den Öffnungsgrad des Ventils bestimmt.
            double bottleneckArea = this.Area * _currentOpeningFactor;
            if (bottleneckArea <= 1e-9)
            {
                _lastFlowInternal = 0;
                return 0;
            }


            // Berechne den geglätteten Volumenstrom basierend auf den Drücken der Junctions.
            double q_internal = FlowPhysics.ComputeSmoothedVolumeFlow(
                p1, p2,
                bottleneckArea,
                FlowCoefficient,
                _lastFlowInternal,
                model.DeltaT
            );

            _lastFlowInternal = q_internal;

            // Bestimme den Quell-Druck für die Ladungsberechnung.
            double pSource = (q_internal > 0) ? p1 : p2;
            double qCharge_internal = FlowPhysics.VolumeFlowToChargeFlow(q_internal, pSource);

            // Dies ist der entscheidende Schritt:
            // Die Ladung wird direkt zwischen den internen Akkumulatoren der beiden Ports verschoben.
            // In Phase 4 wird dann der Druck dieser Ports basierend auf dem Fluss von/zu den Junctions
            // UND diesem internen Fluss aktualisiert.
            model.AddCharge(this.ChargeIndex, -qCharge_internal * model.DeltaT);     // Ladung von Port 1 wegnehmen
            model.AddCharge(this.ChargeIndex + 1, +qCharge_internal * model.DeltaT); // Ladung zu Port 2 hinzufügen

            return q_internal;

        }
        public override double CalcPressure(PneumaticModel model)
        {
            // --- Defensive Programmierung: NaN-Check ---
            if (double.IsNaN(Pressure) || double.IsNaN(PressurePort2) ||
                double.IsInfinity(Pressure) || double.IsInfinity(PressurePort2))
            {
                throw new InvalidOperationException($"FATAL: Pressure became invalid in {Name} at T={model.CurrentTime:F4}s. P1={Pressure}, P2={PressurePort2}. Simulation halted.");
            }
            // Port 1
            double oldP1 = this.Pressure;
            double charge1 = (oldP1 * this.Volume) + model.GetCharge(this.ChargeIndex);
            this.Pressure = charge1 / this.Volume;
            if (this.Pressure < 0) this.Pressure = 0; // Wichtiger Schutz gegen negativen Druck

            // Port 2
            double oldP2 = this.PressurePort2;
            double charge2 = (oldP2 * this.VolumePort2) + model.GetCharge(this.ChargeIndex + 1);
            this.PressurePort2 = charge2 / this.VolumePort2;
            if (this.PressurePort2 < 0) this.PressurePort2 = 0;

            return Math.Max(Math.Abs(this.Pressure - oldP1), Math.Abs(this.PressurePort2 - oldP2));
        }
    }
}

