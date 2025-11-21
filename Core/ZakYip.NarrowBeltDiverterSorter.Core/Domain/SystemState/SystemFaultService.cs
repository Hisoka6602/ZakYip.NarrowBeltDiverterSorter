using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState;

/// <summary>
/// 系统故障服务实现
/// 维护当前活动故障的集合，并提供故障管理功能
/// </summary>
public class SystemFaultService : ISystemFaultService
{
    private readonly Dictionary<SystemFaultCode, SystemFault> _activeFaults = new();
    private readonly object _faultsLock = new();

    public SystemFaultService()
    {
    }

    /// <inheritdoc/>
    public event EventHandler<SystemFaultEventArgs>? FaultAdded;

    /// <inheritdoc/>
    public event EventHandler<SystemFaultCode>? FaultCleared;

    /// <inheritdoc/>
    public IReadOnlyList<SystemFault> GetActiveFaults()
    {
        lock (_faultsLock)
        {
            return _activeFaults.Values.ToList();
        }
    }

    /// <inheritdoc/>
    public bool HasBlockingFault()
    {
        lock (_faultsLock)
        {
            return _activeFaults.Values.Any(f => f.IsBlocking);
        }
    }

    /// <inheritdoc/>
    public void RegisterFault(SystemFaultCode faultCode, string message, bool isBlocking = true, Exception? exception = null)
    {
        lock (_faultsLock)
        {
            // 如果故障已存在，不重复添加
            if (_activeFaults.ContainsKey(faultCode))
            {
                return;
            }

            var fault = new SystemFault
            {
                FaultCode = faultCode,
                OccurredAt = DateTimeOffset.Now,
                Message = message,
                IsBlocking = isBlocking
            };

            _activeFaults[faultCode] = fault;
        }

        // 在锁外触发事件，避免死锁
        var eventArgs = new SystemFaultEventArgs
        {
            FaultCode = faultCode,
            OccurredAt = DateTimeOffset.Now, // 使用本地时间
            Message = message,
            Exception = exception
        };
        FaultAdded?.Invoke(this, eventArgs);
    }

    /// <inheritdoc/>
    public bool ClearFault(SystemFaultCode faultCode)
    {
        bool removed;
        lock (_faultsLock)
        {
            removed = _activeFaults.Remove(faultCode);
        }

        // 在锁外触发事件
        if (removed)
        {
            FaultCleared?.Invoke(this, faultCode);
        }

        return removed;
    }

    /// <inheritdoc/>
    public void ClearAllFaults()
    {
        List<SystemFaultCode> clearedFaults;
        lock (_faultsLock)
        {
            clearedFaults = _activeFaults.Keys.ToList();
            _activeFaults.Clear();
        }

        // 在锁外触发事件
        foreach (var faultCode in clearedFaults)
        {
            FaultCleared?.Invoke(this, faultCode);
        }
    }
}
