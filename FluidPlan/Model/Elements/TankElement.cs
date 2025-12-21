using FluidPlan.Helper;

namespace FluidSimu
{
    public class TankElement : BaseElement
    {
        public TankElement(ElementDto dto, int id, int charge) : base(dto, id, charge)
        {
            Type = PneumaticType.tank;
            Pressure = ParameterHelper.GetPressure(dto);
            Volume = ParameterHelper.GetVolume(dto);
        }
    }
}