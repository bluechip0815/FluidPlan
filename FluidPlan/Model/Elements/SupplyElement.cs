namespace FluidSimu
{
    public class SupplyElement : BaseElement
    {
        private double Area = 0.0;
        public SupplyElement(ElementDto dto, int num, bool Exhaust) : base(dto, num)
        {
            Type = Exhaust ? PneumaticType.exhaust : PneumaticType.supply;
            Pressure = Exhaust ? 0 : ParameterHelper.GetPressure(dto);

            // Supplies and exhausts now have a configurable port diameter.
            // Use "portDiameter" first, but fall back to "diameter" for backward compatibility.
            Area = Math.PI / 4 * Diameter * Diameter;
        }

        // DoStep logic is identical to PipeElement's new logic.
        protected override void DoStep(PneumaticModel model, IPneumaticElement otherNode)
        {

            // Calculate the volume flow based on the pressure difference.
            var effectiveDiameter = Math.Min(Diameter, otherNode.Diameter);
            var effectiveArea = Math.PI / 4 * Math.Pow(effectiveDiameter, 2);
            double q = FlowPhysics.ComputeSmoothedVolumeFlow(Pressure, otherNode.Pressure, effectiveArea, FlowCoefficient, LastFlow, model.DeltaT);
            LastFlow = q;

            double pMean = 0.5 * (Pressure + otherNode.Pressure);
            double qCharge = FlowPhysics.VolumeFlowToChargeFlow(q, pMean);
            double currentQ = qCharge * model.DeltaT;

            model.AddCharge(new ChargeData() { Id = otherNode.Id, dQ = +currentQ });
        }
        public override double CalcPressure(PneumaticModel model)
        {
            return 0;
        }
    }
}