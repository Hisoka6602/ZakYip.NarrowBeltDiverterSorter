using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Application;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;
using ZakYip.NarrowBeltDiverterSorter.Execution.Feeding;

namespace ZakYip.NarrowBeltDiverterSorter.E2ETests;

/// <summary>
/// 端到端仿真测试
/// 测试从小车位置跟踪到包裹装载预测的完整流程
/// </summary>
public class EndToEndSimulationTests
{
    /// <summary>
    /// 测试完整的小车跟踪和包裹装载预测流程
    /// </summary>
    [Fact]
    public async Task E2E_CartTracking_And_ParcelLoadPrediction_Should_Work_Together()
    {
        // Arrange - 设置测试环境
        var cartRingBuilder = new CartRingBuilder();
        var cartPositionTracker = new CartPositionTracker(cartRingBuilder);
        var mainLineFeedback = new MockMainLineFeedbackPort(1000.0); // 1000 mm/s
        var infeedConveyor = new MockInfeedConveyorPort(1000.0); // 1000 mm/s
        
        var infeedLayoutOptions = new InfeedLayoutOptions
        {
            InfeedToMainLineDistanceMm = 2000m, // 2000mm 距离
            TimeToleranceMs = 100,
            CartOffsetCalibration = 0 // 入口落车点在原点位置（偏移0）
        };

        var cartLifecycleService = new CartLifecycleService();

        var parcelLoadPlanner = new ParcelLoadPlanner(
            cartRingBuilder,
            cartPositionTracker,
            mainLineFeedback,
            infeedConveyor,
            cartLifecycleService,
            infeedLayoutOptions);

        var loadCoordinator = new ParcelLoadCoordinator(parcelLoadPlanner);

        // 记录装载事件
        var loadedEvents = new List<ParcelLoadedOnCartEventArgs>();
        loadCoordinator.ParcelLoadedOnCart += (sender, e) => loadedEvents.Add(e);

        // 步骤 1: 构建小车环（10个小车）
        BuildCartRing(cartRingBuilder, 10);

        var snapshot = cartRingBuilder.CurrentSnapshot;
        Assert.NotNull(snapshot);
        Assert.Equal(10, snapshot.RingLength.Value);

        // 初始化小车到 CartLifecycleService
        for (int i = 0; i < snapshot.RingLength.Value; i++)
        {
            var cartId = snapshot.CartIds[i];
            cartLifecycleService.InitializeCart(cartId, new CartIndex(i), DateTimeOffset.UtcNow);
        }

        // 步骤 2: 模拟小车经过原点，初始化位置跟踪器
        // 假设开始时小车0在原点
        cartPositionTracker.OnCartPassedOrigin(DateTimeOffset.UtcNow);
        
        Assert.NotNull(cartPositionTracker.CurrentOriginCartIndex);
        Assert.Equal(0, cartPositionTracker.CurrentOriginCartIndex.Value.Value);

        // 步骤 3: 模拟包裹到达入口传感器，测试装载预测
        var parcelId1 = new ParcelId(1);
        var infeedTime1 = DateTimeOffset.UtcNow;

        var parcelEvent1 = new ParcelCreatedFromInfeedEventArgs
        {
            ParcelId = parcelId1,
            Barcode = "PKG001",
            InfeedTriggerTime = infeedTime1
        };

        loadCoordinator.HandleParcelCreatedFromInfeed(null, parcelEvent1);
        await Task.Delay(150); // 等待异步处理完成

        // 验证包裹被装载到正确的小车（偏移0，当前在原点的小车0）
        Assert.Single(loadedEvents);
        Assert.Equal(parcelId1, loadedEvents[0].ParcelId);
        Assert.Equal(0L, loadedEvents[0].CartId.Value); // 应该装载到小车0

        // 步骤 4: 模拟更多小车经过原点
        cartPositionTracker.OnCartPassedOrigin(DateTimeOffset.UtcNow); // 小车1现在在原点
        Assert.Equal(1, cartPositionTracker.CurrentOriginCartIndex!.Value.Value);

        cartPositionTracker.OnCartPassedOrigin(DateTimeOffset.UtcNow); // 小车2现在在原点
        Assert.Equal(2, cartPositionTracker.CurrentOriginCartIndex!.Value.Value);

        // 步骤 5: 测试第二个包裹的装载预测
        var parcelId2 = new ParcelId(2);
        var infeedTime2 = DateTimeOffset.UtcNow;

        var parcelEvent2 = new ParcelCreatedFromInfeedEventArgs
        {
            ParcelId = parcelId2,
            Barcode = "PKG002",
            InfeedTriggerTime = infeedTime2
        };

        loadCoordinator.HandleParcelCreatedFromInfeed(null, parcelEvent2);
        await Task.Delay(150);

        // 验证第二个包裹被装载到小车2（当前在原点）
        Assert.Equal(2, loadedEvents.Count);
        Assert.Equal(parcelId2, loadedEvents[1].ParcelId);
        Assert.Equal(2L, loadedEvents[1].CartId.Value); // 应该装载到小车2

        // 步骤 6: 测试环绕（wrap-around）
        // 继续让小车经过原点直到接近环的末尾
        for (int i = 3; i < 10; i++)
        {
            cartPositionTracker.OnCartPassedOrigin(DateTimeOffset.UtcNow);
        }
        Assert.Equal(9, cartPositionTracker.CurrentOriginCartIndex!.Value.Value);

        // 再让一个小车经过，应该回到0
        cartPositionTracker.OnCartPassedOrigin(DateTimeOffset.UtcNow);
        Assert.Equal(0, cartPositionTracker.CurrentOriginCartIndex!.Value.Value); // 回绕到0

        // 测试第三个包裹
        var parcelId3 = new ParcelId(3);
        var parcelEvent3 = new ParcelCreatedFromInfeedEventArgs
        {
            ParcelId = parcelId3,
            Barcode = "PKG003",
            InfeedTriggerTime = DateTimeOffset.UtcNow
        };

        loadCoordinator.HandleParcelCreatedFromInfeed(null, parcelEvent3);
        await Task.Delay(150);

        // 验证环绕后仍能正确预测（小车0）
        Assert.Equal(3, loadedEvents.Count);
        Assert.Equal(parcelId3, loadedEvents[2].ParcelId);
        Assert.Equal(0L, loadedEvents[2].CartId.Value); // 应该回到小车0
    }

