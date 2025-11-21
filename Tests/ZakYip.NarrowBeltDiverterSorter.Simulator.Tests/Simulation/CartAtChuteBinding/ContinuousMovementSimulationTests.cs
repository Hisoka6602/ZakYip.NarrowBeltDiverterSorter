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
/// 仿真测试 - 连续移动场景
/// 验证首车连续移动时，格口小车号计算和包裹绑定的正确性
/// </summary>
[Trait("Category", "Simulation")]
[Trait("Category", "CartBinding")]
public class ContinuousMovementSimulationTests
{
    private const int TotalCartCount = 100;
    private const long Chute1Id = 1;
    private const long Chute3Id = 3;
    private const int Chute1BaseCartNumber = 90;
    private const int Chute3BaseCartNumber = 80;

    [Fact]
    public void ContinuousMovement_FullCycle_AllCartNumbersValid()
    {
        // Arrange & Act: 仿真首车从1到100的完整循环
        var results = new List<(int HeadCart, int Chute1Cart)>();

        for (int headCart = 1; headCart <= TotalCartCount; headCart++)
        {
            var resolver = CreateResolver(headCart);
            var chute1Cart = resolver.ResolveCurrentCartNumberForChute(Chute1Id);
            results.Add((headCart, chute1Cart));
        }

        // Assert: 所有结果在有效范围内
        foreach (var (headCart, chute1Cart) in results)
        {
            Assert.InRange(chute1Cart, 1, TotalCartCount);
        }

        // Assert: 验证完整循环
        Assert.Equal(TotalCartCount, results.Count);
    }

    [Fact]
    public void ContinuousMovement_WithPackageCreation_NoOffByOneError()
    {
        // Arrange: 每5步在格口1触发一次包裹创建
        var packageBindings = new List<(int PackageId, int HeadCart, int BoundCart)>();
        int packageId = 1000;

        // Act: 仿真首车移动并创建包裹
        for (int headCart = 1; headCart <= TotalCartCount; headCart++)
        {
            var resolver = CreateResolver(headCart);
            var binder = new PackageCartBinder(resolver, NullLogger<PackageCartBinder>.Instance);

            // 每5步创建一个包裹
            if (headCart % 5 == 0)
            {
                var boundCart = binder.BindCartForNewPackage(packageId, Chute1Id);
                var expectedCart = resolver.ResolveCurrentCartNumberForChute(Chute1Id);
                packageBindings.Add((packageId, headCart, boundCart));

                // Assert: 验证每次绑定与 Resolver 一致
                Assert.Equal(expectedCart, boundCart);
                packageId++;
            }
        }

        // Assert: 所有绑定的小车号都有效
        Assert.All(packageBindings, binding =>
        {
            Assert.InRange(binding.BoundCart, 1, TotalCartCount);
        });

        // Assert: 验证创建了正确数量的包裹
        Assert.Equal(TotalCartCount / 5, packageBindings.Count);
    }

    [Fact]
    public void ContinuousMovement_WrapAround_HandledCorrectly()
    {
        // Arrange: 关注环绕点附近的行为
        var criticalPoints = new[] { 10, 11, 12, 13, 98, 99, 100, 1, 2 };
        var results = new Dictionary<int, int>();

        // Act: 测试关键点的小车号计算
        foreach (var headCart in criticalPoints)
        {
            var resolver = CreateResolver(headCart);
            var cartNumber = resolver.ResolveCurrentCartNumberForChute(Chute1Id);
            results[headCart] = cartNumber;
        }

        // Assert: 验证环绕逻辑
        Assert.Equal(100, results[11]); // 首车=11 -> 格口1=100
        Assert.Equal(1, results[12]);    // 首车=12 -> 格口1=1 (环绕)
        Assert.Equal(2, results[13]);    // 首车=13 -> 格口1=2

        // 所有结果都在有效范围内
        Assert.All(results.Values, cartNumber => Assert.InRange(cartNumber, 1, TotalCartCount));
    }

    [Fact]
    public void ContinuousMovement_MultipleChutes_IndependentCalculation()
    {
        // Arrange & Act: 验证多个格口的计算互不干扰
        var chute1Results = new List<int>();
        var chute3Results = new List<int>();

        for (int headCart = 1; headCart <= 20; headCart++)
        {
            var resolver = CreateResolver(headCart);
            chute1Results.Add(resolver.ResolveCurrentCartNumberForChute(Chute1Id));
            chute3Results.Add(resolver.ResolveCurrentCartNumberForChute(Chute3Id));
        }

        // Assert: 两个格口的结果序列不同
        Assert.NotEqual(chute1Results, chute3Results);

        // Assert: 格口1和格口3的偏移量保持恒定（基准差=10）
        for (int i = 0; i < chute1Results.Count; i++)
        {
            var expectedDiff = Chute1BaseCartNumber - Chute3BaseCartNumber; // 90 - 80 = 10
            var actualDiff = (chute1Results[i] - chute3Results[i] + TotalCartCount) % TotalCartCount;
            if (actualDiff == 0) actualDiff = TotalCartCount;
            Assert.Equal(expectedDiff, actualDiff);
        }
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
