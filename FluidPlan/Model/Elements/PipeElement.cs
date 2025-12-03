
namespace FluidSimu
{
    public class PipeElement : BaseElement
    {
        //private readonly double Roughness = 1.0;
        private readonly double Area = 0.0;

        public PipeElement(ElementDto dto, int num) : base(dto, num)
        {
            Type = PneumaticType.pipe;

            double length = ParameterHelper.GetLength(dto);
            double diameter = ParameterHelper.GetDiameter(dto);
            //Roughness = ParameterHelper.GetDouble(dto, "roughness", 1.0);

            Pressure = ParameterHelper.GetPressure(dto);
            Area = Math.PI / 4 * diameter * diameter;
            Volume = Area * length;
        }

        protected override void DoStep(PneumaticModel model, IPneumaticElement otherNode)
        {
            // Drücke an den Enden [bar]
            double pFrom = otherNode.Pressure;
            double pTo = Pressure;

            // Volumenstrom [m³/s], positiv von From -> To
            double q = FlowPhysics.ComputeVolumeFlow(pFrom, pTo, Area, FlowCoefficient);

            // Mittlerer Druck für die Ladungsdefinition
            double pMean = 0.5 * (pFrom + pTo);

            // Ladungsstrom [m³·bar / s]
            double qCharge = FlowPhysics.VolumeFlowToChargeFlow(q, pMean);
            if (double.IsNaN(qCharge))
                throw new ArgumentException($"Connection {Id}:{Name} has no pressure");
            if (double.IsInfinity(qCharge))
                throw new ArgumentException($"Connection {Id}:{Name} has Infinity pressure");

            // Änderung der Ladung während dt
            double CurrentQ = qCharge * model.DeltaT;

            // From verliert Ladung, To gewinnt
            model.AddCharge(new ChargeData() { Id = otherNode.Id, dQ = -CurrentQ });
            model.AddCharge(new ChargeData() { Id = Id, dQ = +CurrentQ });
        }
    }
}