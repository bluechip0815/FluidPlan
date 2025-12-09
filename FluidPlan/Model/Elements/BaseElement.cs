namespace FluidSimu
{

    public interface IPneumaticElement
    {
        string Name { get; }
        int Id { get; }
        PneumaticType Type { get; }
        double Pressure { get; }
        bool IsVisible { get; }
        string ToString();
        void AddConnection(string connectorName, IPneumaticElement connectedElement);
        void CalcFlow(PneumaticModel model);
        double CalcPressure(PneumaticModel model);
    }
    public abstract class BaseElement : IPneumaticElement
    {
        public int Id { get; }
        public string Name { get; }
        public PneumaticType Type { get; protected set; }
        public string Comment { get; protected set; }
        public string Description { get; protected set; }
        public double Volume { get; protected set; } = 0;
        // Internal pressure
        public double Pressure { get; protected set; } = 0;
        public double LastQ { get; protected set; } = 0;
        public double LastFlow { get; protected set; } = 0;
        public bool IsVisible { get; protected set; }
        public double FlowCoefficient { get; protected set; } = 1.0;
        public List<string> ValidConnectorNames { get; protected set; } = new List<string>();
        /// <summary>
        /// 
        /// </summary>
        protected BaseElement(ElementDto dto, int number)
        {
            Id = number;
            Name = dto.Name;
            Comment = dto.Comment ?? "";
            Description = dto.Description ?? "";
            IsVisible = ParameterHelper.GetBool(dto, "visible", false);
            // Initialize with a default zero-flow port. Derived classes must override this.
            IsVisible = dto.Visible;
            FlowCoefficient = dto.FlowCoefficient;
            Reset();
        }
        public override string ToString()
        {
            return $"Element #{Id}: {Name} ({Type.ToString()})";
        }
        public Dictionary<string, IPneumaticElement> Connections { get; } = new Dictionary<string, IPneumaticElement>();
        public void AddConnection(string connectorName, IPneumaticElement connectedElement)
        {
            if (Connections.ContainsKey(connectorName))
            {
                // Handle error: connector already in use
                throw new InvalidOperationException($"Connector '{connectorName}' on element '{Name}' is already connected.");
            }
            Connections[connectorName] = connectedElement;
        }
        public void CalcFlow(PneumaticModel model)
        {
            foreach (var connection in Connections)
            {
                var other = connection.Value;

                // By processing flow only when our ID is less than the other's,
                // we ensure each connection is processed exactly once.
                if (Id > other.Id)
                    continue;

                // FIX: If the other element is a Valve (Volume 0), we let the Valve handle the logic.
                // We do NOT push flow into it blindly.
                if (other.Type == PneumaticType.valve ||
                    other.Type == PneumaticType.checkvalve)
                    continue;

                DoStep(model, other);
            }
        }

        protected abstract void DoStep(PneumaticModel model, IPneumaticElement oherNode);
        public void Reset()
        {
            LastQ = 0;
        }
        public virtual double CalcPressure(PneumaticModel model)
        {
            // 1. Store old pressure
            double oldPressure = Pressure;

            if (double.IsNaN(Pressure))
                throw new ArgumentException($"Connection {Id}:{Name} calculated NaN pressure");

            // 2. Calculate new Charge (existing logic)
            double dQ = Pressure * Volume; // Current Charge
            dQ += model.GetCharge(Id);     // Add Delta
            if (double.IsNaN(dQ))
                throw new ArgumentException($"Connection {Id}:{Name} calculated NaN pressure");

            // 3. Update Pressure
            Pressure = dQ / Volume;
            if (double.IsNaN(Pressure))
                throw new ArgumentException($"Connection {Id}:{Name} calculated NaN pressure");

            // 4. Return Absolute Pressure Difference (Delta P)
            return Math.Abs(Pressure - oldPressure);
        }
    }
}