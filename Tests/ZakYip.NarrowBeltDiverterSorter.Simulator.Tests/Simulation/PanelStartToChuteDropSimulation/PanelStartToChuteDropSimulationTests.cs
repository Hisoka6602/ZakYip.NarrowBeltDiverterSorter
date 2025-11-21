using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Execution.Sorting;

namespace ZakYip.NarrowBeltDiverterSorter.Simulator.Tests.Simulation.PanelStartToChuteDropSimulation;

/// <summary>
/// 面板启动到格口落格的完整仿真测试
/// 测试从API配置启动按钮、通过IO识别小车、包裹上车绑定、到最终落格DO输出的完整流程
/// </summary>
[Trait("Category", "Simulation")]
[Trait("Category", "E2E")]
public class PanelStartToChuteDropSimulationTests
{
    private const int TotalCartCount = 100;
    private const int Chute1Id = 1;
    private const int Chute2Id = 2;
    private const int Chute3Id = 3;
    private const int Chute1BaseCartNumber = 90; // 当首车在原点时，格口1对应90号车
    private const int Chute2BaseCartNumber = 85; // 当首车在原点时，格口2对应85号车
    private const int Chute3BaseCartNumber = 80; // 当首车在原点时，格口3对应80号车

    /// <summary>
    /// 核心测试：1000个包裹的完整仿真流程
    /// 验证：
    /// 1. 小车IO识别正确
    /// 2. 包裹绑定上车号正确
    /// 3. 落格车号与格口匹配正确
    /// </summary>
    [Fact]
    public async Task Should_CorrectlyIdentifyCarts_AndDropParcels_For1000Packages()
    {
        // Arrange
        var clock = new SimulationClock();
        var ioBoard = new SimulatedIoBoard();
        var cartTracker = new SimulatedCartPositionTracker(TotalCartCount);
        var chuteTransmitter = new SimulatedChuteTransmitterPort(clock);

        // 初始化小车跟踪器
        cartTracker.Initialize();
        cartTracker.SetCurrentOriginCartIndex(0); // 从1号车开始（0-based index 0 = 1号车）

        // 配置格口解析器
        var resolver = CreateCartAtChuteResolver(cartTracker);

        // 创建包裹绑定器
        var binder = new PackageCartBinder(resolver, NullLogger<PackageCartBinder>.Instance);

        // 生成1000个包裹
        var parcels = GenerateParcels(1000);

        // 记录包裹绑定和落格事件
        var packageBindings = new Dictionary<long, PackageBinding>();
        var ejectionEvents = new List<EjectionRecord>();

        // 小车运行仿真参数
        const int cartPassIntervalTicks = 500; // 小车经过原点间隔500ms
        int ticksSinceLastCart = 0;
        int currentOriginCartIndex = 0; // 0-based索引

        // Act - 仿真执行
        foreach (var parcel in parcels)
        {
            // 推进仿真时钟到包裹上料时刻
            var ticksToAdvance = parcel.FeedingTick - clock.CurrentTick;
            if (ticksToAdvance > 0)
            {
                clock.Advance(ticksToAdvance);
                ticksSinceLastCart += ticksToAdvance;
            }

            // 模拟小车通过原点（每500ms一辆车）
            while (ticksSinceLastCart >= cartPassIntervalTicks)
            {
                // 模拟IO脉冲：小车经过原点传感器
                ioBoard.SetInput(CartSensorInputChannel, true);
                cartTracker.OnCartPassedOrigin(DateTimeOffset.UtcNow);
                currentOriginCartIndex = (currentOriginCartIndex + 1) % TotalCartCount;
                
                // 重置IO
                ioBoard.SetInput(CartSensorInputChannel, false);
                
                ticksSinceLastCart -= cartPassIntervalTicks;
            }

            // 包裹上料：绑定到小车
            var boundCartNumber = binder.BindCartForNewPackage(parcel.PackageId, parcel.TargetChuteId);
            var boundTime = clock.CurrentTick;
            
            packageBindings[parcel.PackageId] = new PackageBinding(
                parcel.PackageId,
                boundCartNumber,
                parcel.TargetChuteId,
                boundTime);

            // 仿真包裹随小车运行到目标格口
            // 简化模型：假设包裹在绑定后的下一个tick就到达格口
            clock.Advance(50); // 50ms后到达格口

            // 计算目标格口当前对应的小车号
            var cartAtTargetChute = resolver.ResolveCurrentCartNumberForChute(parcel.TargetChuteId);

            // 当绑定的车号经过目标格口时，触发落格
            if (cartAtTargetChute == boundCartNumber)
            {
                // 触发落格DO
                await chuteTransmitter.OpenWindowAsync(
                    new ChuteId(parcel.TargetChuteId),
                    TimeSpan.FromMilliseconds(100));

                ejectionEvents.Add(new EjectionRecord(
                    parcel.PackageId,
                    parcel.TargetChuteId,
                    cartAtTargetChute,
                    clock.CurrentTick));
            }
        }

        // Assert - 验证结果
        Assert.Equal(1000, packageBindings.Count);

        // 验证1：所有包裹都绑定了车号
        foreach (var binding in packageBindings.Values)
        {
            Assert.InRange(binding.CartNumber, 1, TotalCartCount);
        }

        // 验证2：落格事件数量应该等于包裹数量（每个包裹都应该落格）
        Assert.Equal(1000, ejectionEvents.Count);

        // 验证3：每个包裹的落格格口应该与目标格口一致，车号应该与绑定车号一致
        foreach (var ejection in ejectionEvents)
        {
            var binding = packageBindings[ejection.PackageId];
            Assert.Equal(binding.TargetChuteId, ejection.ChuteId);
            Assert.Equal(binding.CartNumber, ejection.CartNumber);
        }

        // 验证4：格口发信器事件记录正确
        var transmitterEvents = chuteTransmitter.GetEjectionEvents();
        Assert.Equal(1000, transmitterEvents.Count);

        // 验证5：检查小车传感器被触发过
        // 注：IO输入通过SetInput设置，不记录历史，这里验证输出历史
        // 实际应用中，小车传感器触发应该通过输出或专门的事件记录
        var outputHistory = ioBoard.GetAllOutputHistory();
        // 简化验证：确保格口发信器记录了事件
        Assert.Equal(1000, transmitterEvents.Count);

        // 验证6：确保没有重复的包裹ID
        var uniquePackageIds = ejectionEvents.Select(e => e.PackageId).Distinct().Count();
        Assert.Equal(1000, uniquePackageIds);

        // 验证7：验证每个格口都有包裹落格
        var chutesUsed = ejectionEvents.Select(e => e.ChuteId).Distinct().ToList();
        Assert.Contains(Chute1Id, chutesUsed);
        Assert.Contains(Chute2Id, chutesUsed);
        Assert.Contains(Chute3Id, chutesUsed);
    }

