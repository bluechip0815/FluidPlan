using FluidPlan.Dto;
using FluidPlan.Helper;

namespace FluidSimu
{
    /// <summary>
    /// Defines an interface for pneumatic elements that can be
    /// controlled externally during an interactive simulation.
    /// </summary>
    public interface IControllable
    {
        /// <summary>
        /// Sets the primary control value for the element.
        /// For a Valve, this could be 0.0 (close) or 1.0 (open).
        /// For an EPU, this would be the target pressure in bar.
        /// </summary>
        /// <param name="value">The control value to set.</param>
        void SetControlValue(double value);
    }
    public class EpuElement : BaseElement, IControllable
    {
        // The connection port area for flow calculation
        private List<EpuEventDto> _schedule = new();
        // The target pressure (pSoll) from the schedule.
        private double _targetPressure = 0.0;
        private readonly double _initialPressure;
        private PropValvePT1 _pt1Model { get; set; } = new();
        private PropValvePT2 _pt2Model { get; set; } = new();
        // Verwende PT2 als Standard
        private bool _usePt2Model = true;
        public EpuElement(ElementDto dto, int id, int charge) : base(dto, id, charge)
        {
            Type = PneumaticType.epu;
            Pressure = ParameterHelper.GetPressure(dto);
            _initialPressure = this.Pressure;
            _targetPressure = _initialPressure;

            _pt1Model = new PropValvePT1(
                // The time constant (T) for the first-order response.
                // Default: 100ms
                ParameterHelper.GetDouble(dto, "timeConstant", 0.1),
                // The maximum rate of pressure change (dp/dt).
                // Default: 25 bar/s
                ParameterHelper.GetDouble(dto, "maxDpDt", 25.0));

            // Good defaults might be wn=20 (fast) and
            // zeta=0.7 (slightly underdamped for an S-curve).
            _pt2Model = new PropValvePT2(
                ParameterHelper.GetDouble(dto, "naturalFrequency", 20.0),
                ParameterHelper.GetDouble(dto, "dampingRatio", 0.7));
            // Was nutzen wir?
            _usePt2Model = ParameterHelper.GetBool(dto, "usePt2Model", true);
        }
        public override string ToString() => $"Element #{Id}: {Name} ({Type.ToString()}) [{Connector1} => {Connector2}]";

        /// <summary>
        /// Implements IControllable to set the target pressure directly.
        /// </summary>
        public void SetControlValue(double value) => _targetPressure = value;
        public override double LoggableValue
        {
            get
            {
                return this.IsVisible ? _targetPressure : this.Pressure;
            }
        }
        public void SetSchedule(List<EpuEventDto> timeline)
        {
            _schedule = timeline.OrderBy(x => x.TimeSeconds).ToList();
        }
        // --- Logik zum Ermitteln des Zieldrucks ---
        public override void UpdateInternalState(PneumaticModel model)
        {
            if (model.IsInteractive) return;
            if (_schedule.Count == 0) return;

            // Beginne mit dem Initialdruck als Standard-Sollwert.
            // Dieser wird beibehalten, bis das erste Event erreicht wird.
            double newTarget = _initialPressure;

            // Finde den letzten Sollwert, dessen Zeit abgelaufen ist.
            foreach (var item in _schedule)
            {
                if (item.TimeSeconds <= model.CurrentTime)
                {
                    newTarget = item.TargetPressure;
                }
                else
                {
                    // Da die Liste sortiert ist, können wir hier abbrechen.
                    break;
                }
            }

            _targetPressure = newTarget;
        }
        public override double CalcPressure(PneumaticModel model)
        {
            // Aktualisiere den aktuellen Druck basierend auf dem Regelungsmodell.
            // Dies ignoriert model.GetCharge() vollständig, da eine Quelle ihren
            // Druck selbst definiert.
            if (_usePt2Model)
            {
                var (newPressure, newVelocity) = _pt2Model.Update(
                    currentValue: this.Pressure,
                    currentVelocity: _pt2Model.PressureVelocity,
                    targetValue: _targetPressure,
                    dt: model.DeltaT
                );
                this.Pressure = newPressure;
                _pt2Model.PressureVelocity = newVelocity;
            }
            else
            {
                this.Pressure = _pt1Model.UpdatePressure(
                    currentPressure: this.Pressure,
                    setPoint: _targetPressure,
                    dt: model.DeltaT
                );
            }
            // Wie bei einem SupplyElement ist die interne Druckänderung einer Quelle
            // kein Maß für die Stabilität des Netzwerks. Gib 0 zurück.
            return 0;
        }
    }
}