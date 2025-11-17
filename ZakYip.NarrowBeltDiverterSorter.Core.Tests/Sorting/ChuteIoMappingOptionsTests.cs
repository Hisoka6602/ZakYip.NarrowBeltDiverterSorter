using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Tests.Sorting;

/// <summary>
/// 格口 IO 映射配置选项测试
/// </summary>
public class ChuteIoMappingOptionsTests
{
    [Fact]
    public void CreateDefault_Should_Create_Valid_Mapping()
    {
        // Act
        var options = ChuteIoMappingOptions.CreateDefault(numberOfChutes: 5, strongEjectChuteId: 5);

        // Assert
        Assert.Equal(5, options.StrongEjectChuteId);
        Assert.Equal(5, options.ChuteIdToIoChannel.Count);
        Assert.Equal(200, options.PulseDurationMilliseconds);

        // Verify default mapping: ChuteId -> IoChannel is 1:1
        for (int i = 1; i <= 5; i++)
        {
            Assert.True(options.ChuteIdToIoChannel.ContainsKey(i));
            Assert.Equal(i, options.ChuteIdToIoChannel[i]);
        }
    }

    [Fact]
    public void CreateDefault_With_Default_Parameters_Should_Create_10_Chutes()
    {
        // Act
        var options = ChuteIoMappingOptions.CreateDefault();

        // Assert
        Assert.Equal(10, options.StrongEjectChuteId);
        Assert.Equal(10, options.ChuteIdToIoChannel.Count);
    }

    [Fact]
    public void Custom_Mapping_Should_Be_Supported()
    {
        // Arrange
        var customMapping = new Dictionary<long, int>
        {
            { 1, 10 },
            { 2, 20 },
            { 3, 30 }
        };

        // Act
        var options = new ChuteIoMappingOptions
        {
            StrongEjectChuteId = 3,
            ChuteIdToIoChannel = customMapping,
            PulseDurationMilliseconds = 150
        };

        // Assert
        Assert.Equal(3, options.StrongEjectChuteId);
        Assert.Equal(3, options.ChuteIdToIoChannel.Count);
        Assert.Equal(150, options.PulseDurationMilliseconds);
        Assert.Equal(10, options.ChuteIdToIoChannel[1]);
        Assert.Equal(20, options.ChuteIdToIoChannel[2]);
        Assert.Equal(30, options.ChuteIdToIoChannel[3]);
    }
}
