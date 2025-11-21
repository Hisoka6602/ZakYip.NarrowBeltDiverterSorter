using ZakYip.NarrowBeltDiverterSorter.Shared.Kernel;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState;

/// <summary>
/// 系统运行状态服务实现
/// 管理系统状态机转换和验证逻辑
/// </summary>
public class SystemRunStateService : ISystemRunStateService
{
    private SystemRunState _currentState;
    private readonly object _stateLock = new();

    public SystemRunStateService()
    {
        _currentState = SystemRunState.Stopped; // 默认状态为停止，需要通过面板启动按钮切换到运行状态
    }

    /// <inheritdoc/>
    public SystemRunState Current
    {
        get
        {
            lock (_stateLock)
            {
                return _currentState;
            }
        }
    }

    /// <inheritdoc/>
    public OperationResult TryHandleStart()
    {
        lock (_stateLock)
        {
            // 规则：故障状态下所有按钮无效
            if (_currentState == SystemRunState.Fault)
            {
                return OperationResult.Failure("系统当前处于故障状态，无法启动");
            }

            // 规则：运行状态下再按启动按钮无效
            if (_currentState == SystemRunState.Running)
            {
                return OperationResult.Failure("系统已处于运行状态");
            }

            // 从停止状态可以启动
            _currentState = SystemRunState.Running;
            return OperationResult.Success();
        }
    }

    /// <inheritdoc/>
    public OperationResult TryHandleStop()
    {
        lock (_stateLock)
        {
            // 规则：故障状态下所有按钮无效
            if (_currentState == SystemRunState.Fault)
            {
                return OperationResult.Failure("系统当前处于故障状态，无法停止");
            }

            // 规则：停止状态下再按停止按钮无效
            if (_currentState == SystemRunState.Stopped)
            {
                return OperationResult.Failure("系统已处于停止状态");
            }

            // 从运行状态可以停止
            _currentState = SystemRunState.Stopped;
            return OperationResult.Success();
        }
    }

    /// <inheritdoc/>
    public OperationResult TryHandleEmergencyStop()
    {
        lock (_stateLock)
        {
            // 规则：故障状态下所有按钮无效（包括再次急停）
            if (_currentState == SystemRunState.Fault)
            {
                return OperationResult.Failure("系统已处于故障状态");
            }

            // 从任意非故障状态都可以进入故障状态
            _currentState = SystemRunState.Fault;
            return OperationResult.Success();
        }
    }

    /// <inheritdoc/>
    public OperationResult TryHandleEmergencyReset()
    {
        lock (_stateLock)
        {
            // 只有在故障状态下才能执行急停解除
            if (_currentState != SystemRunState.Fault)
            {
                return OperationResult.Failure($"系统当前状态为 {_currentState}，不需要解除急停");
            }

            // 急停解除后进入停止状态（而非就绪状态），需要通过启动按钮才能运行
            _currentState = SystemRunState.Stopped;
            return OperationResult.Success();
        }
    }

    /// <inheritdoc/>
    public OperationResult ValidateCanCreateParcel()
    {
        lock (_stateLock)
        {
            if (_currentState != SystemRunState.Running)
            {
                var errorMessage = _currentState switch
                {
                    SystemRunState.Stopped => $"系统当前未处于运行状态，禁止创建包裹。当前状态: 停止",
                    SystemRunState.Fault => $"系统当前处于故障状态，禁止创建包裹",
                    SystemRunState.Ready => $"系统当前未处于运行状态，禁止创建包裹。当前状态: 就绪（不应出现此状态）",
                    _ => $"系统当前未处于运行状态，禁止创建包裹。当前状态: {_currentState}"
                };

                return OperationResult.Failure(errorMessage);
            }

            return OperationResult.Success();
        }
    }

    /// <inheritdoc/>
    public void ForceToFaultState(string reason)
    {
        lock (_stateLock)
        {
            if (_currentState != SystemRunState.Fault)
            {
                _currentState = SystemRunState.Fault;
            }
        }
    }
}
