using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Communication;

namespace ZakYip.NarrowBeltDiverterSorter.Communication.Tests;

/// <summary>
/// FieldBusClient测试
/// </summary>
public class FieldBusClientTests
{
    /// <summary>
    /// Mock日志记录器
    /// </summary>
    private class MockLogger : ILogger<FieldBusClient>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    [Fact]
    public async Task FieldBusClient_Should_Connect_Successfully()
    {
        // Arrange
        var config = new FieldBusClientConfiguration
        {
            IpAddress = "192.168.1.100",
            Port = 502,
            SlaveId = 1
        };
        var logger = new MockLogger();
        var client = new FieldBusClient(config, logger);

        // Act
        var result = await client.ConnectAsync();

        // Assert
        Assert.True(result);
        Assert.True(client.IsConnected());
    }

    [Fact]
    public async Task FieldBusClient_Should_Disconnect_Successfully()
    {
        // Arrange
        var config = new FieldBusClientConfiguration();
        var logger = new MockLogger();
        var client = new FieldBusClient(config, logger);
        await client.ConnectAsync();

        // Act
        await client.DisconnectAsync();

        // Assert
        Assert.False(client.IsConnected());
    }

    [Fact]
    public async Task WriteSingleCoil_Should_Return_False_When_Not_Connected()
    {
        // Arrange
        var config = new FieldBusClientConfiguration();
        var logger = new MockLogger();
        var client = new FieldBusClient(config, logger);

        // Act
        var result = await client.WriteSingleCoilAsync(100, true);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task WriteSingleCoil_Should_Return_True_When_Connected()
    {
        // Arrange
        var config = new FieldBusClientConfiguration();
        var logger = new MockLogger();
        var client = new FieldBusClient(config, logger);
        await client.ConnectAsync();

        // Act
        var result = await client.WriteSingleCoilAsync(100, true);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task WriteMultipleCoils_Should_Return_True_When_Connected()
    {
        // Arrange
        var config = new FieldBusClientConfiguration();
        var logger = new MockLogger();
        var client = new FieldBusClient(config, logger);
        await client.ConnectAsync();

        // Act
        var values = new[] { true, false, true, true };
        var result = await client.WriteMultipleCoilsAsync(100, values);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task WriteSingleRegister_Should_Return_True_When_Connected()
    {
        // Arrange
        var config = new FieldBusClientConfiguration();
        var logger = new MockLogger();
        var client = new FieldBusClient(config, logger);
        await client.ConnectAsync();

        // Act
        var result = await client.WriteSingleRegisterAsync(1000, 12345);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task WriteMultipleRegisters_Should_Return_True_When_Connected()
    {
        // Arrange
        var config = new FieldBusClientConfiguration();
        var logger = new MockLogger();
        var client = new FieldBusClient(config, logger);
        await client.ConnectAsync();

        // Act
        var values = new ushort[] { 100, 200, 300, 400 };
        var result = await client.WriteMultipleRegistersAsync(1000, values);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ReadCoils_Should_Return_Array_When_Connected()
    {
        // Arrange
        var config = new FieldBusClientConfiguration();
        var logger = new MockLogger();
        var client = new FieldBusClient(config, logger);
        await client.ConnectAsync();

        // Act
        var result = await client.ReadCoilsAsync(100, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Length);
    }

    [Fact]
    public async Task ReadDiscreteInputs_Should_Return_Array_When_Connected()
    {
        // Arrange
        var config = new FieldBusClientConfiguration();
        var logger = new MockLogger();
        var client = new FieldBusClient(config, logger);
        await client.ConnectAsync();

        // Act
        var result = await client.ReadDiscreteInputsAsync(100, 8);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(8, result.Length);
    }

    [Fact]
    public async Task ReadHoldingRegisters_Should_Return_Array_When_Connected()
    {
        // Arrange
        var config = new FieldBusClientConfiguration();
        var logger = new MockLogger();
        var client = new FieldBusClient(config, logger);
        await client.ConnectAsync();

        // Act
        var result = await client.ReadHoldingRegistersAsync(1000, 5);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Length);
    }

    [Fact]
    public async Task ReadInputRegisters_Should_Return_Array_When_Connected()
    {
        // Arrange
        var config = new FieldBusClientConfiguration();
        var logger = new MockLogger();
        var client = new FieldBusClient(config, logger);
        await client.ConnectAsync();

        // Act
        var result = await client.ReadInputRegistersAsync(2000, 3);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
    }

    [Fact]
    public async Task Read_Operations_Should_Return_Null_When_Not_Connected()
    {
        // Arrange
        var config = new FieldBusClientConfiguration();
        var logger = new MockLogger();
        var client = new FieldBusClient(config, logger);

        // Act
        var coils = await client.ReadCoilsAsync(100, 10);
        var inputs = await client.ReadDiscreteInputsAsync(100, 10);
        var holdingRegs = await client.ReadHoldingRegistersAsync(1000, 5);
        var inputRegs = await client.ReadInputRegistersAsync(2000, 5);

        // Assert
        Assert.Null(coils);
        Assert.Null(inputs);
        Assert.Null(holdingRegs);
        Assert.Null(inputRegs);
    }
}
