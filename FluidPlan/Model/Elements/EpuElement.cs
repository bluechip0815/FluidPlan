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
        private readonly double Area; 
        // The connection port area for flow calculation
        private List<EpuEventDto> _schedule = new();
        private readonly List<IPneumaticElement> _neighbors = new();
        public void RegisterNeighbor(IPneumaticElement neighbor)
        {
            if (!_neighbors.Contains(neighbor))
                _neighbors.Add(neighbor);
        }
        // The target pressure (pSoll) from the schedule.
        private double _targetPressure = 0.0;
        public IReadOnlyList<IPneumaticElement> Neighbors => _neighbors;
        private PropValvePT1 _pt1Model { get; set; } = new();
        private PropValvePT2 _pt2Model { get; set; } = new();
        public EpuElement(ElementDto dto, int num) : base(dto, num)
        {
            Type = PneumaticType.epu;

            // Define the connection port size for the EPU
            double diameter = ParameterHelper.GetDiameter(dto);
            if (diameter == 0) diameter = 0.02; // Default to 2cm if not specified
            this.Area = Math.PI / 4 * diameter * diameter;

            Pressure = ParameterHelper.GetPressure(dto);

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
        }
        /// <summary>
        /// Implements IControllable to set the target pressure directly.
        /// </summary>
        public void SetControlValue(double value)
        {
            _targetPressure = value;
        }
        public void SetSchedule(List<EpuEventDto> timeline)
        {
            _schedule = timeline.OrderBy(x => x.TimeSeconds).ToList();
        }
        private double GetCurrentTargetPressure(double time)
        {
            if (_schedule.Count == 0) return 0.0;

            double target = 0.0;
            foreach (var item in _schedule)
            {
                if (item.TimeSeconds <= time)
                    target = item.TargetPressure;
                else
                    break;
            }
            return target;
        }
        protected override void DoStep(PneumaticModel model, IPneumaticElement otherNode)
        {
            // The pressure "From" is this EPU's internally regulated pressure.
            double pFrom = this.Pressure;
            // The pressure "To" is the neighbor's pressure.
            double pTo = otherNode.Pressure;

            // Calculate flow through our connection port.
            double q = FlowPhysics.ComputeVolumeFlow(pFrom, pTo, this.Area);
            double pMean = 0.5 * (pFrom + pTo);
            double qCharge = FlowPhysics.VolumeFlowToChargeFlow(q, pMean);
            double currentQ = qCharge * model.DeltaT;

            // The neighbor GAINS charge from us, and we LOSE charge.
            // Note: Our own pressure is recalculated by the PT2 model, so this dQ for our own Id
            // will be ignored, which is correct for a perfect regulator. But we still calculate it
            // for physical consistency.
            model.AddCharge(new ChargeData() { Id = this.Id, dQ = -currentQ });
            model.AddCharge(new ChargeData() { Id = otherNode.Id, dQ = +currentQ });
        }
        public override double CalcPressure(PneumaticModel model)
        {
            return CalcPressurePt2(model);
        }
        private double CalcPressurePt2(PneumaticModel model)
        {
            double oldPressure = Pressure;
            double targetPressure = GetCurrentTargetPressure(model.CurrentTime);

            // --- Use the new PT2 model to update both pressure and pressure-velocity ---
            var (newPressure, newVelocity) = _pt2Model.Update(
                currentValue: this.Pressure,
                currentVelocity: _pt2Model.PressureVelocity,
                targetValue: targetPressure,
                dt: model.DeltaT
            );

            Pressure = newPressure;
            _pt2Model.PressureVelocity = newVelocity;

            return Math.Abs(this.Pressure - oldPressure);
        }

        private double CalcPressurePt1(PneumaticModel model)
        {
            double oldPressure = Pressure;

            // 1. Get the target pressure for the current time from the schedule.
            // (Note: We pass model.CurrentTime, which you'll need to expose or pass down)
            // For now, let's assume we can get the time from the model execution context.
            // Let's get it from the PneumaticModel

            // For this to work, we need to know the current simulation time.
            // Let's modify the interface slightly.
            // In IPneumaticElement: double CalcPressure(PneumaticModel model, double timeSeconds);
            // In BaseElement: public virtual double CalcPressure(PneumaticModel model, double timeSeconds)
            // And in PneumaticModel.Execute: element.CalcPressure(this, timeSeconds);

            // Assuming that change is made:
            _targetPressure = GetCurrentTargetPressure(model.CurrentTime); // You'll need to add CurrentTime to the model

            // 2. Use the ProportionalValveModel to calculate the new pressure for this time step.
            Pressure = _pt1Model.UpdatePressure(currentPressure: oldPressure, setPoint: _targetPressure, dt: model.DeltaT);

            // 3. Return the absolute change in pressure.
            return Math.Abs(this.Pressure - oldPressure);
        }
    }
}