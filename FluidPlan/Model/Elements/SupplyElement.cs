namespace FluidSimu
{
    public class SupplyElement : BaseElement
    {
public SupplyElement(ElementDto dto, int num, bool Exhaust) : base(dto, num)
{
    Type = Exhaust ? PneumaticType.exhaust : PneumaticType.supply;
    Pressure = Exhaust ? 0 : ParameterHelper.GetPressure(dto);

    // Supplies and exhausts now have a configurable port diameter.
    // Use "portDiameter" first, but fall back to "diameter" for backward compatibility.
    double portDiameter = ParameterHelper.GetDiameter(dto, "portDiameter");
    if (portDiameter == 0.0) portDiameter = ParameterHelper.GetDiameter(dto, "diameter");
    if (portDiameter == 0.0) portDiameter = 0.02; // Sane default if neither is specified
    ConnectionPort = new Port(portDiameter);
}

// DoStep logic is identical to PipeElement's new logic.
protected override void DoStep(PneumaticModel model, IPneumaticElement otherNode)
{
    double effectiveArea = Math.Min(this.ConnectionPort.Area, otherNode.ConnectionPort.Area);

    double pFrom = this.Pressure;
    double pTo = otherNode.Pressure;

    double q = FlowPhysics.ComputeVolumeFlow(pFrom, pTo, effectiveArea);
    double pMean = 0.5 * (pFrom + pTo);
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