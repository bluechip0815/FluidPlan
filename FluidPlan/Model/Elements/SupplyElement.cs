namespace FluidSimu
{
    public class SupplyElement : BaseElement
    {
        public SupplyElement(ElementDto dto, int num, bool Exhaust) : base(dto, num)
        {
            Type = Exhaust ? PneumaticType.exhaust : PneumaticType.supply;
            Pressure = Exhaust ? 0 : ParameterHelper.GetPressure(dto);
        }
        // DoStep logic is identical to PipeElement's new logic.
        protected override void DoStep(PneumaticModel model, IPneumaticElement otherNode)
        {
            CalculateAndApplyFlow(model, otherNode);
        }
        public override double CalcPressure(PneumaticModel model)
        {
            return 0;
        }
    }
}