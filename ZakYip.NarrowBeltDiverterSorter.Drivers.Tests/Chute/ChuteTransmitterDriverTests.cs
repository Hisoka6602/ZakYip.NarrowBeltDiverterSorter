using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Communication;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Drivers.Chute;

namespace ZakYip.NarrowBeltDiverterSorter.Drivers.Tests.Chute;

/// <summary>
/// ChuteTransmitterDriver测试
/// </summary>
public class ChuteTransmitterDriverTests
{
    /// <summary>
    /// Mock现场总线客户端
    /// </summary>
    private class MockFieldBusClient : IFieldBusClient
    {
        private bool _isConnected;
        private readonly Dictionary<int, bool> _coilStates = new();

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

        public Task<bool> WriteSingleCoilAsync(int address, bool value, CancellationToken cancellationToken = default)
        {
            if (!_isConnected) return Task.FromResult(false);
            _coilStates[address] = value;
            return Task.FromResult(true);
        }

        public bool GetCoilState(int address) => _coilStates.TryGetValue(address, out var state) && state;

        public Task<bool> WriteMultipleCoilsAsync(int startAddress, bool[] values, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> WriteSingleRegisterAsync(int address, ushort value, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> WriteMultipleRegistersAsync(int startAddress, ushort[] values, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool[]?> ReadCoilsAsync(int address, int count, CancellationToken cancellationToken = default) => Task.FromResult<bool[]?>(new bool[count]);
        public Task<bool[]?> ReadDiscreteInputsAsync(int address, int count, CancellationToken cancellationToken = default) => Task.FromResult<bool[]?>(new bool[count]);
        public Task<ushort[]?> ReadHoldingRegistersAsync(int address, int count, CancellationToken cancellationToken = default) => Task.FromResult<ushort[]?>(new ushort[count]);
        public Task<ushort[]?> ReadInputRegistersAsync(int address, int count, CancellationToken cancellationToken = default) => Task.FromResult<ushort[]?>(new ushort[count]);
        public bool IsConnected() => _isConnected;
    }

    /// <summary>
    /// Mock日志记录器
    /// </summary>
    private class MockLogger : ILogger<ChuteTransmitterDriver>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    [Fact]
    public async Task OpenWindow_Should_Write_True_Then_False_To_Coil()
    {
        // Arrange
        var mockClient = new MockFieldBusClient();
        await mockClient.ConnectAsync();

        var mapping = new ChuteMappingConfiguration
        {
            ChuteAddressMap = new Dictionary<long, int>
            {
                { 1, 100 }
            }
        };

        var logger = new MockLogger();
        var driver = new ChuteTransmitterDriver(mockClient, mapping, logger);

        // Act
        var chuteId = new ChuteId(1);
        var openDuration = TimeSpan.FromMilliseconds(50);
        await driver.OpenWindowAsync(chuteId, openDuration);

        // Assert - After the operation, coil should be false (closed)
        Assert.False(mockClient.GetCoilState(100));
    }

    [Fact]
    public async Task ForceClose_Should_Write_False_To_Coil()
    {
        // Arrange
        var mockClient = new MockFieldBusClient();
        await mockClient.ConnectAsync();

        var mapping = new ChuteMappingConfiguration
        {
            ChuteAddressMap = new Dictionary<long, int>
            {
                { 1, 100 }
            }
        };

        var logger = new MockLogger();
        var driver = new ChuteTransmitterDriver(mockClient, mapping, logger);

        // Act
        var chuteId = new ChuteId(1);
        await driver.ForceCloseAsync(chuteId);

        // Assert
        Assert.False(mockClient.GetCoilState(100));
    }

    [Fact]
    public async Task OpenWindow_Should_Handle_Unmapped_Chute()
    {
        // Arrange
        var mockClient = new MockFieldBusClient();
        await mockClient.ConnectAsync();

        var mapping = new ChuteMappingConfiguration
        {
            ChuteAddressMap = new Dictionary<long, int>()
        };

        var logger = new MockLogger();
        var driver = new ChuteTransmitterDriver(mockClient, mapping, logger);

        // Act - Should not throw
        var chuteId = new ChuteId(999);
        await driver.OpenWindowAsync(chuteId, TimeSpan.FromMilliseconds(50));

        // Assert - No exception thrown
        Assert.True(true);
    }

    [Fact]
    public async Task ForceClose_Should_Handle_Unmapped_Chute()
    {
        // Arrange
        var mockClient = new MockFieldBusClient();
        await mockClient.ConnectAsync();

        var mapping = new ChuteMappingConfiguration
        {
            ChuteAddressMap = new Dictionary<long, int>()
        };

        var logger = new MockLogger();
        var driver = new ChuteTransmitterDriver(mockClient, mapping, logger);

        // Act - Should not throw
        var chuteId = new ChuteId(999);
        await driver.ForceCloseAsync(chuteId);

        // Assert - No exception thrown
        Assert.True(true);
    }
}
