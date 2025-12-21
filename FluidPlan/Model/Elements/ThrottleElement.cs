using FluidPlan.Helper;

namespace FluidSimu
{

    public class ThrottleElement : ValveElement
    {
        public ThrottleElement(ElementDto dto, int id, int charge) : base(dto, id, charge)
        {
            Type = PneumaticType.throttle;
            Pressure = ParameterHelper.GetPressure(dto);
            _currentOpeningFactor = 1;
        }
        public override void UpdateInternalState(PneumaticModel _)
        {
            // do not change => _currentOpeningFactor
        }
    }
}