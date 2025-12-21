
using FluidPlan.Helper;

namespace FluidSimu
{
    public class PipeElement : BaseElement
    {
        public PipeElement(ElementDto dto, int id, int charge) : base(dto, id, charge)
        {
            Type = PneumaticType.pipe;
            double length = ParameterHelper.GetLength(dto);
            Pressure = ParameterHelper.GetPressure(dto);
            Volume = Area * length;
        }        
    }
}