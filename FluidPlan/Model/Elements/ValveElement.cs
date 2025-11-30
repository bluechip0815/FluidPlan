namespace FluidSimu
{
    public class ValveElement : BaseElement, IControllable
    {
        private readonly double Area;
        private readonly double Diameter;
        // Ventil-Schaltzeit selbst(intern) liegt oft im Bereich von etwa 10–50 ms, je nach Typ.
        private readonly double SwtichingTime;
        // The state (0 or 1) the valve is moving TOWARDS.
        private double _commandedState = 0.0;
        // The simulation time when the last command was received.
        private double _lastStateChangeTime = -1.0;
        // The actual, current opening percentage of the valve (0.0 to 1.0).
        private double _currentOpeningFactor = 0.0;
        public ValveElement(ElementDto dto, int num) : base(dto, num)
        {
            Type = PneumaticType.valve;

            Diameter = ParameterHelper.GetDiameter(dto);
            SwtichingTime = ParameterHelper.GetDouble(dto, "ValveSwitchingTime", 0);

            Area = Math.PI / 4 * Diameter * Diameter;
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
        public void SetSchedule(List<ValveEventDto> timeline)
        {
            // Sort to ensure efficiency
            _schedule = timeline.OrderBy(x => x.TimeSeconds).ToList();
        }
        private readonly List<IPneumaticElement> _neighbors = new();

        public void RegisterNeighbor(IPneumaticElement element)
        {
            if (!_neighbors.Contains(element))
                _neighbors.Add(element);
        }
        private double UpdateAndGetCurrentOpening(double simulationTime)
        {
            if (_schedule.Count == 0) return 0.0;

            // 1. Find the most recent command from the schedule
            double latestCommand = 0.0;
            double latestCommandTime = 0.0;
            foreach (var item in _schedule)
            {
                if (item.TimeSeconds <= simulationTime)
                {
                    latestCommand = item.State;
                    latestCommandTime = item.TimeSeconds;
                }
                else
                {
                    break; // Future event
                }
            }

            // 2. Check if a NEW command has been issued
            if (Math.Abs(latestCommand - _commandedState) > 1e-6 && simulationTime >= latestCommandTime)
            {
                // A state change was triggered! Record the time.
                _commandedState = latestCommand;
                _lastStateChangeTime = latestCommandTime;
            }

            // 3. Calculate the opening factor using the smooth transition
            if (_lastStateChangeTime < 0)
            {
                // No command has been issued yet
                _currentOpeningFactor = 0.0;
            }
            else
            {
                double timeSinceCommand_s = simulationTime - _lastStateChangeTime;
                double timeSinceCommand_ms = timeSinceCommand_s * 1000.0;

                // Get the transition progress (0 to 1) from our new physics function
                double alpha = FlowPhysics.GetValveTransitionAlpha(timeSinceCommand_ms);

                // If the command is to OPEN, the opening is alpha.
                // If the command is to CLOSE, the opening is 1.0 - alpha.
                if (_commandedState > 0.5) // Command is to OPEN
                {
                    _currentOpeningFactor = alpha;
                }
                else // Command is to CLOSE
                {
                    _currentOpeningFactor = 1.0 - alpha;
                }
            }

            return _currentOpeningFactor;
        }
        public override double CalcPressure(PneumaticModel model)
        {
            // Do nothing. Pressure remains undefined or 0, 
            // but it doesn't matter because neighbors act directly on each other now.
            return 0.0;
        }
        protected override void DoStep(PneumaticModel model, IPneumaticElement otherNode)
        {
            // Find the two elements that the valve connects.
            // One is 'otherNode'. The other must be a neighbor different from 'otherNode'.
            IPneumaticElement? element1 = otherNode;
            IPneumaticElement? element2 = _neighbors.FirstOrDefault(n => n.Id != element1.Id);

            if (element1 == null || element2 == null)
                return;

            // 2. Get Valve State
            double opening = UpdateAndGetCurrentOpening(model.CurrentTime);
            if (opening <= 1e-6)
            {
                Pressure = 0;   // Valve is closed
                return;         // No flow
            }
            else
            {
                Pressure = 1; // Valve is open (internal state, not physical pressure)
            }

            // 3. Calculate Flow between element1 and element2
            double p1 = element1.Pressure;
            double p2 = element2.Pressure;

            // Use the Area * Opening
            double effectiveArea = Area * opening;

            // Calculate flow: q is positive if p1 > p2, negative if p2 > p1
            double q = FlowPhysics.ComputeVolumeFlow(p1, p2, effectiveArea);

            // If q is positive, flow is from element1 to element2.
            // If q is negative, flow is from element2 to element1.

            // 4. Calculate Charge Transfer
            double pMean = 0.5 * (p1 + p2);
            double qCharge = FlowPhysics.VolumeFlowToChargeFlow(Math.Abs(q), pMean); // Use absolute value of q for charge amount

            double currentChargeTransfer = qCharge * model.DeltaT;

            // 5. Apply Charge DIRECTLY to the neighbors (Skipping the Valve itself)
            if (q > 0) // Flow from element1 to element2
            {
                model.AddCharge(new ChargeData() { Id = element1.Id, dQ = -currentChargeTransfer });
                model.AddCharge(new ChargeData() { Id = element2.Id, dQ = +currentChargeTransfer });
            }
            else if (q < 0) // Flow from element2 to element1
            {
                model.AddCharge(new ChargeData() { Id = element2.Id, dQ = -currentChargeTransfer });
                model.AddCharge(new ChargeData() { Id = element1.Id, dQ = +currentChargeTransfer });
            }
            // If q == 0, no charge transfer.
        }
    }
}