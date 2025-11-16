using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;

/// <summary>
/// 模拟主驱动线反馈端口
/// </summary>
public class FakeMainLineFeedbackPort : IMainLineFeedbackPort
{
    private readonly FakeMainLineDrivePort _drivePort;

    public FakeMainLineFeedbackPort(FakeMainLineDrivePort drivePort)
    {
        _drivePort = drivePort;
    }

    public double GetCurrentSpeed()
    {
        return _drivePort.IsRunning ? _drivePort.TargetSpeed : 0;
    }

    public MainLineStatus GetCurrentStatus()
    {
        return _drivePort.IsRunning ? MainLineStatus.Running : MainLineStatus.Stopped;
    }

    public int? GetFaultCode()
    {
        return null;
    }
}
