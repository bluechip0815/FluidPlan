
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

            Pressure = ParameterHelper.GetPressure(dto);
            Area = Math.PI / 4 * Diameter * Diameter;
            Volume = Area * length;

        }

        protected override void DoStep(PneumaticModel model, IPneumaticElement otherNode)
        {
            // --- START: IMPROVED FLOW LOGIC ---
            // The effective area for flow is the SMALLER of the two connecting ports.
            // This correctly models that the tightest restriction dictates the flow rate.
            var effectiveDiameter = Math.Min(Diameter, otherNode.Diameter);
            var effectiveArea = Math.PI / 4 * Math.Pow(effectiveDiameter, 2);

            double q = FlowPhysics.ComputeSmoothedVolumeFlow(otherNode.Pressure, Pressure, effectiveArea, FlowCoefficient, LastFlow, model.DeltaT);
            LastFlow = q;

            double pMean = 0.5 * (otherNode.Pressure + Pressure);
            double qCharge = FlowPhysics.VolumeFlowToChargeFlow(q, pMean);
            if (double.IsNaN(qCharge))
                throw new ArgumentException($"Connection {Id}:{Name} has no pressure");
            if (double.IsInfinity(qCharge))
                throw new ArgumentException($"Connection {Id}:{Name} has Infinity pressure");

            double CurrentQ = qCharge * model.DeltaT;
            model.AddCharge(new ChargeData() { Id = otherNode.Id, dQ = -CurrentQ });
            model.AddCharge(new ChargeData() { Id = Id, dQ = +CurrentQ });
        }
    }
}