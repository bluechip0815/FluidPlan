namespace FluidSimu
{
    public class ValveElement : BaseElement, IControllable
    {
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
            SwtichingTime = ParameterHelper.GetDouble(dto, "ValveSwitchingTime", 0);
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
            double opening = UpdateAndGetCurrentOpening(model.CurrentTime);

            if (opening <= 1e-6)
            {
                LastFlow = 0;
                return;
            }

            // 2. Calculate the Valve's current open area
            double currentValveArea = this.Area * opening;

            // 3. Pass this dynamic area to the calculation
            CalculateAndApplyFlow(model, otherNode, currentValveArea);
        }
    }
}