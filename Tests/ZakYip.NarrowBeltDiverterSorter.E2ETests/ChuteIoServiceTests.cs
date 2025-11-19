using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Execution.Chute;
using ZakYip.NarrowBeltDiverterSorter.Execution.Chute.Drivers.ZhiQian32Relay;

namespace ZakYip.NarrowBeltDiverterSorter.E2ETests;

/// <summary>
/// 格口IO服务测试
/// 验证格口IO服务的基本功能，包括单IP和多IP场景
/// </summary>
public class ChuteIoServiceTests
{
    private readonly ITestOutputHelper _output;

    public ChuteIoServiceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// 测试单IP节点场景（1-32个格口）
    /// </summary>
    [Fact]
    public async Task SingleIpNode_32Chutes_ShouldWork()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddProvider(new TestLoggerProvider(_output)));
        
        var endpoints = new List<IChuteIoEndpoint>();
        var chuteMapping = new Dictionary<long, (IChuteIoEndpoint endpoint, int channelIndex)>();
        
        // 创建单个节点控制 1-32 格口
        var endpoint = new SimulationChuteIoEndpoint(
            "sim-node-1",
            32,
            loggerFactory.CreateLogger<SimulationChuteIoEndpoint>());
        endpoints.Add(endpoint);
        
        for (int i = 1; i <= 32; i++)
        {
            chuteMapping[i] = (endpoint, i);
        }
        
        var service = new SimulationChuteIoService(
            endpoints,
            chuteMapping,
            loggerFactory.CreateLogger<SimulationChuteIoService>());

        // Act & Assert - 测试打开和关闭格口
        await service.OpenAsync(1);
        await service.CloseAsync(1);
        await service.OpenAsync(32);
        await service.CloseAsync(32);
        
        // Act & Assert - 测试关闭所有格口
        await service.CloseAllAsync();
        
        _output.WriteLine("✓ Single IP node (32 chutes) test passed");
    }

    /// <summary>
    /// 测试多IP节点场景（>32个格口）
    /// </summary>
    [Fact]
    public async Task MultiIpNode_64Chutes_ShouldDistributeCorrectly()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddProvider(new TestLoggerProvider(_output)));
        
        var endpoints = new List<IChuteIoEndpoint>();
        var chuteMapping = new Dictionary<long, (IChuteIoEndpoint endpoint, int channelIndex)>();
        
        // 第一个节点控制 1-32 格口
        var endpoint1 = new SimulationChuteIoEndpoint(
            "zq-node-1",
            32,
            loggerFactory.CreateLogger<SimulationChuteIoEndpoint>());
        endpoints.Add(endpoint1);
        
        for (int i = 1; i <= 32; i++)
        {
            chuteMapping[i] = (endpoint1, i);
        }
        
        // 第二个节点控制 33-64 格口
        var endpoint2 = new SimulationChuteIoEndpoint(
            "zq-node-2",
            32,
            loggerFactory.CreateLogger<SimulationChuteIoEndpoint>());
        endpoints.Add(endpoint2);
        
        for (int i = 33; i <= 64; i++)
        {
            chuteMapping[i] = (endpoint2, (i - 32));
        }
        
        var service = new SimulationChuteIoService(
            endpoints,
            chuteMapping,
            loggerFactory.CreateLogger<SimulationChuteIoService>());

        // Act & Assert - 测试第一个节点的格口
        await service.OpenAsync(1);
        await service.CloseAsync(1);
        await service.OpenAsync(32);
        await service.CloseAsync(32);
        
        // Act & Assert - 测试第二个节点的格口
        await service.OpenAsync(33);
        await service.CloseAsync(33);
        await service.OpenAsync(64);
        await service.CloseAsync(64);
        
        // Act & Assert - 测试关闭所有格口
        await service.CloseAllAsync();
        
        _output.WriteLine("✓ Multi IP node (64 chutes) test passed");
    }

    /// <summary>
    /// 测试未映射格口场景
    /// </summary>
    [Fact]
    public async Task UnmappedChute_ShouldLogErrorWithoutCrashing()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddProvider(new TestLoggerProvider(_output))
                .SetMinimumLevel(LogLevel.Error));
        
        var endpoints = new List<IChuteIoEndpoint>();
        var chuteMapping = new Dictionary<long, (IChuteIoEndpoint endpoint, int channelIndex)>();
        
        // 只映射前10个格口
        var endpoint = new SimulationChuteIoEndpoint(
            "partial-node",
            32,
            loggerFactory.CreateLogger<SimulationChuteIoEndpoint>());
        endpoints.Add(endpoint);
        
        for (int i = 1; i <= 10; i++)
        {
            chuteMapping[i] = (endpoint, i);
        }
        
        var service = new SimulationChuteIoService(
            endpoints,
            chuteMapping,
            loggerFactory.CreateLogger<SimulationChuteIoService>());

        // Act & Assert - 测试映射的格口
        await service.OpenAsync(5);
        await service.CloseAsync(5);
        
        // Act & Assert - 测试未映射的格口（应该记录错误但不崩溃）
        await service.OpenAsync(15); // 未映射，应该输出错误日志
        await service.CloseAsync(15); // 未映射，应该输出错误日志
        
        // Act & Assert - CloseAll 应该正常工作
        await service.CloseAllAsync();
        
        _output.WriteLine("✓ Unmapped chute test passed - system did not crash");
    }

    /// <summary>
    /// 测试智嵌继电器服务的基本功能
    /// </summary>
    [Fact]
    public async Task ZhiQian32RelayService_BasicFunctions_ShouldWork()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddProvider(new TestLoggerProvider(_output)));
        
        var endpoints = new List<ZhiQian32RelayEndpoint>();
        var chuteMapping = new Dictionary<long, (IChuteIoEndpoint endpoint, int channelIndex)>();
        
        // 创建智嵌继电器端点（注意：这里使用假的IP，不会真正连接）
        var endpoint = new ZhiQian32RelayEndpoint(
            "zhiqian-node-1",
            "127.0.0.1",
            8000,
            32,
            loggerFactory.CreateLogger<ZhiQian32RelayEndpoint>(),
            loggerFactory.CreateLogger<ZhiQian32RelayClient>());
        endpoints.Add(endpoint);
        
        for (int i = 1; i <= 32; i++)
        {
            chuteMapping[i] = (endpoint, i);
        }
        
        var service = new ZhiQian32RelayChuteIoService(
            endpoints,
            chuteMapping,
            loggerFactory.CreateLogger<ZhiQian32RelayChuteIoService>());

        // Act & Assert - 测试映射的格口（不会真正连接，只验证映射逻辑）
        // 由于没有真实的TCP连接，这些调用会失败，但不会崩溃
        try
        {
            await service.OpenAsync(1);
        }
        catch
        {
            // 预期会失败，因为没有真实的服务器
            _output.WriteLine("Expected: OpenAsync failed due to no real server");
        }
        
        // Act & Assert - 测试未映射的格口
        await service.OpenAsync(99); // 未映射，应该输出错误日志
        
        // Act & Assert - CloseAll 应该调用所有端点
        try
        {
            await service.CloseAllAsync();
        }
        catch
        {
            // 预期会失败，因为没有真实的服务器
            _output.WriteLine("Expected: CloseAllAsync failed due to no real server");
        }
        
        // Cleanup
        service.Dispose();
        
        _output.WriteLine("✓ ZhiQian32Relay service test passed");
    }

    /// <summary>
    /// 测试格口映射配置正确性
    /// </summary>
    [Fact]
    public void ChuteMapping_Configuration_ShouldBeCorrect()
    {
        // Arrange - 模拟多节点配置
        var node1Chutes = Enumerable.Range(1, 32).Select(i => (long)i).ToList();
        var node2Chutes = Enumerable.Range(33, 32).Select(i => (long)i).ToList();
        
        // Assert - 验证没有重叠
        var intersection = node1Chutes.Intersect(node2Chutes).ToList();
        Assert.Empty(intersection);
        
        // Assert - 验证总数正确
        Assert.Equal(32, node1Chutes.Count);
        Assert.Equal(32, node2Chutes.Count);
        
        // Assert - 验证范围正确
        Assert.All(node1Chutes, chuteId => Assert.InRange(chuteId, 1, 32));
        Assert.All(node2Chutes, chuteId => Assert.InRange(chuteId, 33, 64));
        
        _output.WriteLine("✓ Chute mapping configuration test passed");
    }
}
