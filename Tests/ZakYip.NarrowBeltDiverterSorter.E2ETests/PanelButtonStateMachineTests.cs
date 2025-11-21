using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Observability;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.SystemState;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Observability;
using ZakYip.NarrowBeltDiverterSorter.Core.Application;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Observability;
using ZakYip.NarrowBeltDiverterSorter.Execution.Panel;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Observability;
using ZakYip.NarrowBeltDiverterSorter.Communication;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Observability;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Observability;
using Microsoft.Extensions.Options;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums.Observability;

namespace ZakYip.NarrowBeltDiverterSorter.E2ETests;

/// <summary>
/// 面板按钮 IO / 联动 IO 状态机端到端测试
/// </summary>
public class PanelButtonStateMachineTests
{
    /// <summary>
    /// 假的现场总线客户端，用于跟踪 IO 写入操作
    /// </summary>
    private class FakeFieldBusClientWithTracking : IFieldBusClient
    {
        private readonly Dictionary<int, bool> _coilStates = new();
        public List<(int address, bool value)> WriteHistory { get; } = new();

        public Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<bool> WriteSingleCoilAsync(int address, bool value, CancellationToken cancellationToken = default)
        {
            _coilStates[address] = value;
            WriteHistory.Add((address, value));
            return Task.FromResult(true);
        }

        public Task<bool> WriteMultipleCoilsAsync(int startAddress, bool[] values, CancellationToken cancellationToken = default)
        {
            for (int i = 0; i < values.Length; i++)
            {
                _coilStates[startAddress + i] = values[i];
                WriteHistory.Add((startAddress + i, values[i]));
            }
            return Task.FromResult(true);
        }

        public Task<bool> WriteSingleRegisterAsync(int address, ushort value, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> WriteMultipleRegistersAsync(int startAddress, ushort[] values, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool[]?> ReadCoilsAsync(int address, int count, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<bool[]?>(null);
        }

        public Task<bool[]?> ReadDiscreteInputsAsync(int address, int count, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<bool[]?>(null);
        }

        public Task<ushort[]?> ReadHoldingRegistersAsync(int address, int count, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ushort[]?>(null);
        }

        public Task<ushort[]?> ReadInputRegistersAsync(int address, int count, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ushort[]?>(null);
        }

        public bool IsConnected()
        {
            return true;
        }

        public void ClearHistory()
        {
            WriteHistory.Clear();
        }

        public bool GetCoilState(int address)
        {
            return _coilStates.TryGetValue(address, out var value) && value;
        }
    }

    private static ILogger<PanelIoCoordinator> CreateLogger()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        return loggerFactory.CreateLogger<PanelIoCoordinator>();
    }

    [Fact]
    public async Task Scenario1_DefaultStateAndStartButton_ShouldTransitionToRunningAndWriteIOs()
    {
        // Arrange: 配置跟随启动的 IO 通道
        var options = Options.Create(new PanelIoLinkageOptions
        {
            StartFollowOutputChannels = new List<int> { 100, 101, 102 },
            StopFollowOutputChannels = new List<int> { 200, 201 }
        });
        var fakeFieldBus = new FakeFieldBusClientWithTracking();
        var ioCoordinator = new PanelIoCoordinator(fakeFieldBus, options, CreateLogger());
        var stateService = new SystemRunStateService();
        var parcelService = new ParcelLifecycleService(stateService);

        // Assert: 初始状态为停止
        Assert.Equal(SystemRunState.Stopped, stateService.Current);

        // Act: 按下启动按钮
        var startResult = stateService.TryHandleStart();
        Assert.True(startResult.IsSuccess);

        // 执行启动联动 IO 写入
        var ioResult = await ioCoordinator.ExecuteStartLinkageAsync();
        Assert.True(ioResult.IsSuccess);

        // Assert: 系统状态变更为运行
        Assert.Equal(SystemRunState.Running, stateService.Current);

        // Assert: 所有跟随启动的 IO 通道已写入 ON (true)
        Assert.Equal(3, fakeFieldBus.WriteHistory.Count(w => w.value == true));
        Assert.True(fakeFieldBus.GetCoilState(100));
        Assert.True(fakeFieldBus.GetCoilState(101));
        Assert.True(fakeFieldBus.GetCoilState(102));

        // Assert: 此时可以创建包裹
        var parcel = parcelService.CreateParcel(new ParcelId(1000000000001), "TEST001", DateTimeOffset.Now);
        Assert.NotNull(parcel);
    }

