namespace FluidSimu
{
    public class TankElement : BaseElement
    {
public TankElement(ElementDto dto, int num) : base(dto, num)
{
    Type = PneumaticType.tank;
    Pressure = ParameterHelper.GetPressure(dto);
    Volume = ParameterHelper.GetVolume(dto);

    // A tank now has a specific port diameter for its connection.
    double portDiameter = ParameterHelper.GetDiameter(dto, "portDiameter");
    if (portDiameter == 0.0) portDiameter = ParameterHelper.GetDiameter(dto, "diameter");
    ConnectionPort = new Port(portDiameter);
}

// DoStep logic is identical to PipeElement's new logic.
protected override void DoStep(PneumaticModel model, IPneumaticElement otherNode)
{
    double effectiveArea = Math.Min(this.ConnectionPort.Area, otherNode.ConnectionPort.Area);

    double pFrom = Pressure;
    double pTo = otherNode.Pressure;

    double q = FlowPhysics.ComputeVolumeFlow(pFrom, pTo, effectiveArea);
    double pMean = 0.5 * (pFrom + pTo);
    double qCharge = FlowPhysics.VolumeFlowToChargeFlow(q, pMean);
    double CurrentQ = qCharge * model.DeltaT;

    model.AddCharge(new ChargeData() { Id = otherNode.Id, dQ = +CurrentQ });
    model.AddCharge(new ChargeData() { Id = Id, dQ = -CurrentQ });
}
    }
}