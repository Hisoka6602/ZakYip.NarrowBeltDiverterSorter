using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;
using ZakYip.NarrowBeltDiverterSorter.Execution.Sorting;

namespace ZakYip.NarrowBeltDiverterSorter.Simulator.Tests.Simulation.CartAtChuteBinding;

/// <summary>
/// 仿真测试 - 热更新场景
/// 验证运行中修改格口配置时，新旧配置的切换是否正常
/// </summary>
[Trait("Category", "Simulation")]
[Trait("Category", "CartBinding")]
public class HotUpdateSimulationTests
{
    private const int TotalCartCount = 100;
    private const long Chute1Id = 1;

    [Fact]
    public void HotUpdate_ChuteBaseCart_NewBindingsUseNewConfig()
    {
        // Arrange
        var headCartNumber = 5;
        var oldBaseCartNumber = 90;
        var newBaseCartNumber = 85;

        var oldConfig = new ChuteConfig
        {
            ChuteId = new ChuteId(Chute1Id),
            CartNumberWhenHeadAtOrigin = oldBaseCartNumber
        };

        var newConfig = new ChuteConfig
        {
            ChuteId = new ChuteId(Chute1Id),
            CartNumberWhenHeadAtOrigin = newBaseCartNumber
        };

        var mockChuteConfig = new Mock<IChuteConfigProvider>();
        
        // 初始返回旧配置
        mockChuteConfig.Setup(x => x.GetConfig(new ChuteId(Chute1Id))).Returns(oldConfig);

        var resolver = CreateResolverWithMockChuteConfig(headCartNumber, mockChuteConfig.Object);
        var binder = new PackageCartBinder(resolver, NullLogger<PackageCartBinder>.Instance);

        // Act & Assert Phase 1: 使用旧配置
        var oldBoundCart = binder.BindCartForNewPackage(packageId: 1001, chuteId: Chute1Id);
        Assert.Equal(94, oldBoundCart); // 90 + 5 - 1 = 94

        // Act: 热更新配置（修改 Mock 返回新配置）
        mockChuteConfig.Setup(x => x.GetConfig(new ChuteId(Chute1Id))).Returns(newConfig);

        // Act & Assert Phase 2: 使用新配置
        var newBoundCart = binder.BindCartForNewPackage(packageId: 1002, chuteId: Chute1Id);
        Assert.Equal(89, newBoundCart); // 85 + 5 - 1 = 89

        // Assert: 新旧配置结果不同
        Assert.NotEqual(oldBoundCart, newBoundCart);
    }

    [Fact]
    public void HotUpdate_MultiplePackages_BeforeAndAfter_NoMixing()
    {
        // Arrange
        var headCartNumber = 10;
        var oldBaseCartNumber = 90;
        var newBaseCartNumber = 80;

        var oldConfig = new ChuteConfig
        {
            ChuteId = new ChuteId(Chute1Id),
            CartNumberWhenHeadAtOrigin = oldBaseCartNumber
        };

        var newConfig = new ChuteConfig
        {
            ChuteId = new ChuteId(Chute1Id),
            CartNumberWhenHeadAtOrigin = newBaseCartNumber
        };

        var mockChuteConfig = new Mock<IChuteConfigProvider>();
        
        // 初始返回旧配置
        mockChuteConfig.Setup(x => x.GetConfig(new ChuteId(Chute1Id))).Returns(oldConfig);

        var resolver = CreateResolverWithMockChuteConfig(headCartNumber, mockChuteConfig.Object);
        var binder = new PackageCartBinder(resolver, NullLogger<PackageCartBinder>.Instance);

        // Act Phase 1: 使用旧配置绑定5个包裹
        var oldPhaseBindings = new List<int>();
        for (int i = 1001; i <= 1005; i++)
        {
            var boundCart = binder.BindCartForNewPackage(i, Chute1Id);
            oldPhaseBindings.Add(boundCart);
        }

        // Act: 热更新配置（修改 Mock 返回新配置）
        mockChuteConfig.Setup(x => x.GetConfig(new ChuteId(Chute1Id))).Returns(newConfig);

        // Act Phase 2: 使用新配置绑定5个包裹
        var newPhaseBindings = new List<int>();
        for (int i = 2001; i <= 2005; i++)
        {
            var boundCart = binder.BindCartForNewPackage(i, Chute1Id);
            newPhaseBindings.Add(boundCart);
        }

        // Assert: 前半段全部使用旧配置 (99 = 90 + 10 - 1)
        Assert.All(oldPhaseBindings, cart => Assert.Equal(99, cart));

        // Assert: 后半段全部使用新配置 (89 = 80 + 10 - 1)
        Assert.All(newPhaseBindings, cart => Assert.Equal(89, cart));
    }

    private ICartAtChuteResolver CreateResolverWithMockChuteConfig(
        int headCartNumber, IChuteConfigProvider chuteConfigProvider)
    {
        var mockTracker = new Mock<ICartPositionTracker>();
        mockTracker.Setup(x => x.IsInitialized).Returns(true);
        mockTracker.Setup(x => x.CurrentOriginCartIndex)
            .Returns(new CartIndex(headCartNumber - 1));

        var mockRingConfig = new Mock<ICartRingConfigurationProvider>();
        mockRingConfig.Setup(x => x.Current)
            .Returns(new CartRingConfiguration { TotalCartCount = TotalCartCount });

        var calculator = new ChuteCartNumberCalculator(NullLogger<ChuteCartNumberCalculator>.Instance);

        return new CartAtChuteResolver(
            mockTracker.Object,
            mockRingConfig.Object,
            chuteConfigProvider,
            calculator,
            NullLogger<CartAtChuteResolver>.Instance);
    }
}
