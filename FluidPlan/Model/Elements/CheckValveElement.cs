using FluidPlan.Helper;
using System.Reflection;

namespace FluidSimu
{
    public class CheckValveElement : ValveElement //, IDirectionalElement
    {
        private readonly double _openingDeltaP;
        public CheckValveElement(ElementDto dto, int id, int charge) : base(dto, id, charge)
        {
            Type = PneumaticType.checkvalve;
            _openingDeltaP = ParameterHelper.GetDouble(dto, "openingdeltap", 0.05);
            _currentOpeningFactor = 1.0;
        }
        public override string ToString()
        {
            return $"Element #{Id}: {Name} ({Type.ToString()}) [{Connector1} => {Connector2}]";
        }
        public override void UpdateInternalState(PneumaticModel model)
        {
            // Deaktivieren, damit es nicht auf einen Zeitplan reagiert.
        }
        public override double CalculateInternalFlow(PneumaticModel model)
        {
            // Schritt 1: Finde die Knotenpunkte (Junctions), an die das Ventil angeschlossen ist.
            if (!model.Junctions.TryGetValue(this.Connector1, out var junction1) ||
                !model.Junctions.TryGetValue(this.Connector2, out var junction2))
            {
                _lastFlowInternal = 0;
                return 0; // Nicht korrekt verbunden
            }

            // Schritt 2: Hole die externen Drücke von den Junctions.
            double p1_external = junction1.Pressure; // Druck vor dem Ventil (Flussrichtung 1 -> 2)
            double p2_external = junction2.Pressure; // Druck nach dem Ventil

            // Schritt 3: Die entscheidende Bedingung für ein Rückschlagventil.
            // Prüfe, ob der Druck in Flussrichtung groß genug ist, um das Ventil zu öffnen.
            if (p1_external > p2_external + _openingDeltaP)
            {
                // Ventil ist offen: Berechne den Fluss von Junction 1 nach Junction 2.
                // Der _currentOpeningFactor ist bei einem Rückschlagventil effektiv 1.0, wenn es offen ist.
                double bottleneckArea = this.Area * _currentOpeningFactor;

                double q_internal = FlowPhysics.ComputeSmoothedVolumeFlow(
                    p1_external, p2_external,
                    bottleneckArea,
                    FlowCoefficient,
                    _lastFlowInternal,
                    model.DeltaT
                );

                _lastFlowInternal = q_internal;

                double pSource = (q_internal > 0) ? p1_external : p2_external;
                double qCharge_internal = FlowPhysics.VolumeFlowToChargeFlow(q_internal, pSource);

                // Verschiebe die Ladung intern von Port 1 nach Port 2.
                model.AddCharge(this.ChargeIndex, -qCharge_internal * model.DeltaT);
                model.AddCharge(this.ChargeIndex + 1, +qCharge_internal * model.DeltaT);

                return q_internal;
            }
            else
            {
                // Ventil ist geschlossen: Kein Fluss.
                _lastFlowInternal = 0;
                return 0;
            }
        }
    }
}
