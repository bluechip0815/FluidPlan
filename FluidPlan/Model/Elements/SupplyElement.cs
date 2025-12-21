using FluidPlan.Helper;

namespace FluidSimu
{
    public class SupplyElement : BaseElement
    {
        public SupplyElement(ElementDto dto, int id, int charge, bool Exhaust) : base(dto, id, charge)
        {
            Type = Exhaust ? PneumaticType.exhaust : PneumaticType.supply;
            Pressure = Exhaust ? 0 : ParameterHelper.GetPressure(dto);
        }        
        public override double CalcPressure(PneumaticModel model)
        {
            return 0;
        }
    }
}