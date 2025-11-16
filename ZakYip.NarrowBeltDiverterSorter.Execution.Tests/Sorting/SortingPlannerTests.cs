using Moq;
using ZakYip.NarrowBeltDiverterSorter.Core.Application;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Carts;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;
using ZakYip.NarrowBeltDiverterSorter.Execution.Sorting;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Tests.Sorting;

/// <summary>
/// 分拣规划器测试
/// </summary>
public class SortingPlannerTests
{
    [Fact]
    public void PlanEjects_WithNoCartRing_ShouldReturnEmptyList()
    {
        // Arrange
        var mockCartRingBuilder = new Mock<ICartRingBuilder>();
        mockCartRingBuilder.Setup(x => x.CurrentSnapshot).Returns((CartRingSnapshot?)null);

        var planner = CreatePlanner(cartRingBuilder: mockCartRingBuilder.Object);

        // Act
        var plans = planner.PlanEjects(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

        // Assert
        Assert.Empty(plans);
    }

    [Fact]
    public void PlanEjects_WithUnstableSpeed_ShouldReturnEmptyList()
    {
        // Arrange
        var mockCartRingBuilder = new Mock<ICartRingBuilder>();
        var cartRing = CreateTestCartRing(10);
        mockCartRingBuilder.Setup(x => x.CurrentSnapshot).Returns(cartRing);

        var mockSpeedProvider = new Mock<IMainLineSpeedProvider>();
        mockSpeedProvider.Setup(x => x.IsSpeedStable).Returns(false);

        var planner = CreatePlanner(
            cartRingBuilder: mockCartRingBuilder.Object,
            mainLineSpeedProvider: mockSpeedProvider.Object);

        // Act
        var plans = planner.PlanEjects(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

        // Assert
        Assert.Empty(plans);
    }

    [Fact]
    public void PlanEjects_WithNormalEject_ShouldGeneratePlan()
    {
        // Arrange
        var cartRing = CreateTestCartRing(10);
        var cartId = cartRing.CartIds[5];
        var parcelId = new ParcelId(100);
        var chuteId = new ChuteId(1);

        var mockCartRingBuilder = new Mock<ICartRingBuilder>();
        mockCartRingBuilder.Setup(x => x.CurrentSnapshot).Returns(cartRing);

        var mockCartPositionTracker = new Mock<ICartPositionTracker>();
        mockCartPositionTracker.Setup(x => x.CalculateCartIndexAtOffset(10, It.IsAny<RingLength>()))
            .Returns(new CartIndex(5));

        var cartLifecycleService = new CartLifecycleService();
        cartLifecycleService.InitializeCart(cartId, new CartIndex(5), DateTimeOffset.UtcNow);
        cartLifecycleService.LoadParcel(cartId, parcelId);

        var parcelLifecycleService = new ParcelLifecycleService();
        parcelLifecycleService.CreateParcel(parcelId, "TEST123", DateTimeOffset.UtcNow);
        parcelLifecycleService.BindChuteId(parcelId, chuteId);
        parcelLifecycleService.BindCartId(parcelId, cartId, DateTimeOffset.UtcNow);

        var mockChuteConfigProvider = new Mock<IChuteConfigProvider>();
        mockChuteConfigProvider.Setup(x => x.GetAllConfigs()).Returns(new[]
        {
            new ChuteConfig
            {
                ChuteId = chuteId,
                IsEnabled = true,
                IsForceEject = false,
                CartOffsetFromOrigin = 10,
                MaxOpenDuration = TimeSpan.FromSeconds(2)
            }
        });

        var mockSpeedProvider = new Mock<IMainLineSpeedProvider>();
        mockSpeedProvider.Setup(x => x.IsSpeedStable).Returns(true);
        mockSpeedProvider.Setup(x => x.CurrentMmps).Returns(1000m);

        var planner = CreatePlanner(
            cartRingBuilder: mockCartRingBuilder.Object,
            cartPositionTracker: mockCartPositionTracker.Object,
            cartLifecycleService: cartLifecycleService,
            parcelLifecycleService: parcelLifecycleService,
            chuteConfigProvider: mockChuteConfigProvider.Object,
            mainLineSpeedProvider: mockSpeedProvider.Object);

        // Act
        var plans = planner.PlanEjects(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

        // Assert
        Assert.Single(plans);
        var plan = plans[0];
        Assert.Equal(parcelId, plan.ParcelId);
        Assert.Equal(cartId, plan.CartId);
        Assert.Equal(chuteId, plan.ChuteId);
        Assert.False(plan.IsForceEject);
    }

    [Fact]
    public void PlanEjects_WithForceEject_ShouldGenerateForcePlan()
    {
        // Arrange
        var cartRing = CreateTestCartRing(10);
        var cartId = cartRing.CartIds[5];
        var parcelId = new ParcelId(100);
        var chuteId = new ChuteId(99); // Force eject chute

        var mockCartRingBuilder = new Mock<ICartRingBuilder>();
        mockCartRingBuilder.Setup(x => x.CurrentSnapshot).Returns(cartRing);

        var mockCartPositionTracker = new Mock<ICartPositionTracker>();
        mockCartPositionTracker.Setup(x => x.CalculateCartIndexAtOffset(50, It.IsAny<RingLength>()))
            .Returns(new CartIndex(5));

        var cartLifecycleService = new CartLifecycleService();
        cartLifecycleService.InitializeCart(cartId, new CartIndex(5), DateTimeOffset.UtcNow);
        cartLifecycleService.LoadParcel(cartId, parcelId);

        var parcelLifecycleService = new ParcelLifecycleService();

        var mockChuteConfigProvider = new Mock<IChuteConfigProvider>();
        mockChuteConfigProvider.Setup(x => x.GetAllConfigs()).Returns(new[]
        {
            new ChuteConfig
            {
                ChuteId = chuteId,
                IsEnabled = true,
                IsForceEject = true,
                CartOffsetFromOrigin = 50,
                MaxOpenDuration = TimeSpan.FromSeconds(10)
            }
        });

        var mockSpeedProvider = new Mock<IMainLineSpeedProvider>();
        mockSpeedProvider.Setup(x => x.IsSpeedStable).Returns(true);
        mockSpeedProvider.Setup(x => x.CurrentMmps).Returns(1000m);

        var planner = CreatePlanner(
            cartRingBuilder: mockCartRingBuilder.Object,
            cartPositionTracker: mockCartPositionTracker.Object,
            cartLifecycleService: cartLifecycleService,
            parcelLifecycleService: parcelLifecycleService,
            chuteConfigProvider: mockChuteConfigProvider.Object,
            mainLineSpeedProvider: mockSpeedProvider.Object);

        // Act
        var plans = planner.PlanEjects(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

        // Assert
        Assert.Single(plans);
        var plan = plans[0];
        Assert.Equal(cartId, plan.CartId);
        Assert.Equal(chuteId, plan.ChuteId);
        Assert.True(plan.IsForceEject);
    }

    [Fact]
    public void PlanEjects_WithDisabledChute_ShouldNotGeneratePlan()
    {
        // Arrange
        var cartRing = CreateTestCartRing(10);
        var chuteId = new ChuteId(1);

        var mockCartRingBuilder = new Mock<ICartRingBuilder>();
        mockCartRingBuilder.Setup(x => x.CurrentSnapshot).Returns(cartRing);

        var mockChuteConfigProvider = new Mock<IChuteConfigProvider>();
        mockChuteConfigProvider.Setup(x => x.GetAllConfigs()).Returns(new[]
        {
            new ChuteConfig
            {
                ChuteId = chuteId,
                IsEnabled = false, // Disabled
                IsForceEject = false,
                CartOffsetFromOrigin = 10,
                MaxOpenDuration = TimeSpan.FromSeconds(2)
            }
        });

        var mockSpeedProvider = new Mock<IMainLineSpeedProvider>();
        mockSpeedProvider.Setup(x => x.IsSpeedStable).Returns(true);
        mockSpeedProvider.Setup(x => x.CurrentMmps).Returns(1000m);

        var planner = CreatePlanner(
            cartRingBuilder: mockCartRingBuilder.Object,
            chuteConfigProvider: mockChuteConfigProvider.Object,
            mainLineSpeedProvider: mockSpeedProvider.Object);

        // Act
        var plans = planner.PlanEjects(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

        // Assert
        Assert.Empty(plans);
    }

    private SortingPlanner CreatePlanner(
        ICartRingBuilder? cartRingBuilder = null,
        ICartPositionTracker? cartPositionTracker = null,
        ICartLifecycleService? cartLifecycleService = null,
        IParcelLifecycleService? parcelLifecycleService = null,
        IChuteConfigProvider? chuteConfigProvider = null,
        IMainLineSpeedProvider? mainLineSpeedProvider = null)
    {
        return new SortingPlanner(
            cartRingBuilder ?? Mock.Of<ICartRingBuilder>(),
            cartPositionTracker ?? Mock.Of<ICartPositionTracker>(),
            cartLifecycleService ?? new CartLifecycleService(),
            parcelLifecycleService ?? new ParcelLifecycleService(),
            chuteConfigProvider ?? Mock.Of<IChuteConfigProvider>(),
            mainLineSpeedProvider ?? Mock.Of<IMainLineSpeedProvider>(),
            new SortingPlannerOptions { CartSpacingMm = 500m }
        );
    }

    private CartRingSnapshot CreateTestCartRing(int cartCount)
    {
        var cartIds = Enumerable.Range(0, cartCount)
            .Select(i => new CartId(i))
            .ToList();

        return new CartRingSnapshot
        {
            RingLength = new RingLength(cartCount),
            ZeroCartId = new CartId(0),
            ZeroIndex = new CartIndex(0),
            CartIds = cartIds,
            BuiltAt = DateTimeOffset.UtcNow
        };
    }
}