    /// <summary>
    /// 测试带偏移的装载预测
    /// </summary>
    [Fact]
    public async Task E2E_ParcelLoadPrediction_With_Offset_Should_Work()
    {
        // Arrange
        var cartRingBuilder = new CartRingBuilder();
        var cartPositionTracker = new CartPositionTracker(cartRingBuilder);
        var mainLineFeedback = new MockMainLineFeedbackPort(1000.0);
        var infeedConveyor = new MockInfeedConveyorPort(1000.0);
        
        var infeedLayoutOptions = new InfeedLayoutOptions
        {
            InfeedToMainLineDistanceMm = 2000m,
            TimeToleranceMs = 100,
            CartOffsetCalibration = 3 // 入口落车点在原点偏移3个小车的位置
        };

        var cartLifecycleService = new CartLifecycleService();

        var parcelLoadPlanner = new ParcelLoadPlanner(
            cartRingBuilder,
            cartPositionTracker,
            mainLineFeedback,
            infeedConveyor,
            cartLifecycleService,
            infeedLayoutOptions);

        var loadCoordinator = new ParcelLoadCoordinator(parcelLoadPlanner);

        var loadedEvents = new List<ParcelLoadedOnCartEventArgs>();
        loadCoordinator.ParcelLoadedOnCart += (sender, e) => loadedEvents.Add(e);

        // 构建小车环
        BuildCartRing(cartRingBuilder, 10);

        var snapshot = cartRingBuilder.CurrentSnapshot;
        Assert.NotNull(snapshot);

        // 初始化小车到 CartLifecycleService
        for (int i = 0; i < snapshot.RingLength.Value; i++)
        {
            var cartId = snapshot.CartIds[i];
            cartLifecycleService.InitializeCart(cartId, new CartIndex(i), DateTimeOffset.UtcNow);
        }

        // 初始化位置跟踪器（小车0在原点）
        cartPositionTracker.OnCartPassedOrigin(DateTimeOffset.UtcNow);

        // Act - 创建包裹
        var parcelId = new ParcelId(100);
        var parcelEvent = new ParcelCreatedFromInfeedEventArgs
        {
            ParcelId = parcelId,
            Barcode = "PKG100",
            InfeedTriggerTime = DateTimeOffset.UtcNow
        };

        loadCoordinator.HandleParcelCreatedFromInfeed(null, parcelEvent);
        await Task.Delay(150);

        // Assert - 验证装载到偏移3的小车
        Assert.Single(loadedEvents);
        Assert.Equal(3L, loadedEvents[0].CartId.Value); // 偏移3，所以是小车3
    }

