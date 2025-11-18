using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Observability;
using Microsoft.Extensions.Logging.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Communication;
using ZakYip.NarrowBeltDiverterSorter.Ingress.Chute;

namespace ZakYip.NarrowBeltDiverterSorter.Ingress.Tests.Chute;

/// <summary>
/// ChuteIoMonitor测试
/// </summary>
public class ChuteIoMonitorTests
{
    /// <summary>
    /// Mock现场总线客户端
    /// </summary>
    private class MockFieldBusClient : IFieldBusClient
    {
        private bool _isConnected;
        private readonly Dictionary<int, bool> _discreteInputs = new();

        public Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            _isConnected = true;
            return Task.FromResult(true);
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            _isConnected = false;
            return Task.CompletedTask;
        }

        public void SetDiscreteInput(int address, bool value)
        {
            _discreteInputs[address] = value;
        }

        public Task<bool[]?> ReadDiscreteInputsAsync(int address, int count, CancellationToken cancellationToken = default)
        {
            if (!_isConnected) return Task.FromResult<bool[]?>(null);

            var result = new bool[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = _discreteInputs.TryGetValue(address + i, out var value) && value;
            }
            return Task.FromResult<bool[]?>(result);
        }

        public Task<bool> WriteSingleCoilAsync(int address, bool value, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> WriteMultipleCoilsAsync(int startAddress, bool[] values, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> WriteSingleRegisterAsync(int address, ushort value, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> WriteMultipleRegistersAsync(int startAddress, ushort[] values, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool[]?> ReadCoilsAsync(int address, int count, CancellationToken cancellationToken = default) => Task.FromResult<bool[]?>(new bool[count]);
        public Task<ushort[]?> ReadHoldingRegistersAsync(int address, int count, CancellationToken cancellationToken = default) => Task.FromResult<ushort[]?>(new ushort[count]);
        public Task<ushort[]?> ReadInputRegistersAsync(int address, int count, CancellationToken cancellationToken = default) => Task.FromResult<ushort[]?>(new ushort[count]);
        public bool IsConnected() => _isConnected;
    }

    /// <summary>
    /// Mock日志记录器，捕获日志消息
    /// </summary>
    private class MockLogger : ILogger<ChuteIoMonitor>
    {
        public List<string> LogMessages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LogMessages.Add(formatter(state, exception));
        }
    }

    [Fact]
    public async Task ChuteIoMonitor_Should_Start_And_Stop_Successfully()
    {
        // Arrange
        var mockClient = new MockFieldBusClient();
        await mockClient.ConnectAsync();

        var config = new ChuteIoMonitorConfiguration
        {
            PollingInterval = TimeSpan.FromMilliseconds(10),
            MonitoredChuteIds = new List<long> { 1, 2, 3 },
            ChuteIoAddressMap = new Dictionary<long, int>
            {
                { 1, 100 },
                { 2, 101 },
                { 3, 102 }
            }
        };

        var logger = new MockLogger();
        var monitor = new ChuteIoMonitor(mockClient, config, new MockEventBus(), logger);

        // Act
        await monitor.StartAsync();
        await Task.Delay(50); // Let it run for a bit
        await monitor.StopAsync();

        // Assert
        Assert.Contains(logger.LogMessages, m => m.Contains("启动格口IO监视器"));
        Assert.Contains(logger.LogMessages, m => m.Contains("格口IO监视器已停止"));
    }

    [Fact]
    public async Task ChuteIoMonitor_Should_Detect_State_Changes()
    {
        // Arrange
        var mockClient = new MockFieldBusClient();
        await mockClient.ConnectAsync();

        var config = new ChuteIoMonitorConfiguration
        {
            PollingInterval = TimeSpan.FromMilliseconds(10),
            MonitoredChuteIds = new List<long> { 1 },
            ChuteIoAddressMap = new Dictionary<long, int>
            {
                { 1, 100 }
            }
        };

        var logger = new MockLogger();
        var monitor = new ChuteIoMonitor(mockClient, config, new MockEventBus(), logger);

        // Act
        await monitor.StartAsync();
        await Task.Delay(30); // Let it poll once

        // Change state
        mockClient.SetDiscreteInput(100, true);
        await Task.Delay(30); // Let it detect the change

        await monitor.StopAsync();

        // Assert
        Assert.Contains(logger.LogMessages, m => m.Contains("格口 1 IO状态变化"));
    }

    [Fact]
    public async Task ChuteIoMonitor_Should_Handle_Unmapped_Chute()
    {
        // Arrange
        var mockClient = new MockFieldBusClient();
        await mockClient.ConnectAsync();

        var config = new ChuteIoMonitorConfiguration
        {
            PollingInterval = TimeSpan.FromMilliseconds(10),
            MonitoredChuteIds = new List<long> { 1, 999 }, // 999 is not mapped
            ChuteIoAddressMap = new Dictionary<long, int>
            {
                { 1, 100 }
            }
        };

        var logger = new MockLogger();
        var monitor = new ChuteIoMonitor(mockClient, config, new MockEventBus(), logger);

        // Act
        await monitor.StartAsync();
        await Task.Delay(30);
        await monitor.StopAsync();

        // Assert
        Assert.Contains(logger.LogMessages, m => m.Contains("格口 999 未配置IO地址映射"));
    }

    [Fact]
    public async Task ChuteIoMonitor_Should_Handle_Disconnected_Client()
    {
        // Arrange
        var mockClient = new MockFieldBusClient();
        // Don't connect the client

        var config = new ChuteIoMonitorConfiguration
        {
            PollingInterval = TimeSpan.FromMilliseconds(10),
            MonitoredChuteIds = new List<long> { 1 },
            ChuteIoAddressMap = new Dictionary<long, int>
            {
                { 1, 100 }
            }
        };

        var logger = new MockLogger();
        var monitor = new ChuteIoMonitor(mockClient, config, new MockEventBus(), logger);

        // Act
        await monitor.StartAsync();
        await Task.Delay(30);
        await monitor.StopAsync();

        // Assert
        Assert.Contains(logger.LogMessages, m => m.Contains("现场总线未连接"));
    }

    [Fact]
    public async Task ChuteIoMonitor_Should_Not_Start_Twice()
    {
        // Arrange
        var mockClient = new MockFieldBusClient();
        await mockClient.ConnectAsync();

        var config = new ChuteIoMonitorConfiguration
        {
            PollingInterval = TimeSpan.FromMilliseconds(10),
            MonitoredChuteIds = new List<long> { 1 },
            ChuteIoAddressMap = new Dictionary<long, int> { { 1, 100 } }
        };

        var logger = new MockLogger();
        var monitor = new ChuteIoMonitor(mockClient, config, new MockEventBus(), logger);

        // Act
        await monitor.StartAsync();
        await monitor.StartAsync(); // Try to start again
        await Task.Delay(30);
        await monitor.StopAsync();

        // Assert
        Assert.Contains(logger.LogMessages, m => m.Contains("格口IO监视器已经在运行中"));
    }
}

/// <summary>
/// Mock事件总线（仅用于测试）
/// </summary>
internal class MockEventBus : IEventBus
{
    public void Subscribe<TEventArgs>(Func<TEventArgs, CancellationToken, Task> handler) where TEventArgs : class { }
    public void Unsubscribe<TEventArgs>(Func<TEventArgs, CancellationToken, Task> handler) where TEventArgs : class { }
    public Task PublishAsync<TEventArgs>(TEventArgs eventArgs, CancellationToken cancellationToken = default) where TEventArgs : class => Task.CompletedTask;
    public int GetBacklogCount() => 0;
}
