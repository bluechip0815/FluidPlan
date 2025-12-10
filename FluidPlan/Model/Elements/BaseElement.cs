namespace FluidSimu
{

    public interface IPneumaticElement
    {
        string Name { get; }
        int Id { get; }
        PneumaticType Type { get; }
        double Pressure { get; }
        bool IsVisible { get; }
        double Diameter { get; }
        string ToString();
        void CalcFlow(PneumaticModel model, List<IPneumaticElement> elements, int startIndex);
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
        public double Diameter { get; protected set; }
        protected double Area { get; private set; }
        // Internal pressure
        public double Pressure { get; protected set; } = 0;
        public double LastQ { get; protected set; } = 0;
        public double LastFlow { get; protected set; } = 0;
        public bool IsVisible { get; protected set; }
        public double FlowCoefficient { get; protected set; } = 1.0;
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
            Diameter = ParameterHelper.GetDiameter(dto);
            if (Diameter <= 0) Diameter = 0.01; // Default 10mm to prevent divide-by-zero
            Area = Math.PI * Math.Pow(Diameter / 2.0, 2);
            IsVisible = dto.Visible;
            FlowCoefficient = dto.FlowCoefficient;
            Reset();
        }
        public override string ToString()
        {
            return $"Element #{Id}: {Name} ({Type.ToString()})";
        }
        public void CalcFlow(PneumaticModel model, List<IPneumaticElement> elements, int startIndex)
        {
            for (int idx = startIndex; idx < elements.Count; idx++)
            {
                var other = elements[idx];

                // FIX: If the other element is a Valve (Volume 0), we let the Valve handle the logic.
                // We do NOT push flow into it blindly.
                if (other.Type == PneumaticType.valve ||
                    other.Type == PneumaticType.checkvalve)
                    continue;

                DoStep(model, other);
            }
        }

        /// <summary>
        /// Centralized Physics Calculation.
        /// Handles: Effective Area (Bottleneck), Flow Smoothing, and Upstream Density.
        /// </summary>
        /// <param name="model">Simulation context</param>
        /// <param name="neighbor">The element connected to this one</param>
        /// <param name="areaOverride">Optional: Pass a specific area (e.g. for Valves/Throttles)</param>
        protected void CalculateAndApplyFlow(PneumaticModel model, IPneumaticElement neighbor, double? areaOverride = null)
        {
            // 1. Determine My Area (Use override if provided, else physical area)
            double myArea = areaOverride ?? this.Area;

            // 2. Determine Neighbor Area 
            // We need to calculate it from the neighbor's diameter
            double neighborArea = Math.PI * Math.Pow(neighbor.Diameter / 2.0, 2);

            // 3. THE FIX: The effective restriction is the SMALLEST of the two.
            double effectiveArea = Math.Min(myArea, neighborArea);

            if (effectiveArea <= 1e-9)
            {
                LastFlow = 0;
                return;
            }

            // 4. Get Pressures
            double pFrom = this.Pressure;
            double pTo = neighbor.Pressure;

            // 5. Compute Volume Flow
            double q = FlowPhysics.ComputeSmoothedVolumeFlow(pFrom, pTo, effectiveArea, FlowCoefficient, LastFlow, model.DeltaT);
            LastFlow = q;

            // 6. Compute Mass (Charge) Flow
            // CRITICAL: Use the pressure of the SOURCE (Upstream) for density
            double pSource = (q > 0) ? pFrom : pTo;

            // Convert Volume Flow -> Mass Flow (Charge)
            double qCharge = FlowPhysics.VolumeFlowToChargeFlow(Math.Abs(q), pSource);
            double currentQ = qCharge * model.DeltaT;

            // 7. Apply Charges
            if (q > 0) // From Me -> Neighbor
            {
                model.AddCharge(this.Id, -currentQ);
                model.AddCharge(neighbor.Id, +currentQ);
            }
            else // From Neighbor -> Me (Backflow)
            {
                model.AddCharge(this.Id, +currentQ);
                model.AddCharge(neighbor.Id, -currentQ);
            }
        }
        // ... (Keep existing DoStep, CalcPressure, etc.) ...
        protected abstract void DoStep(PneumaticModel model, IPneumaticElement oherNode);
        public void Reset()
        {
            LastQ = 0;
        }
        public virtual double CalcPressure(PneumaticModel model)
        {
            double effectiveVolume = (Volume < 1e-9) ? 1e-9 : Volume;

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

            // Optional: Prevent negative pressure (Vacuum limit)
            if (Pressure < 0) Pressure = 0;

            if (double.IsNaN(Pressure))
                throw new ArgumentException($"Connection {Id}:{Name} calculated NaN pressure");

            // 4. Return Absolute Pressure Difference (Delta P)
            return Math.Abs(Pressure - oldPressure);
        }
    }
}