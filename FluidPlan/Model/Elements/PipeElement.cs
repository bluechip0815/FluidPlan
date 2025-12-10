
namespace FluidSimu
{
    public class PipeElement : BaseElement
    {
        public PipeElement(ElementDto dto, int num) : base(dto, num)
        {
            Type = PneumaticType.pipe;
            double length = ParameterHelper.GetLength(dto);
            Pressure = ParameterHelper.GetPressure(dto);
            Volume = Area * length;
        }
        protected override void DoStep(PneumaticModel model, IPneumaticElement otherNode)
        {
            CalculateAndApplyFlow(model, otherNode); 
        }
    }
}