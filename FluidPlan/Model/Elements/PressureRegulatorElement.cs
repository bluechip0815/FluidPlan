using FluidPlan.Helper;
using System.Collections.Generic; // Hinzufügen, falls noch nicht vorhanden

namespace FluidSimu
{
    public class PressureRegulatorElement : ValveElement
    {
        private readonly double _targetPressure;
        private readonly double _kp;
        private readonly double _ki;
        private double _integralError = 0.0;

        public override string ToString() => $"Element #{Id}: {Name} ({Type.ToString()}) [{Connector1} => {Connector2}]";

        public override double LoggableValue => this.IsVisible ? _targetPressure : this.Pressure;

        // NEU: Der Header wird ebenfalls angepasst, um den Sollwert anzuzeigen
        //public override string LoggableHeaderSuffix => this.IsVisible ? "_Setpoint[bar]" : "_P[bar]";

        public PressureRegulatorElement(ElementDto dto, int id, int charge) : base(dto, id, charge)
        {
            Type = PneumaticType.regulator; // Enum muss 'regulator' enthalten
            _targetPressure = ParameterHelper.GetDouble(dto, "pressure", 0.0);
            _kp = ParameterHelper.GetDouble(dto, "kp", 0.5);
            _ki = ParameterHelper.GetDouble(dto, "ki", 5.0);
        }

        public override void SetSchedule(List<FluidPlan.Dto.ValveEventDto> timeline) { }

        public override void UpdateInternalState(PneumaticModel model)
        {
            if (!model.Junctions.TryGetValue(Connector2, out var junctionOut))
            {
                _currentOpeningFactor = 0; // Wenn kein Ausgang da ist, schließen.
                return;
            }

            double currentPressureOut = junctionOut.Pressure;
            double error = _targetPressure - currentPressureOut;
            _integralError += error * model.DeltaT;
            _integralError = Clamp(_integralError, -1.0, 1.0);
            double controlOutput = (_kp * error) + (_ki * _integralError);
            _currentOpeningFactor = Clamp(controlOutput, 0.0, 1.0);
        }

        // --- NEU: Überschriebene Fluss-Logik für unidirektionalen Fluss ---
        public override double CalculateInternalFlow(PneumaticModel model)
        {
            if (!model.Junctions.TryGetValue(Connector1, out var junctionIn) ||
                !model.Junctions.TryGetValue(Connector2, out var junctionOut))
            {
                _lastFlowInternal = 0;
                return 0;
            }

            // Ein Druckregler kann nur arbeiten, wenn der Eingangsdruck HÖHER als der Ausgangsdruck ist.
            // Dies verhindert den physikalisch unmöglichen Rückfluss.
            if (junctionIn.Pressure <= junctionOut.Pressure)
            {
                _lastFlowInternal = 0;
                return 0; // Blockiere den Fluss
            }

            // Wenn die Bedingung erfüllt ist, verwende die normale, bewährte Flussberechnung der Basisklasse.
            return base.CalculateInternalFlow(model);
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}