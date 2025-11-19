using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Topology;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Tests.Topology;

/// <summary>
/// 轨道拓扑测试
/// </summary>
public class TrackTopologyTests
{
    [Fact]
    public void Constructor_WithValidOptions_CreatesTopology()
    {
        // Arrange
        var options = CreateTestOptions(cartCount: 20, spacingMm: 500m, chuteCount: 10);

        // Act
        var topology = new TrackTopology(options);

        // Assert
        Assert.Equal(20, topology.CartCount);
        Assert.Equal(500m, topology.CartSpacingMm);
        Assert.Equal(10000m, topology.RingTotalLengthMm); // 20 * 500
        Assert.Equal(10, topology.ChuteCount);
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TrackTopology(null!));
    }

    [Fact]
    public void Constructor_WithInvalidCartCount_ThrowsArgumentException()
    {
        // Arrange
        var options = CreateTestOptions(cartCount: 0, spacingMm: 500m, chuteCount: 10);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TrackTopology(options));
    }

    [Fact]
    public void Constructor_WithInvalidSpacing_ThrowsArgumentException()
    {
        // Arrange
        var options = CreateTestOptions(cartCount: 20, spacingMm: 0m, chuteCount: 10);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TrackTopology(options));
    }

    [Fact]
    public void GetCartPosition_WithValidCartId_ReturnsCorrectPosition()
    {
        // Arrange
        var topology = new TrackTopology(CreateTestOptions(20, 500m, 10));

        // Act & Assert
        Assert.Equal(0m, topology.GetCartPosition(new CartId(0)));
        Assert.Equal(500m, topology.GetCartPosition(new CartId(1)));
        Assert.Equal(1000m, topology.GetCartPosition(new CartId(2)));
        Assert.Equal(9500m, topology.GetCartPosition(new CartId(19)));
    }

    [Fact]
    public void GetCartPosition_WithInvalidCartId_ReturnsNull()
    {
        // Arrange
        var topology = new TrackTopology(CreateTestOptions(20, 500m, 10));

        // Act & Assert
        Assert.Null(topology.GetCartPosition(new CartId(20)));
        Assert.Null(topology.GetCartPosition(new CartId(100)));
    }

    [Fact]
    public void GetCartIdByPosition_WithValidPosition_ReturnsCorrectCartId()
    {
        // Arrange
        var topology = new TrackTopology(CreateTestOptions(20, 500m, 10));

        // Act & Assert
        Assert.Equal(0, topology.GetCartIdByPosition(0m)?.Value);
        Assert.Equal(1, topology.GetCartIdByPosition(500m)?.Value);
        Assert.Equal(2, topology.GetCartIdByPosition(1000m)?.Value);
        Assert.Equal(0, topology.GetCartIdByPosition(10000m)?.Value); // Wraps around
    }

    [Fact]
    public void GetCartIdByPosition_WithNegativePosition_ReturnsNull()
    {
        // Arrange
        var topology = new TrackTopology(CreateTestOptions(20, 500m, 10));

        // Act & Assert
        Assert.Null(topology.GetCartIdByPosition(-100m));
    }

    [Fact]
    public void GetChutePosition_WithValidChuteId_ReturnsCorrectPosition()
    {
        // Arrange
        var topology = new TrackTopology(CreateTestOptions(20, 500m, 10));

        // Act & Assert
        // 格口1在cart offset 2
        Assert.Equal(1000m, topology.GetChutePosition(new ChuteId(1)));
        // 格口5在cart offset 10
        Assert.Equal(5000m, topology.GetChutePosition(new ChuteId(5)));
    }

    [Fact]
    public void GetChutePosition_WithInvalidChuteId_ReturnsNull()
    {
        // Arrange
        var topology = new TrackTopology(CreateTestOptions(20, 500m, 10));

        // Act & Assert
        Assert.Null(topology.GetChutePosition(new ChuteId(999)));
    }

    [Fact]
    public void GetStrongEjectChuteId_WhenConfigured_ReturnsCorrectId()
    {
        // Arrange
        var options = CreateTestOptions(20, 500m, 10, forceEjectChuteId: 10);
        var topology = new TrackTopology(options);

        // Act
        var strongEjectChuteId = topology.GetStrongEjectChuteId();

        // Assert
        Assert.NotNull(strongEjectChuteId);
        Assert.Equal(10, strongEjectChuteId.Value.Value);
    }

    [Fact]
    public void GetStrongEjectChuteId_WhenNotConfigured_ReturnsNull()
    {
        // Arrange
        var topology = new TrackTopology(CreateTestOptions(20, 500m, 10));

        // Act
        var strongEjectChuteId = topology.GetStrongEjectChuteId();

        // Assert
        Assert.Null(strongEjectChuteId);
    }

    [Fact]
    public void GetChuteCartOffset_WithValidChuteId_ReturnsCorrectOffset()
    {
        // Arrange
        var topology = new TrackTopology(CreateTestOptions(20, 500m, 10));

        // Act & Assert
        Assert.Equal(2, topology.GetChuteCartOffset(new ChuteId(1)));
        Assert.Equal(10, topology.GetChuteCartOffset(new ChuteId(5)));
    }

    [Fact]
    public void GetChuteCartOffset_WithInvalidChuteId_ReturnsNull()
    {
        // Arrange
        var topology = new TrackTopology(CreateTestOptions(20, 500m, 10));

        // Act & Assert
        Assert.Null(topology.GetChuteCartOffset(new ChuteId(999)));
    }

    [Fact]
    public void TrackTopology_WithRealWorldScenario_WorksCorrectly()
    {
        // Arrange: 20车，节距500mm，10个格口的拓扑
        var options = CreateTestOptions(
            cartCount: 20,
            spacingMm: 500m,
            chuteCount: 10,
            forceEjectChuteId: 10);

        // Act
        var topology = new TrackTopology(options);

        // Assert: 验证基本属性
        Assert.Equal(20, topology.CartCount);
        Assert.Equal(500m, topology.CartSpacingMm);
        Assert.Equal(10000m, topology.RingTotalLengthMm);
        Assert.Equal(10, topology.ChuteCount);

        // Assert: 验证强排口配置
        var strongEjectChuteId = topology.GetStrongEjectChuteId();
        Assert.NotNull(strongEjectChuteId);
        Assert.Equal(10, strongEjectChuteId.Value.Value);

        // Assert: 验证格口位置（强排口在cart offset 19）
        var strongEjectPosition = topology.GetChutePosition(strongEjectChuteId.Value);
        Assert.Equal(9500m, strongEjectPosition); // 19 * 500mm

        // Assert: 验证小车位置计算
        Assert.Equal(0m, topology.GetCartPosition(new CartId(0)));
        Assert.Equal(9500m, topology.GetCartPosition(new CartId(19)));

        // Assert: 验证位置到小车ID的转换
        Assert.Equal(0, topology.GetCartIdByPosition(0m)?.Value);
        Assert.Equal(19, topology.GetCartIdByPosition(9500m)?.Value);
    }

    private static TrackTopologyOptions CreateTestOptions(
        int cartCount,
        decimal spacingMm,
        int chuteCount,
        int? forceEjectChuteId = null)
    {
        var options = new TrackTopologyOptions
        {
            CartCount = cartCount,
            CartSpacingMm = spacingMm,
            ForceEjectChuteId = forceEjectChuteId,
            InfeedDropPointOffsetMm = 0m,
            ChutePositions = new List<ChutePositionConfig>()
        };

        // 创建格口配置：格口均匀分布
        // 格口1-10，分别在cart offset 2, 4, 6, 8, 10, 12, 14, 16, 18, 19
        for (int i = 0; i < chuteCount; i++)
        {
            int chuteId = i + 1;
            int cartOffset = (i < chuteCount - 1) ? (i + 1) * 2 : 19; // 最后一个格口（强排口）在19
            options.ChutePositions.Add(new ChutePositionConfig
            {
                ChuteId = new ChuteId(chuteId),
                CartOffsetFromOrigin = cartOffset
            });
        }

        return options;
    }
}