    /// <summary>
    /// 测试无小车环时的行为
    /// </summary>
    [Fact]
    public async Task E2E_ParcelLoadPrediction_Without_CartRing_Should_Fail_Gracefully()
    {
        // Arrange
        var cartRingBuilder = new CartRingBuilder();
        var cartPositionTracker = new CartPositionTracker(cartRingBuilder);
        var mainLineFeedback = new MockMainLineFeedbackPort(1000.0);
        var infeedConveyor = new MockInfeedConveyorPort(1000.0);
        
        var infeedLayoutOptions = new InfeedLayoutOptions
        {
            InfeedToMainLineDistanceMm = 2000m,
            TimeToleranceMs = 100,
            CartOffsetCalibration = 0
        };

        var cartLifecycleService = new CartLifecycleService();

        var parcelLoadPlanner = new ParcelLoadPlanner(
            cartRingBuilder,
            cartPositionTracker,
            mainLineFeedback,
            infeedConveyor,
            cartLifecycleService,
            infeedLayoutOptions);

        var loadCoordinator = new ParcelLoadCoordinator(parcelLoadPlanner);

        // Act - 在没有构建小车环的情况下尝试创建包裹
        var parcelId = new ParcelId(200);
        var parcelEvent = new ParcelCreatedFromInfeedEventArgs
        {
            ParcelId = parcelId,
            Barcode = "PKG200",
            InfeedTriggerTime = DateTimeOffset.UtcNow
        };

        loadCoordinator.HandleParcelCreatedFromInfeed(null, parcelEvent);
        await Task.Delay(150);

        // Assert - 验证包裹保持在等待状态（而不是失败状态）
        var snapshots = loadCoordinator.GetParcelSnapshots();
        Assert.Single(snapshots);
        Assert.Equal(ParcelRouteState.WaitingForRouting, snapshots[parcelId].RouteState);
    }

    /// <summary>
    /// 辅助方法：构建小车环
    /// </summary>
    private void BuildCartRing(ICartRingBuilder builder, int cartCount)
    {
        var timestamp = DateTimeOffset.UtcNow;

        // 第一次：零点小车通过
        builder.OnOriginSensorTriggered(true, true, timestamp);
        builder.OnOriginSensorTriggered(false, true, timestamp.AddMilliseconds(10));
        builder.OnOriginSensorTriggered(true, false, timestamp.AddMilliseconds(50));
        builder.OnOriginSensorTriggered(false, false, timestamp.AddMilliseconds(60));

        // 其他小车通过
        for (int i = 1; i < cartCount; i++)
        {
            timestamp = timestamp.AddMilliseconds(100);
            builder.OnOriginSensorTriggered(true, true, timestamp);
            builder.OnOriginSensorTriggered(true, false, timestamp.AddMilliseconds(50));
        }

        // 第二次零点小车通过，完成环
        timestamp = timestamp.AddMilliseconds(100);
        builder.OnOriginSensorTriggered(true, true, timestamp);
        builder.OnOriginSensorTriggered(false, true, timestamp.AddMilliseconds(10));
        builder.OnOriginSensorTriggered(true, false, timestamp.AddMilliseconds(50));
        builder.OnOriginSensorTriggered(false, false, timestamp.AddMilliseconds(60));
    }

    /// <summary>
    /// Mock 主线反馈端口
    /// </summary>
    private class MockMainLineFeedbackPort : IMainLineFeedbackPort
    {
        private readonly double _speed;

        public MockMainLineFeedbackPort(double speed)
        {
            _speed = speed;
        }

        public double GetCurrentSpeed() => _speed;
        public MainLineStatus GetCurrentStatus() => MainLineStatus.Running;
        public int? GetFaultCode() => null;
    }

    /// <summary>
    /// Mock 入口输送线端口
    /// </summary>
    private class MockInfeedConveyorPort : IInfeedConveyorPort
    {
        private double _speed;

        public MockInfeedConveyorPort(double speed)
        {
            _speed = speed;
        }

        public double GetCurrentSpeed() => _speed;

        public Task<bool> SetSpeedAsync(double speedMmPerSec, CancellationToken cancellationToken = default)
        {
            _speed = speedMmPerSec;
            return Task.FromResult(true);
        }

        public Task<bool> StartAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> StopAsync(CancellationToken cancellationToken = default)
        {
            _speed = 0;
            return Task.FromResult(true);
        }
    }
}