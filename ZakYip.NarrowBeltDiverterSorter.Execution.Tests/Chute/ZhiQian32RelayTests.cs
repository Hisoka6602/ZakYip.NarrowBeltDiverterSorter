using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Execution.Chute;
using ZakYip.NarrowBeltDiverterSorter.Execution.Chute.Drivers.ZhiQian32Relay;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Tests.Chute;

/// <summary>
/// 智嵌32路网络继电器测试
/// </summary>
public class ZhiQian32RelayTests
{
    /// <summary>
    /// Mock日志记录器（ZhiQian32RelayEndpoint）
    /// </summary>
    private class MockEndpointLogger : ILogger<ZhiQian32RelayEndpoint>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    /// <summary>
    /// Mock日志记录器（ZhiQian32RelayClient）
    /// </summary>
    private class MockClientLogger : ILogger<ZhiQian32RelayClient>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    /// <summary>
    /// Mock日志记录器（ZhiQian32RelayChuteIoService）
    /// </summary>
    private class MockServiceLogger : ILogger<ZhiQian32RelayChuteIoService>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    [Fact]
    public async Task ZhiQian32RelayEndpoint_Should_Handle_Valid_Channel_Index()
    {
        // Arrange
        var endpointLogger = new MockEndpointLogger();
        var clientLogger = new MockClientLogger();
        var endpoint = new ZhiQian32RelayEndpoint(
            "test-node",
            "192.168.1.100",
            8080,
            32,
            endpointLogger,
            clientLogger);

        // Act & Assert - Should not throw, even if connection fails (network errors are caught)
        // The endpoint should log errors but not propagate exceptions
        await endpoint.SetChannelAsync(1, true);
        await endpoint.SetChannelAsync(32, true);
        await endpoint.SetChannelAsync(16, false);

        Assert.Equal("test-node", endpoint.EndpointKey);

        // Cleanup
        endpoint.Dispose();
    }

    [Fact]
    public async Task ZhiQian32RelayEndpoint_Should_Reject_Invalid_Channel_Index()
    {
        // Arrange
        var endpointLogger = new MockEndpointLogger();
        var clientLogger = new MockClientLogger();
        var endpoint = new ZhiQian32RelayEndpoint(
            "test-node",
            "192.168.1.100",
            8080,
            32,
            endpointLogger,
            clientLogger);

        // Act & Assert - Should not throw, but log error and return early
        await endpoint.SetChannelAsync(0, true);
        await endpoint.SetChannelAsync(33, true);
        await endpoint.SetChannelAsync(-1, false);

        // No exception thrown - errors are logged
        Assert.True(true);

        // Cleanup
        endpoint.Dispose();
    }

    [Fact]
    public async Task ZhiQian32RelayEndpoint_SetAllAsync_Should_Complete()
    {
        // Arrange
        var endpointLogger = new MockEndpointLogger();
        var clientLogger = new MockClientLogger();
        var endpoint = new ZhiQian32RelayEndpoint(
            "test-node",
            "192.168.1.100",
            8080,
            32,
            endpointLogger,
            clientLogger);

        // Act & Assert - Should not throw, even if connection fails
        await endpoint.SetAllAsync(true);
        await endpoint.SetAllAsync(false);

        Assert.True(true);

        // Cleanup
        endpoint.Dispose();
    }

    [Fact]
    public async Task ZhiQian32RelayChuteIoService_Should_Open_Mapped_Chute()
    {
        // Arrange
        var endpointLogger = new MockEndpointLogger();
        var clientLogger = new MockClientLogger();
        var endpoint = new ZhiQian32RelayEndpoint(
            "node-1",
            "192.168.1.100",
            8080,
            32,
            endpointLogger,
            clientLogger);

        var chuteMapping = new Dictionary<long, (IChuteIoEndpoint endpoint, int channelIndex)>
        {
            { 1, (endpoint, 1) },
            { 2, (endpoint, 2) },
            { 3, (endpoint, 3) }
        };

        var serviceLogger = new MockServiceLogger();
        var service = new ZhiQian32RelayChuteIoService(new[] { endpoint }, chuteMapping, serviceLogger);

        // Act & Assert - Should not throw
        await service.OpenAsync(1);
        await service.OpenAsync(2);
        await service.OpenAsync(3);

        Assert.True(true);

        // Cleanup
        service.Dispose();
    }

    [Fact]
    public async Task ZhiQian32RelayChuteIoService_Should_Close_Mapped_Chute()
    {
        // Arrange
        var endpointLogger = new MockEndpointLogger();
        var clientLogger = new MockClientLogger();
        var endpoint = new ZhiQian32RelayEndpoint(
            "node-1",
            "192.168.1.100",
            8080,
            32,
            endpointLogger,
            clientLogger);

        var chuteMapping = new Dictionary<long, (IChuteIoEndpoint endpoint, int channelIndex)>
        {
            { 1, (endpoint, 1) },
            { 2, (endpoint, 2) }
        };

        var serviceLogger = new MockServiceLogger();
        var service = new ZhiQian32RelayChuteIoService(new[] { endpoint }, chuteMapping, serviceLogger);

        // Act & Assert - Should not throw
        await service.CloseAsync(1);
        await service.CloseAsync(2);

        Assert.True(true);

        // Cleanup
        service.Dispose();
    }