    [Fact]
    public async Task Scenario2_StopButton_ShouldTransitionToStoppedAndWriteIOs()
    {
        // Arrange
        var options = Options.Create(new PanelIoLinkageOptions
        {
            StartFollowOutputChannels = new List<int> { 100, 101 },
            StopFollowOutputChannels = new List<int> { 200, 201, 202 }
        });
        var fakeFieldBus = new FakeFieldBusClientWithTracking();
        var ioCoordinator = new PanelIoCoordinator(fakeFieldBus, options, CreateLogger());
        var stateService = new SystemRunStateService();
        var parcelService = new ParcelLifecycleService(stateService);

        // 先启动系统
        stateService.TryHandleStart();
        await ioCoordinator.ExecuteStartLinkageAsync();
        Assert.Equal(SystemRunState.Running, stateService.Current);
        fakeFieldBus.ClearHistory();

        // Act: 按下停止按钮
        var stopResult = stateService.TryHandleStop();
        Assert.True(stopResult.IsSuccess);

        var ioResult = await ioCoordinator.ExecuteStopLinkageAsync();
        Assert.True(ioResult.IsSuccess);

        // Assert: 系统状态变更为停止
        Assert.Equal(SystemRunState.Stopped, stateService.Current);

        // Assert: 所有跟随停止的 IO 通道已写入 OFF (false)
        Assert.Equal(3, fakeFieldBus.WriteHistory.Count(w => w.value == false));
        Assert.False(fakeFieldBus.GetCoilState(200));
        Assert.False(fakeFieldBus.GetCoilState(201));
        Assert.False(fakeFieldBus.GetCoilState(202));

        // Assert: 创建包裹应被拒绝
        Assert.Throws<InvalidOperationException>(() =>
            parcelService.CreateParcel(new ParcelId(1000000000002), "TEST002", DateTimeOffset.Now));

        // Act: 再次按下停止按钮
        fakeFieldBus.ClearHistory();
        var stopResult2 = stateService.TryHandleStop();
        Assert.False(stopResult2.IsSuccess); // 应失败
        Assert.Contains("已处于停止状态", stopResult2.ErrorMessage);

        // Assert: 状态保持停止，IO 不重复写入
        Assert.Equal(SystemRunState.Stopped, stateService.Current);
        Assert.Empty(fakeFieldBus.WriteHistory); // 没有新的 IO 写入
    }

    [Fact]
    public async Task Scenario3_RunningStateDuplicateStart_ShouldFailAndNotWriteIOs()
    {
        // Arrange
        var options = Options.Create(new PanelIoLinkageOptions
        {
            StartFollowOutputChannels = new List<int> { 100 },
            StopFollowOutputChannels = new List<int> { 200 }
        });
        var fakeFieldBus = new FakeFieldBusClientWithTracking();
        var ioCoordinator = new PanelIoCoordinator(fakeFieldBus, options, CreateLogger());
        var stateService = new SystemRunStateService();

        // 先启动系统
        stateService.TryHandleStart();
        await ioCoordinator.ExecuteStartLinkageAsync();
        Assert.Equal(SystemRunState.Running, stateService.Current);
        fakeFieldBus.ClearHistory();

        // Act: 再次按下启动按钮
        var startResult = stateService.TryHandleStart();

        // Assert: 操作失败
        Assert.False(startResult.IsSuccess);
        Assert.Contains("已处于运行状态", startResult.ErrorMessage);

        // Assert: 状态保持运行
        Assert.Equal(SystemRunState.Running, stateService.Current);

        // Assert: 不应写入任何 IO（因为状态机拒绝了操作）
        Assert.Empty(fakeFieldBus.WriteHistory);
    }