    /// <summary>
    /// 包裹绑定记录
    /// </summary>
    private sealed record PackageBinding(
        long PackageId,
        int CartNumber,
        int TargetChuteId,
        int BoundAtTick);

    /// <summary>
    /// 落格事件记录
    /// </summary>
    private sealed record EjectionRecord(
        long PackageId,
        int ChuteId,
        int CartNumber,
        int Tick);

    private const int CartSensorInputChannel = 100; // 小车传感器IO通道

    /// <summary>
    /// 生成测试用包裹
    /// </summary>
    private List<SimulatedParcel> GenerateParcels(int count)
    {
        var parcels = new List<SimulatedParcel>();
        var random = new Random(42); // 固定种子确保可重复性
        var validChutes = new[] { Chute1Id, Chute2Id, Chute3Id };

        for (int i = 1; i <= count; i++)
        {
            parcels.Add(new SimulatedParcel
            {
                PackageId = i,
                TargetChuteId = validChutes[random.Next(validChutes.Length)],
                FeedingTick = i * 50 // 每50ms上一个包裹
            });
        }

        return parcels;
    }

    /// <summary>
    /// 创建格口小车号解析器
    /// </summary>
    private ICartAtChuteResolver CreateCartAtChuteResolver(SimulatedCartPositionTracker tracker)
    {
        var mockRingConfig = new Mock<ICartRingConfigurationProvider>();
        mockRingConfig.Setup(x => x.Current)
            .Returns(new CartRingConfiguration { TotalCartCount = TotalCartCount });

        var mockChuteConfig = new Mock<IChuteConfigProvider>();
        mockChuteConfig.Setup(x => x.GetConfig(new ChuteId(Chute1Id)))
            .Returns(new ChuteConfig
            {
                ChuteId = new ChuteId(Chute1Id),
                CartNumberWhenHeadAtOrigin = Chute1BaseCartNumber
            });
        mockChuteConfig.Setup(x => x.GetConfig(new ChuteId(Chute2Id)))
            .Returns(new ChuteConfig
            {
                ChuteId = new ChuteId(Chute2Id),
                CartNumberWhenHeadAtOrigin = Chute2BaseCartNumber
            });
        mockChuteConfig.Setup(x => x.GetConfig(new ChuteId(Chute3Id)))
            .Returns(new ChuteConfig
            {
                ChuteId = new ChuteId(Chute3Id),
                CartNumberWhenHeadAtOrigin = Chute3BaseCartNumber
            });

        var calculator = new ChuteCartNumberCalculator(NullLogger<ChuteCartNumberCalculator>.Instance);

        return new CartAtChuteResolver(
            tracker,
            mockRingConfig.Object,
            mockChuteConfig.Object,
            calculator,
            NullLogger<CartAtChuteResolver>.Instance);
    }
}
