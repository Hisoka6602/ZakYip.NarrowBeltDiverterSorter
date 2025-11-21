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
/// 仿真测试 - 基础正确性场景
/// 配置：TotalCartCount = 100, Chute1.Base = 90, Chute3.Base = 80
/// </summary>
[Trait("Category", "Simulation")]
[Trait("Category", "CartBinding")]
public class BasicCorrectnessSimulationTests
{
    private const int TotalCartCount = 100;
    private const long Chute1Id = 1;
    private const long Chute3Id = 3;
    private const int Chute1BaseCartNumber = 90;
    private const int Chute3BaseCartNumber = 80;

    [Theory]
    [InlineData(1, Chute1Id, 90)]  // 首车=1, 格口1 -> 90
    [InlineData(1, Chute3Id, 80)]  // 首车=1, 格口3 -> 80
    [InlineData(5, Chute1Id, 94)]  // 首车=5, 格口1 -> 94
    [InlineData(5, Chute3Id, 84)]  // 首车=5, 格口3 -> 84
    [InlineData(98, Chute1Id, 87)] // 首车=98, 格口1 -> 87
    [InlineData(99, Chute1Id, 88)] // 首车=99, 格口1 -> 88
    [InlineData(100, Chute1Id, 89)] // 首车=100, 格口1 -> 89
    [InlineData(12, Chute1Id, 1)]   // 首车=12, 格口1 -> 1 (环绕)
    public void ResolveCartNumber_VariousHeadPositions_ReturnsCorrectCartNumber(
        int headCartNumber, long chuteId, int expectedCartNumber)
    {
        // Arrange
        var resolver = CreateResolver(headCartNumber);

        // Act
        var actualCartNumber = resolver.ResolveCurrentCartNumberForChute(chuteId);

        // Assert
        Assert.Equal(expectedCartNumber, actualCartNumber);
        Assert.InRange(actualCartNumber, 1, TotalCartCount);
    }

    [Fact]
    public void PackageCartBinder_BindsConsistentlyWithResolver()
    {
        // Arrange
        var headCartNumber = 5;
        var resolver = CreateResolver(headCartNumber);
        var binder = new PackageCartBinder(resolver, NullLogger<PackageCartBinder>.Instance);

        // Act
        var boundCart = binder.BindCartForNewPackage(packageId: 1001, chuteId: Chute1Id);
        var resolvedCart = resolver.ResolveCurrentCartNumberForChute(Chute1Id);

        // Assert
        Assert.Equal(resolvedCart, boundCart);
        Assert.Equal(94, boundCart);
    }

    private ICartAtChuteResolver CreateResolver(int headCartNumber)
    {
        var mockTracker = new Mock<ICartPositionTracker>();
        mockTracker.Setup(x => x.IsInitialized).Returns(true);
        mockTracker.Setup(x => x.CurrentOriginCartIndex)
            .Returns(new CartIndex(headCartNumber - 1));

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
        mockChuteConfig.Setup(x => x.GetConfig(new ChuteId(Chute3Id)))
            .Returns(new ChuteConfig
            {
                ChuteId = new ChuteId(Chute3Id),
                CartNumberWhenHeadAtOrigin = Chute3BaseCartNumber
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
