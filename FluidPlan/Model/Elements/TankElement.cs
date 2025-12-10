namespace FluidSimu
{
    public class TankElement : BaseElement
    {
        public TankElement(ElementDto dto, int num) : base(dto, num)
        {
            Type = PneumaticType.tank;
            Pressure = ParameterHelper.GetPressure(dto);
            Volume = ParameterHelper.GetVolume(dto);
        }
        protected override void DoStep(PneumaticModel model, IPneumaticElement otherNode)
        {
            CalculateAndApplyFlow(model, otherNode);
        }
    }
}