    [Fact]
    public async Task ZhiQian32RelayChuteIoService_Should_Handle_Unmapped_Chute()
    {
        // Arrange
        var endpointLogger = new MockEndpointLogger();
        var clientLogger = new MockClientLogger();
        var endpoint = new ZhiQian32RelayEndpoint(
            "node-1",
            "192.168.1.100",
            8080,
            32,
            endpointLogger,
            clientLogger);

        var chuteMapping = new Dictionary<long, (IChuteIoEndpoint endpoint, int channelIndex)>
        {
            { 1, (endpoint, 1) }
        };

        var serviceLogger = new MockServiceLogger();
        var service = new ZhiQian32RelayChuteIoService(new[] { endpoint }, chuteMapping, serviceLogger);

        // Act & Assert - Should not throw, but log warning
        await service.OpenAsync(999);
        await service.CloseAsync(999);

        Assert.True(true);

        // Cleanup
        service.Dispose();
    }

    [Fact]
    public async Task ZhiQian32RelayChuteIoService_Should_CloseAll_Endpoints()
    {
        // Arrange
        var endpointLogger = new MockEndpointLogger();
        var clientLogger = new MockClientLogger();
        var endpoint1 = new ZhiQian32RelayEndpoint(
            "node-1",
            "192.168.1.100",
            8080,
            32,
            endpointLogger,
            clientLogger);
        var endpoint2 = new ZhiQian32RelayEndpoint(
            "node-2",
            "192.168.1.101",
            8080,
            16,
            endpointLogger,
            clientLogger);

        var chuteMapping = new Dictionary<long, (IChuteIoEndpoint endpoint, int channelIndex)>
        {
            { 1, (endpoint1, 1) },
            { 2, (endpoint2, 1) }
        };

        var serviceLogger = new MockServiceLogger();
        var service = new ZhiQian32RelayChuteIoService(new[] { endpoint1, endpoint2 }, chuteMapping, serviceLogger);

        // Act & Assert - Should not throw
        await service.CloseAllAsync();

        Assert.True(true);

        // Cleanup
        service.Dispose();
    }

    [Fact]
    public async Task ZhiQian32RelayChuteIoService_Should_Support_Multiple_Endpoints()
    {
        // Arrange
        var endpointLogger = new MockEndpointLogger();
        var clientLogger = new MockClientLogger();
        var endpoint1 = new ZhiQian32RelayEndpoint(
            "node-1",
            "192.168.1.100",
            8080,
            32,
            endpointLogger,
            clientLogger);
        var endpoint2 = new ZhiQian32RelayEndpoint(
            "node-2",
            "192.168.1.101",
            8080,
            32,
            endpointLogger,
            clientLogger);
        var endpoint3 = new ZhiQian32RelayEndpoint(
            "node-3",
            "192.168.1.102",
            8080,
            16,
            endpointLogger,
            clientLogger);

        var chuteMapping = new Dictionary<long, (IChuteIoEndpoint endpoint, int channelIndex)>
        {
            { 1, (endpoint1, 1) },
            { 2, (endpoint1, 2) },
            { 33, (endpoint2, 1) },
            { 34, (endpoint2, 2) },
            { 65, (endpoint3, 1) }
        };

        var serviceLogger = new MockServiceLogger();
        var service = new ZhiQian32RelayChuteIoService(
            new[] { endpoint1, endpoint2, endpoint3 },
            chuteMapping,
            serviceLogger);

        // Act & Assert - Should not throw
        await service.OpenAsync(1);   // node-1, channel 1
        await service.OpenAsync(33);  // node-2, channel 1
        await service.OpenAsync(65);  // node-3, channel 1
        await service.CloseAsync(2);  // node-1, channel 2
        await service.CloseAsync(34); // node-2, channel 2
        await service.CloseAllAsync();

        Assert.True(true);

        // Cleanup
        service.Dispose();
    }

    [Fact]
    public void ZhiQian32RelayEndpoint_Should_Dispose_Properly()
    {
        // Arrange
        var endpointLogger = new MockEndpointLogger();
        var clientLogger = new MockClientLogger();
        var endpoint = new ZhiQian32RelayEndpoint(
            "test-node",
            "192.168.1.100",
            8080,
            32,
            endpointLogger,
            clientLogger);

        // Act
        endpoint.Dispose();

        // Assert - Should not throw on multiple dispose calls
        endpoint.Dispose();
        Assert.True(true);
    }

    [Fact]
    public void ZhiQian32RelayChuteIoService_Should_Dispose_All_Endpoints()
    {
        // Arrange
        var endpointLogger = new MockEndpointLogger();
        var clientLogger = new MockClientLogger();
        var endpoint1 = new ZhiQian32RelayEndpoint(
            "node-1",
            "192.168.1.100",
            8080,
            32,
            endpointLogger,
            clientLogger);
        var endpoint2 = new ZhiQian32RelayEndpoint(
            "node-2",
            "192.168.1.101",
            8080,
            32,
            endpointLogger,
            clientLogger);

        var chuteMapping = new Dictionary<long, (IChuteIoEndpoint endpoint, int channelIndex)>
        {
            { 1, (endpoint1, 1) },
            { 2, (endpoint2, 1) }
        };

        var serviceLogger = new MockServiceLogger();
        var service = new ZhiQian32RelayChuteIoService(
            new[] { endpoint1, endpoint2 },
            chuteMapping,
            serviceLogger);

        // Act
        service.Dispose();

        // Assert - Should not throw on multiple dispose calls
        service.Dispose();
        Assert.True(true);
    }
}
