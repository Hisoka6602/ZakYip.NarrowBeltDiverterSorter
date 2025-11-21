using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Communication.Upstream;
using ZakYip.NarrowBeltDiverterSorter.Execution.Upstream;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;
using ZakYip.NarrowBeltDiverterSorter.E2ETests.Fakes;

namespace ZakYip.NarrowBeltDiverterSorter.E2ETests;

/// <summary>
/// 上游规则引擎集成 E2E 测试
/// 测试与上游规则引擎通信的完整流程
/// </summary>
public class UpstreamIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public UpstreamIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// 测试成功场景：Fake 客户端正常返回格口号
    /// </summary>
    [Fact]
    public async Task SuccessScenario_FakeClientReturnsChute_RequestSucceeds()
    {
        // Arrange - 设置测试环境
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        var logger = loggerFactory.CreateLogger<UpstreamIntegrationTests>();
        var fakeClientLogger = loggerFactory.CreateLogger<FakeSortingRuleEngineClient>();
        var adapterLogger = loggerFactory.CreateLogger<SortingRuleEnginePortAdapter>();

        // 创建 Fake 客户端（配置为成功场景，返回格口编号 = 5）
        var fakeOptions = FakeSortingRuleEngineClientOptions.CreateSuccessScenario(chuteNumber: 5);
        var fakeClient = new FakeSortingRuleEngineClient(fakeOptions, fakeClientLogger);

        // 创建端口适配器
        var portAdapter = new SortingRuleEnginePortAdapter(fakeClient, adapterLogger);

        // 跟踪事件
        var receivedChuteNumber = 0;
        fakeClient.SortingResultReceived += (sender, result) =>
        {
            logger.LogInformation("收到分拣结果: ParcelId={ParcelId}, ChuteNumber={ChuteNumber}", result.ParcelId, result.ChuteNumber);
            receivedChuteNumber = result.ChuteNumber;
        };

        // Act - 发送分拣请求
        var parcelId = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var sortingRequest = new SortingRequestEventArgs
        {
            ParcelId = parcelId,
            CartNumber = 1,
            Barcode = "TEST-SUCCESS-001",
            Weight = 1.5m,
            Length = 300m,
            Width = 200m,
            Height = 150m,
            RequestTime = DateTimeOffset.Now
        };

        logger.LogInformation("发送分拣请求: ParcelId={ParcelId}, Barcode={Barcode}", parcelId, sortingRequest.Barcode);
        
        await portAdapter.RequestSortingAsync(sortingRequest, CancellationToken.None);

        // 等待异步事件处理
        await Task.Delay(200);

        // Assert - 验证结果
        _output.WriteLine($"收到的格口编号: {receivedChuteNumber}");
        Assert.Equal(5, receivedChuteNumber); // 应该返回我们配置的格口号

        logger.LogInformation("✓ 成功场景测试通过：包裹 {ParcelId} 被路由到格口 {ChuteNumber}", parcelId, receivedChuteNumber);
    }

    /// <summary>
    /// 测试失败场景：Fake 客户端抛出异常，系统不崩溃
    /// </summary>
    [Fact]
    public async Task FailureScenario_FakeClientThrowsException_ExceptionPropagates()
    {
        // Arrange - 设置测试环境
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        var logger = loggerFactory.CreateLogger<UpstreamIntegrationTests>();
        var fakeClientLogger = loggerFactory.CreateLogger<FakeSortingRuleEngineClient>();
        var adapterLogger = loggerFactory.CreateLogger<SortingRuleEnginePortAdapter>();

        // 创建 Fake 客户端（配置为失败场景）
        var fakeOptions = FakeSortingRuleEngineClientOptions.CreateFailureScenario();
        var fakeClient = new FakeSortingRuleEngineClient(fakeOptions, fakeClientLogger);

        // 创建端口适配器
        var portAdapter = new SortingRuleEnginePortAdapter(fakeClient, adapterLogger);

        // Act - 发送分拣请求（应该抛出异常）
        var parcelId = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var sortingRequest = new SortingRequestEventArgs
        {
            ParcelId = parcelId,
            CartNumber = 1,
            Barcode = "TEST-FAILURE-001",
            Weight = 1.5m,
            RequestTime = DateTimeOffset.Now
        };

        logger.LogInformation("发送分拣请求（预期失败）: ParcelId={ParcelId}", parcelId);

        // Assert - 验证异常被抛出
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await portAdapter.RequestSortingAsync(sortingRequest, CancellationToken.None);
        });

        logger.LogInformation("✓ 失败场景测试通过：上游异常被正确捕获");
    }

    /// <summary>
    /// 测试取消场景：主动取消请求
    /// </summary>
    [Fact]
    public async Task CancellationScenario_RequestCancelled_OperationCancelledExceptionThrown()
    {
        // Arrange - 设置测试环境
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        var logger = loggerFactory.CreateLogger<UpstreamIntegrationTests>();
        var fakeClientLogger = loggerFactory.CreateLogger<FakeSortingRuleEngineClient>();
        var adapterLogger = loggerFactory.CreateLogger<SortingRuleEnginePortAdapter>();

        // 创建 Fake 客户端（配置为超时场景）
        var fakeOptions = new FakeSortingRuleEngineClientOptions
        {
            IsConnected = true,
            SimulateTimeout = true,
            TimeoutDelayMs = 5000, // 5秒延迟
            SimulateFailure = false
        };
        var fakeClient = new FakeSortingRuleEngineClient(fakeOptions, fakeClientLogger);

        // 创建端口适配器
        var portAdapter = new SortingRuleEnginePortAdapter(fakeClient, adapterLogger);

        // Act - 发送分拣请求并快速取消
        var parcelId = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var sortingRequest = new SortingRequestEventArgs
        {
            ParcelId = parcelId,
            CartNumber = 1,
            Barcode = "TEST-CANCEL-001",
            Weight = 1.5m,
            RequestTime = DateTimeOffset.Now
        };

        logger.LogInformation("发送分拣请求并准备取消: ParcelId={ParcelId}", parcelId);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)); // 100ms 后取消

        // Assert - 验证 OperationCanceledException 被抛出
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await portAdapter.RequestSortingAsync(sortingRequest, cts.Token);
        });

        logger.LogInformation("✓ 取消场景测试通过：请求被正确取消");
    }
}
