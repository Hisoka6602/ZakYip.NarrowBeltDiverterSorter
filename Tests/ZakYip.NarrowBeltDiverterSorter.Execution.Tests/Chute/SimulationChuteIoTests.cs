using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Execution.Chute;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Tests.Chute;

/// <summary>
/// SimulationChuteIo测试
/// </summary>
public class SimulationChuteIoTests
{
    /// <summary>
    /// Mock日志记录器（SimulationChuteIoEndpoint）
    /// </summary>
    private class MockEndpointLogger : ILogger<SimulationChuteIoEndpoint>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    /// <summary>
    /// Mock日志记录器（SimulationChuteIoService）
    /// </summary>
    private class MockServiceLogger : ILogger<SimulationChuteIoService>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    [Fact]
    public async Task SimulationChuteIoEndpoint_Should_Accept_Valid_Channel_Index()
    {
        // Arrange
        var logger = new MockEndpointLogger();
        var endpoint = new SimulationChuteIoEndpoint("test-node", 32, logger);

        // Act & Assert - Should not throw
        await endpoint.SetChannelAsync(1, true);
        await endpoint.SetChannelAsync(32, true);
        await endpoint.SetChannelAsync(16, false);
        
        Assert.Equal("test-node", endpoint.EndpointKey);
    }

    [Fact]
    public async Task SimulationChuteIoEndpoint_Should_Handle_Invalid_Channel_Index()
    {
        // Arrange
        var logger = new MockEndpointLogger();
        var endpoint = new SimulationChuteIoEndpoint("test-node", 32, logger);

        // Act & Assert - Should not throw, but log warning
        await endpoint.SetChannelAsync(0, true);
        await endpoint.SetChannelAsync(33, true);
        await endpoint.SetChannelAsync(-1, false);
        
        // No exception thrown
        Assert.True(true);
    }

    [Fact]
    public async Task SimulationChuteIoEndpoint_SetAllAsync_Should_Complete()
    {
        // Arrange
        var logger = new MockEndpointLogger();
        var endpoint = new SimulationChuteIoEndpoint("test-node", 32, logger);

        // Act & Assert - Should not throw
        await endpoint.SetAllAsync(true);
        await endpoint.SetAllAsync(false);
        
        Assert.True(true);
    }

    [Fact]
    public async Task SimulationChuteIoService_Should_Open_Mapped_Chute()
    {
        // Arrange
        var endpointLogger = new MockEndpointLogger();
        var endpoint = new SimulationChuteIoEndpoint("node-1", 32, endpointLogger);
        
        var chuteMapping = new Dictionary<long, (IChuteIoEndpoint endpoint, int channelIndex)>
        {
            { 1, (endpoint, 1) },
            { 2, (endpoint, 2) },
            { 3, (endpoint, 3) }
        };

        var serviceLogger = new MockServiceLogger();
        var service = new SimulationChuteIoService(new[] { endpoint }, chuteMapping, serviceLogger);

        // Act & Assert - Should not throw
        await service.OpenAsync(1);
        await service.OpenAsync(2);
        await service.OpenAsync(3);
        
        Assert.True(true);
    }

    [Fact]
    public async Task SimulationChuteIoService_Should_Close_Mapped_Chute()
    {
        // Arrange
        var endpointLogger = new MockEndpointLogger();
        var endpoint = new SimulationChuteIoEndpoint("node-1", 32, endpointLogger);
        
        var chuteMapping = new Dictionary<long, (IChuteIoEndpoint endpoint, int channelIndex)>
        {
            { 1, (endpoint, 1) },
            { 2, (endpoint, 2) }
        };

        var serviceLogger = new MockServiceLogger();
        var service = new SimulationChuteIoService(new[] { endpoint }, chuteMapping, serviceLogger);

        // Act & Assert - Should not throw
        await service.CloseAsync(1);
        await service.CloseAsync(2);
        
        Assert.True(true);
    }

    [Fact]
    public async Task SimulationChuteIoService_Should_Handle_Unmapped_Chute()
    {
        // Arrange
        var endpointLogger = new MockEndpointLogger();
        var endpoint = new SimulationChuteIoEndpoint("node-1", 32, endpointLogger);
        
        var chuteMapping = new Dictionary<long, (IChuteIoEndpoint endpoint, int channelIndex)>
        {
            { 1, (endpoint, 1) }
        };

        var serviceLogger = new MockServiceLogger();
        var service = new SimulationChuteIoService(new[] { endpoint }, chuteMapping, serviceLogger);

        // Act & Assert - Should not throw, but log warning
        await service.OpenAsync(999);
        await service.CloseAsync(999);
        
        Assert.True(true);
    }

    [Fact]
    public async Task SimulationChuteIoService_Should_CloseAll_Endpoints()
    {
        // Arrange
        var endpointLogger = new MockEndpointLogger();
        var endpoint1 = new SimulationChuteIoEndpoint("node-1", 32, endpointLogger);
        var endpoint2 = new SimulationChuteIoEndpoint("node-2", 16, endpointLogger);
        
        var chuteMapping = new Dictionary<long, (IChuteIoEndpoint endpoint, int channelIndex)>
        {
            { 1, (endpoint1, 1) },
            { 2, (endpoint2, 1) }
        };

        var serviceLogger = new MockServiceLogger();
        var service = new SimulationChuteIoService(new[] { endpoint1, endpoint2 }, chuteMapping, serviceLogger);

        // Act & Assert - Should not throw
        await service.CloseAllAsync();
        
        Assert.True(true);
    }

    [Fact]
    public async Task SimulationChuteIoService_Should_Support_Multiple_Endpoints()
    {
        // Arrange
        var endpointLogger = new MockEndpointLogger();
        var endpoint1 = new SimulationChuteIoEndpoint("node-1", 32, endpointLogger);
        var endpoint2 = new SimulationChuteIoEndpoint("node-2", 32, endpointLogger);
        var endpoint3 = new SimulationChuteIoEndpoint("node-3", 16, endpointLogger);
        
        var chuteMapping = new Dictionary<long, (IChuteIoEndpoint endpoint, int channelIndex)>
        {
            { 1, (endpoint1, 1) },
            { 2, (endpoint1, 2) },
            { 33, (endpoint2, 1) },
            { 34, (endpoint2, 2) },
            { 65, (endpoint3, 1) }
        };

        var serviceLogger = new MockServiceLogger();
        var service = new SimulationChuteIoService(
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
    }
}
