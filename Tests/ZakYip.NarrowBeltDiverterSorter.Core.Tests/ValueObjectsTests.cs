using ZakYip.NarrowBeltDiverterSorter.Core.Domain;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Tests;

/// <summary>
/// 值对象测试
/// </summary>
public class ValueObjectsTests
{
    [Fact]
    public void ParcelId_Should_Throw_ArgumentException_When_Value_Is_Negative()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new ParcelId(-1));
        Assert.Contains("ParcelId不能为负值", exception.Message);
    }

    [Fact]
    public void ParcelId_Should_Accept_Zero_And_Positive_Values()
    {
        // Act
        var parcelId1 = new ParcelId(0);
        var parcelId2 = new ParcelId(1234567890123);

        // Assert
        Assert.Equal(0, parcelId1.Value);
        Assert.Equal(1234567890123, parcelId2.Value);
    }

    [Fact]
    public void CartId_Should_Throw_ArgumentException_When_Value_Is_Negative()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new CartId(-1));
        Assert.Contains("CartId不能为负值", exception.Message);
    }

    [Fact]
    public void CartId_Should_Accept_Zero_And_Positive_Values()
    {
        // Act
        var cartId1 = new CartId(0);
        var cartId2 = new CartId(100);

        // Assert
        Assert.Equal(0, cartId1.Value);
        Assert.Equal(100, cartId2.Value);
    }

    [Fact]
    public void ChuteId_Should_Throw_ArgumentException_When_Value_Is_Negative()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new ChuteId(-1));
        Assert.Contains("ChuteId不能为负值", exception.Message);
    }

    [Fact]
    public void ChuteId_Should_Accept_Zero_And_Positive_Values()
    {
        // Act
        var chuteId1 = new ChuteId(0);
        var chuteId2 = new ChuteId(50);

        // Assert
        Assert.Equal(0, chuteId1.Value);
        Assert.Equal(50, chuteId2.Value);
    }

    [Fact]
    public void CartIndex_Should_Throw_ArgumentException_When_Value_Is_Negative()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new CartIndex(-1));
        Assert.Contains("CartIndex不能为负值", exception.Message);
    }

    [Fact]
    public void CartIndex_Should_Accept_Zero_And_Positive_Values()
    {
        // Act
        var cartIndex1 = new CartIndex(0);
        var cartIndex2 = new CartIndex(99);

        // Assert
        Assert.Equal(0, cartIndex1.Value);
        Assert.Equal(99, cartIndex2.Value);
    }

    [Fact]
    public void RingLength_Should_Throw_ArgumentException_When_Value_Is_Negative()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new RingLength(-1));
        Assert.Contains("RingLength不能为负值", exception.Message);
    }

    [Fact]
    public void RingLength_Should_Accept_Zero_And_Positive_Values()
    {
        // Act
        var ringLength1 = new RingLength(0);
        var ringLength2 = new RingLength(200);

        // Assert
        Assert.Equal(0, ringLength1.Value);
        Assert.Equal(200, ringLength2.Value);
    }
}
