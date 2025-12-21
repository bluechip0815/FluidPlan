using FluidPlan.Helper;

namespace FluidSimu
{
    public interface IPneumaticElement
    {
        string Name { get; }
        int Id { get; }
        PneumaticType Type { get; }
        double Pressure { get; }
        double LoggableValue { get; }
        bool IsVisible { get; }
        double Diameter { get; }
        double Area { get; }
        string ToString();
        void SetConnectors(int port1, int port2);
        double CalcPressure(PneumaticModel model);
        void UpdateInternalState(PneumaticModel model);        
        public double FlowCoefficient { get;} 
        int ChargeIndex { get; set; }
    }
    public abstract class BaseElement : IPneumaticElement
    {
        public int Id { get; }
        public int ChargeIndex { get; set; } = -1;
        public string Name { get; }
        public PneumaticType Type { get; protected set; }
        public string Comment { get; protected set; }
        public string Description { get; protected set; }
        public double Volume { get; protected set; } = 0;
        public double Diameter { get; protected set; }
        public double Area { get; private set; }
        public double Pressure { get; protected set; } = 0;
        public virtual double LoggableValue => Pressure;
        //public double LastQ { get; protected set; } = 0;
        public double LastFlow { get; protected set; } = 0;
        public bool IsVisible { get; protected set; }
        public double FlowCoefficient { get; protected set; } = 1.0;
        public int Connector1 { get; protected set; } = 0;
        public int Connector2 { get; protected set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        protected BaseElement(ElementDto dto, int id, int chargeNumber)
        {
            Id = id;
            ChargeIndex = chargeNumber;
            Name = dto.Name;
            Comment = dto.Comment ?? "";
            Description = dto.Description ?? "";
            IsVisible = ParameterHelper.GetBool(dto, "visible", false);
            Diameter = ParameterHelper.GetDiameter(dto);
            if (Diameter <= 0) Diameter = 0.01; // Default 10mm to prevent divide-by-zero
            Area = Math.PI * Math.Pow(Diameter / 2.0, 2);
            IsVisible = dto.Visible;
            FlowCoefficient = dto.FlowCoefficient;
        }
        public override string ToString()
        {
            if (Connector2 == -1) return $"Element #{Id}: {Name} ({Type.ToString()}) [{Connector1}]";
            return $"Element #{Id}: {Name} ({Type.ToString()}) [{Connector1} <=> {Connector2}]";
        }
        public void SetConnectors(int port1, int port2)
        {
            Connector1 = port1;
            Connector2 = port2;
        }
        public virtual void UpdateInternalState(PneumaticModel model)
        {
            // Die meisten Elemente haben keinen internen Zustand, der aktualisiert werden muss.
        }
        public virtual double CalcPressure(PneumaticModel model)
        {
            if (Volume < 1e-9)  return 0;

            double oldPressure = Pressure;

            double charge = (oldPressure * Volume) + model.GetCharge(ChargeIndex);

            Pressure = charge / Volume;
            if (Pressure < 0) Pressure = 0;

            if (double.IsNaN(Pressure))
                throw new ArgumentException($"Connection {Id}:{Name} calculated NaN pressure");

            return Math.Abs(Pressure - oldPressure);
        }
    }
}