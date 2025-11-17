using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Tests.Fakes;

/// <summary>
/// 假的系统运行状态服务，用于测试
/// 总是返回 Running 状态以允许包裹创建
/// </summary>
public class FakeSystemRunStateService : ISystemRunStateService
{
    private SystemRunState _currentState = SystemRunState.Running;

    public SystemRunState Current => _currentState;

    public void SetState(SystemRunState state)
    {
        _currentState = state;
    }

    public OperationResult TryHandleStart()
    {
        if (_currentState == SystemRunState.Running)
            return OperationResult.Failure("已处于运行状态");
        _currentState = SystemRunState.Running;
        return OperationResult.Success();
    }

    public OperationResult TryHandleStop()
    {
        if (_currentState == SystemRunState.Stopped)
            return OperationResult.Failure("已处于停止状态");
        _currentState = SystemRunState.Stopped;
        return OperationResult.Success();
    }

    public OperationResult TryHandleEmergencyStop()
    {
        if (_currentState == SystemRunState.Fault)
            return OperationResult.Failure("已处于故障状态");
        _currentState = SystemRunState.Fault;
        return OperationResult.Success();
    }

    public OperationResult TryHandleEmergencyReset()
    {
        if (_currentState != SystemRunState.Fault)
            return OperationResult.Failure("不在故障状态");
        _currentState = SystemRunState.Ready;
        return OperationResult.Success();
    }

    public OperationResult ValidateCanCreateParcel()
    {
        if (_currentState != SystemRunState.Running)
            return OperationResult.Failure($"系统当前状态为 {_currentState}，禁止创建包裹");
        return OperationResult.Success();
    }
}