    [Fact]
    public async Task Scenario4_EmergencyStopAndFaultState_ShouldBlockAllButtons()
    {
        // Arrange
        var options = Options.Create(new PanelIoLinkageOptions
        {
            StartFollowOutputChannels = new List<int> { 100 },
            StopFollowOutputChannels = new List<int> { 200, 201 }
        });
        var fakeFieldBus = new FakeFieldBusClientWithTracking();
        var ioCoordinator = new PanelIoCoordinator(fakeFieldBus, options, CreateLogger());
        var stateService = new SystemRunStateService();
        var parcelService = new ParcelLifecycleService(stateService);

        // 先启动系统
        stateService.TryHandleStart();
        Assert.Equal(SystemRunState.Running, stateService.Current);

        // Act: 按下急停按钮
        var emergencyStopResult = stateService.TryHandleEmergencyStop();
        Assert.True(emergencyStopResult.IsSuccess);

        // 执行停止联动 IO（急停使用停止联动）
        var ioResult = await ioCoordinator.ExecuteStopLinkageAsync();
        Assert.True(ioResult.IsSuccess);

        // Assert: 系统状态变更为故障
        Assert.Equal(SystemRunState.Fault, stateService.Current);

        // Assert: 停止联动 IO 已写入
        Assert.Equal(2, fakeFieldBus.WriteHistory.Count(w => w.value == false));

        // Assert: 创建包裹应被拒绝
        var exception = Assert.Throws<InvalidOperationException>(() =>
            parcelService.CreateParcel(new ParcelId(1000000000003), "TEST003", DateTimeOffset.Now));
        Assert.Contains("故障状态", exception.Message);

        fakeFieldBus.ClearHistory();

        // Act: 在故障状态下尝试所有按钮
        var startResult = stateService.TryHandleStart();
        var stopResult = stateService.TryHandleStop();
        var emergencyStopResult2 = stateService.TryHandleEmergencyStop();

        // Assert: 所有按钮都无效
        Assert.False(startResult.IsSuccess);
        Assert.False(stopResult.IsSuccess);
        Assert.False(emergencyStopResult2.IsSuccess);

        // Assert: 状态保持故障
        Assert.Equal(SystemRunState.Fault, stateService.Current);

        // Assert: 没有任何 IO 写入
        Assert.Empty(fakeFieldBus.WriteHistory);
    }

    [Fact]
    public async Task Scenario5_EmergencyResetToReadyState_ShouldAllowNormalOperations()
    {
        // Arrange
        var options = Options.Create(new PanelIoLinkageOptions
        {
            StartFollowOutputChannels = new List<int> { 100 },
            StopFollowOutputChannels = new List<int> { 200 }
        });
        var fakeFieldBus = new FakeFieldBusClientWithTracking();
        var ioCoordinator = new PanelIoCoordinator(fakeFieldBus, options, CreateLogger());
        var stateService = new SystemRunStateService();

        // 先触发急停
        stateService.TryHandleEmergencyStop();
        Assert.Equal(SystemRunState.Fault, stateService.Current);

        // Act: 解除急停
        var resetResult = stateService.TryHandleEmergencyReset();

        // Assert: 解除成功
        Assert.True(resetResult.IsSuccess);

        // Assert: 系统状态变更为停止
        Assert.Equal(SystemRunState.Stopped, stateService.Current);

        // Act: 在停止状态下测试场景 1、2、3
        
        // 测试启动
        var startResult = stateService.TryHandleStart();
        Assert.True(startResult.IsSuccess);
        Assert.Equal(SystemRunState.Running, stateService.Current);
        
        await ioCoordinator.ExecuteStartLinkageAsync();
        Assert.Single(fakeFieldBus.WriteHistory.Where(w => w.address == 100 && w.value == true));

        // 测试停止
        fakeFieldBus.ClearHistory();
        var stopResult = stateService.TryHandleStop();
        Assert.True(stopResult.IsSuccess);
        Assert.Equal(SystemRunState.Stopped, stateService.Current);
        
        await ioCoordinator.ExecuteStopLinkageAsync();
        Assert.Single(fakeFieldBus.WriteHistory.Where(w => w.address == 200 && w.value == false));

        // 测试再次启动
        fakeFieldBus.ClearHistory();
        var startResult2 = stateService.TryHandleStart();
        Assert.True(startResult2.IsSuccess);
        Assert.Equal(SystemRunState.Running, stateService.Current);
    }
}
