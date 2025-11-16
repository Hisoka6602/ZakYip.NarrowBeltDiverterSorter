using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Communication;
using ZakYip.NarrowBeltDiverterSorter.Drivers.Cart;

namespace ZakYip.NarrowBeltDiverterSorter.Drivers.Tests.Cart;

/// <summary>
/// CartParameterDriver测试
/// </summary>
public class CartParameterDriverTests
{
    /// <summary>
    /// Mock现场总线客户端
    /// </summary>
    private class MockFieldBusClient : IFieldBusClient
    {
        private bool _isConnected;
        private readonly Dictionary<int, ushort> _registerValues = new();

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

        public Task<bool> WriteSingleRegisterAsync(int address, ushort value, CancellationToken cancellationToken = default)
        {
            if (!_isConnected) return Task.FromResult(false);
            _registerValues[address] = value;
            return Task.FromResult(true);
        }

        public ushort GetRegisterValue(int address) => _registerValues.TryGetValue(address, out var value) ? value : (ushort)0;

        public Task<bool> WriteSingleCoilAsync(int address, bool value, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> WriteMultipleCoilsAsync(int startAddress, bool[] values, CancellationToken cancellationToken = default) => Task.FromResult(true);
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
    private class MockLogger : ILogger<CartParameterDriver>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    [Fact]
    public async Task SetEjectionDistance_Should_Write_To_Register()
    {
        // Arrange
        var mockClient = new MockFieldBusClient();
        await mockClient.ConnectAsync();

        var config = new CartParameterRegisterConfiguration
        {
            EjectionDistanceRegisterAddress = 1000
        };

        var logger = new MockLogger();
        var driver = new CartParameterDriver(mockClient, config, logger);

        // Act
        var result = await driver.SetEjectionDistanceAsync(250.6);

        // Assert
        Assert.True(result);
        Assert.Equal(251, mockClient.GetRegisterValue(1000));
    }

    [Fact]
    public async Task SetEjectionDelay_Should_Write_To_Register()
    {
        // Arrange
        var mockClient = new MockFieldBusClient();
        await mockClient.ConnectAsync();

        var config = new CartParameterRegisterConfiguration
        {
            EjectionDelayRegisterAddress = 1001
        };

        var logger = new MockLogger();
        var driver = new CartParameterDriver(mockClient, config, logger);

        // Act
        var result = await driver.SetEjectionDelayAsync(500);

        // Assert
        Assert.True(result);
        Assert.Equal(500, mockClient.GetRegisterValue(1001));
    }

    [Fact]
    public async Task SetMaxConsecutiveActionCarts_Should_Write_To_Register()
    {
        // Arrange
        var mockClient = new MockFieldBusClient();
        await mockClient.ConnectAsync();

        var config = new CartParameterRegisterConfiguration
        {
            MaxConsecutiveActionCartsRegisterAddress = 1002
        };

        var logger = new MockLogger();
        var driver = new CartParameterDriver(mockClient, config, logger);

        // Act
        var result = await driver.SetMaxConsecutiveActionCartsAsync(10);

        // Assert
        Assert.True(result);
        Assert.Equal(10, mockClient.GetRegisterValue(1002));
    }

    [Fact]
    public async Task SetEjectionDistance_Should_Return_False_For_Negative_Value()
    {
        // Arrange
        var mockClient = new MockFieldBusClient();
        await mockClient.ConnectAsync();

        var config = new CartParameterRegisterConfiguration();
        var logger = new MockLogger();
        var driver = new CartParameterDriver(mockClient, config, logger);

        // Act
        var result = await driver.SetEjectionDistanceAsync(-10.0);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SetEjectionDelay_Should_Return_False_For_Negative_Value()
    {
        // Arrange
        var mockClient = new MockFieldBusClient();
        await mockClient.ConnectAsync();

        var config = new CartParameterRegisterConfiguration();
        var logger = new MockLogger();
        var driver = new CartParameterDriver(mockClient, config, logger);

        // Act
        var result = await driver.SetEjectionDelayAsync(-100);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SetMaxConsecutiveActionCarts_Should_Return_False_For_Negative_Value()
    {
        // Arrange
        var mockClient = new MockFieldBusClient();
        await mockClient.ConnectAsync();

        var config = new CartParameterRegisterConfiguration();
        var logger = new MockLogger();
        var driver = new CartParameterDriver(mockClient, config, logger);

        // Act
        var result = await driver.SetMaxConsecutiveActionCartsAsync(-5);

        // Assert
        Assert.False(result);
    }
}
