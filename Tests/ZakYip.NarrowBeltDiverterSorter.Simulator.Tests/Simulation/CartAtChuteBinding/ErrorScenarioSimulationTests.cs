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
/// 仿真测试 - 异常场景
/// 验证各种异常情况下的错误处理机制
/// </summary>
[Trait("Category", "Simulation")]
[Trait("Category", "CartBinding")]
public class ErrorScenarioSimulationTests
{
    private const int TotalCartCount = 100;
    private const long Chute1Id = 1;

    [Fact]
    public void TotalCartCount_Zero_ThrowsExceptionWithChineseMessage()
    {
        // Arrange: TotalCartCount = 0
        var resolver = CreateResolverWithTotalCartCount(0, headCartNumber: 5);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => resolver.ResolveCurrentCartNumberForChute(Chute1Id));

        Assert.Contains("小车总数量未完成学习或配置", exception.Message);
    }

    [Fact]
    public void TotalCartCount_Negative_ThrowsException()
    {
        // Arrange: TotalCartCount < 0
        var resolver = CreateResolverWithTotalCartCount(-5, headCartNumber: 5);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => resolver.ResolveCurrentCartNumberForChute(Chute1Id));

        Assert.Contains("小车总数量未完成学习或配置", exception.Message);
    }

    [Fact]
    public void CartPositionTracker_NotInitialized_ThrowsExceptionWithChineseMessage()
    {
        // Arrange: 首车跟踪器未初始化
        var mockTracker = new Mock<ICartPositionTracker>();
        mockTracker.Setup(x => x.IsInitialized).Returns(false);
        mockTracker.Setup(x => x.CurrentOriginCartIndex).Returns((CartIndex?)null);

        var mockRingConfig = new Mock<ICartRingConfigurationProvider>();
        mockRingConfig.Setup(x => x.Current)
            .Returns(new CartRingConfiguration { TotalCartCount = TotalCartCount });

        var mockChuteConfig = new Mock<IChuteConfigProvider>();
        mockChuteConfig.Setup(x => x.GetConfig(new ChuteId(Chute1Id)))
            .Returns(new ChuteConfig
            {
                ChuteId = new ChuteId(Chute1Id),
                CartNumberWhenHeadAtOrigin = 90
            });

        var calculator = new ChuteCartNumberCalculator(NullLogger<ChuteCartNumberCalculator>.Instance);
        var resolver = new CartAtChuteResolver(
            mockTracker.Object,
            mockRingConfig.Object,
            mockChuteConfig.Object,
            calculator,
            NullLogger<CartAtChuteResolver>.Instance);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => resolver.ResolveCurrentCartNumberForChute(Chute1Id));

        Assert.Contains("当前首车状态未就绪", exception.Message);
    }

    [Fact]
    public void ChuteConfig_NotFound_ThrowsExceptionWithChineseMessage()
    {
        // Arrange: 格口配置不存在
        var mockTracker = new Mock<ICartPositionTracker>();
        mockTracker.Setup(x => x.IsInitialized).Returns(true);
        mockTracker.Setup(x => x.CurrentOriginCartIndex)
            .Returns(new CartIndex(4)); // Head = 5

        var mockRingConfig = new Mock<ICartRingConfigurationProvider>();
        mockRingConfig.Setup(x => x.Current)
            .Returns(new CartRingConfiguration { TotalCartCount = TotalCartCount });

        var mockChuteConfig = new Mock<IChuteConfigProvider>();
        mockChuteConfig.Setup(x => x.GetConfig(new ChuteId(Chute1Id)))
            .Returns((ChuteConfig?)null); // 配置不存在

        var calculator = new ChuteCartNumberCalculator(NullLogger<ChuteCartNumberCalculator>.Instance);
        var resolver = new CartAtChuteResolver(
            mockTracker.Object,
            mockRingConfig.Object,
            mockChuteConfig.Object,
            calculator,
            NullLogger<CartAtChuteResolver>.Instance);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => resolver.ResolveCurrentCartNumberForChute(Chute1Id));

        Assert.Contains("格口 1 配置不存在", exception.Message);
    }

    [Theory]
    [InlineData(0)]     // 小于最小值
    [InlineData(-1)]    // 负数
    [InlineData(101)]   // 超过 TotalCartCount
    [InlineData(150)]   // 远超 TotalCartCount
    public void CartNumberWhenHeadAtOrigin_OutOfRange_ThrowsExceptionWithChineseMessage(int invalidBaseCart)
    {
        // Arrange
        var resolver = CreateResolverWithInvalidChuteBase(invalidBaseCart);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => resolver.ResolveCurrentCartNumberForChute(Chute1Id));

        Assert.Contains("超出", exception.Message);
        Assert.Contains($"CartNumberWhenHeadAtOrigin={invalidBaseCart}", exception.Message);
        Assert.Contains("[1, 100]", exception.Message);
    }

    [Theory]
    [InlineData(101)]   // 超过 TotalCartCount
    public void HeadCartNumber_OutOfRange_ThrowsExceptionWithChineseMessage(int invalidHeadCart)
    {
        // Arrange: 首车编号超出范围
        var mockTracker = new Mock<ICartPositionTracker>();
        mockTracker.Setup(x => x.IsInitialized).Returns(true);
        mockTracker.Setup(x => x.CurrentOriginCartIndex)
            .Returns(new CartIndex(invalidHeadCart - 1));

        var mockRingConfig = new Mock<ICartRingConfigurationProvider>();
        mockRingConfig.Setup(x => x.Current)
            .Returns(new CartRingConfiguration { TotalCartCount = TotalCartCount });

        var mockChuteConfig = new Mock<IChuteConfigProvider>();
        mockChuteConfig.Setup(x => x.GetConfig(new ChuteId(Chute1Id)))
            .Returns(new ChuteConfig
            {
                ChuteId = new ChuteId(Chute1Id),
                CartNumberWhenHeadAtOrigin = 90
            });

        var calculator = new ChuteCartNumberCalculator(NullLogger<ChuteCartNumberCalculator>.Instance);
        var resolver = new CartAtChuteResolver(
            mockTracker.Object,
            mockRingConfig.Object,
            mockChuteConfig.Object,
            calculator,
            NullLogger<CartAtChuteResolver>.Instance);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => resolver.ResolveCurrentCartNumberForChute(Chute1Id));

        Assert.Contains("首车编号", exception.Message);
        Assert.Contains("超出有效范围", exception.Message);
    }

    [Fact]
    public void PackageCartBinder_WhenResolverFails_ThrowsException()
    {
        // Arrange: Resolver 会失败
        var resolver = CreateResolverWithTotalCartCount(0, headCartNumber: 5);
        var binder = new PackageCartBinder(resolver, NullLogger<PackageCartBinder>.Instance);

        // Act & Assert: 包裹创建应该失败
        var exception = Assert.Throws<InvalidOperationException>(
            () => binder.BindCartForNewPackage(packageId: 1001, chuteId: Chute1Id));

        Assert.Contains("小车总数量未完成学习或配置", exception.Message);
    }

    private ICartAtChuteResolver CreateResolverWithTotalCartCount(int totalCartCount, int headCartNumber)
    {
        var mockTracker = new Mock<ICartPositionTracker>();
        mockTracker.Setup(x => x.IsInitialized).Returns(true);
        mockTracker.Setup(x => x.CurrentOriginCartIndex)
            .Returns(new CartIndex(headCartNumber - 1));

        var mockRingConfig = new Mock<ICartRingConfigurationProvider>();
        mockRingConfig.Setup(x => x.Current)
            .Returns(new CartRingConfiguration { TotalCartCount = totalCartCount });

        var mockChuteConfig = new Mock<IChuteConfigProvider>();
        mockChuteConfig.Setup(x => x.GetConfig(new ChuteId(Chute1Id)))
            .Returns(new ChuteConfig
            {
                ChuteId = new ChuteId(Chute1Id),
                CartNumberWhenHeadAtOrigin = 90
            });

        var calculator = new ChuteCartNumberCalculator(NullLogger<ChuteCartNumberCalculator>.Instance);

        return new CartAtChuteResolver(
            mockTracker.Object,
            mockRingConfig.Object,
            mockChuteConfig.Object,
            calculator,
            NullLogger<CartAtChuteResolver>.Instance);
    }

    private ICartAtChuteResolver CreateResolverWithInvalidChuteBase(int invalidBaseCart)
    {
        var mockTracker = new Mock<ICartPositionTracker>();
        mockTracker.Setup(x => x.IsInitialized).Returns(true);
        mockTracker.Setup(x => x.CurrentOriginCartIndex)
            .Returns(new CartIndex(4)); // Head = 5

        var mockRingConfig = new Mock<ICartRingConfigurationProvider>();
        mockRingConfig.Setup(x => x.Current)
            .Returns(new CartRingConfiguration { TotalCartCount = TotalCartCount });

        var mockChuteConfig = new Mock<IChuteConfigProvider>();
        mockChuteConfig.Setup(x => x.GetConfig(new ChuteId(Chute1Id)))
            .Returns(new ChuteConfig
            {
                ChuteId = new ChuteId(Chute1Id),
                CartNumberWhenHeadAtOrigin = invalidBaseCart
            });

        var calculator = new ChuteCartNumberCalculator(NullLogger<ChuteCartNumberCalculator>.Instance);

        return new CartAtChuteResolver(
            mockTracker.Object,
            mockRingConfig.Object,
            mockChuteConfig.Object,
            calculator,
            NullLogger<CartAtChuteResolver>.Instance);
    }
}
