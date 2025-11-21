using Xunit;
using ZakYip.NarrowBeltDiverterSorter.Simulation;
using ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;
using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.E2ETests;

/// <summary>
/// 测试三种分拣模式的实现
/// </summary>
public class SortingModeTests
{
    [Fact]
    public async Task Normal_Mode_Should_Distribute_Parcels()
    {
        // Arrange
        var config = new SimulationConfiguration
        {
            SortingMode = SortingMode.Normal,
            NumberOfChutes = 10,
            ForceEjectChuteId = 10
        };
        var client = new FakeUpstreamSortingApiClient(config);
        var request = new ParcelRoutingRequestDto
        {
            ParcelId = 1,
            RequestTime = DateTimeOffset.Now
        };

        // Act
        var response = await client.RequestChuteAsync(request);

        // Assert
        Assert.True(response.IsSuccess);
        Assert.InRange(response.ChuteId, 1, 9); // Should not assign to force eject chute
    }

    [Fact]
    public async Task FixedChute_Mode_Should_Always_Return_Same_Chute()
    {
        // Arrange
        var config = new SimulationConfiguration
        {
            SortingMode = SortingMode.FixedChute,
            FixedChuteId = 5,
            NumberOfChutes = 10,
            ForceEjectChuteId = 10
        };
        var client = new FakeUpstreamSortingApiClient(config);

        // Act & Assert
        for (int i = 1; i <= 10; i++)
        {
            var request = new ParcelRoutingRequestDto
            {
                ParcelId = i,
                RequestTime = DateTimeOffset.Now
            };
            var response = await client.RequestChuteAsync(request);

            Assert.True(response.IsSuccess);
            Assert.Equal(5, response.ChuteId);
        }
    }

    [Fact]
    public async Task RoundRobin_Mode_Should_Cycle_Through_Chutes()
    {
        // Arrange
        var config = new SimulationConfiguration
        {
            SortingMode = SortingMode.RoundRobin,
            NumberOfChutes = 10,
            ForceEjectChuteId = 10
        };
        var client = new FakeUpstreamSortingApiClient(config);
        var chuteIds = new List<int>();

        // Act - Request 18 chutes (2 full cycles of 9 chutes)
        for (int i = 1; i <= 18; i++)
        {
            var request = new ParcelRoutingRequestDto
            {
                ParcelId = i,
                RequestTime = DateTimeOffset.Now
            };
            var response = await client.RequestChuteAsync(request);
            chuteIds.Add(response.ChuteId);
        }

        // Assert - Should cycle through chutes 1-9 twice
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, chuteIds);
    }

    [Fact]
    public async Task RoundRobin_Mode_Should_Skip_ForceEject_Chute()
    {
        // Arrange
        var config = new SimulationConfiguration
        {
            SortingMode = SortingMode.RoundRobin,
            NumberOfChutes = 5,
            ForceEjectChuteId = 3 // Force eject is in the middle
        };
        var client = new FakeUpstreamSortingApiClient(config);
        var chuteIds = new List<int>();

        // Act - Request 8 chutes (2 full cycles)
        for (int i = 1; i <= 8; i++)
        {
            var request = new ParcelRoutingRequestDto
            {
                ParcelId = i,
                RequestTime = DateTimeOffset.Now
            };
            var response = await client.RequestChuteAsync(request);
            chuteIds.Add(response.ChuteId);
        }

        // Assert - Should cycle through 1, 2, 4, 5 (skipping 3)
        Assert.Equal(new[] { 1, 2, 4, 5, 1, 2, 4, 5 }, chuteIds);
        Assert.DoesNotContain(3, chuteIds);
    }

    [Fact]
    public async Task FixedChute_Mode_Should_Default_To_Chute_1_When_Not_Specified()
    {
        // Arrange
        var config = new SimulationConfiguration
        {
            SortingMode = SortingMode.FixedChute,
            FixedChuteId = null, // Not specified
            NumberOfChutes = 10,
            ForceEjectChuteId = 10
        };
        var client = new FakeUpstreamSortingApiClient(config);
        var request = new ParcelRoutingRequestDto
        {
            ParcelId = 1,
            RequestTime = DateTimeOffset.Now
        };

        // Act
        var response = await client.RequestChuteAsync(request);

        // Assert
        Assert.True(response.IsSuccess);
        Assert.Equal(1, response.ChuteId);
    }
}
