namespace ZakYip.NarrowBeltDiverterSorter.Core.Domain;

/// <summary>
/// 包裹ID（与WheelDiverterSorter上游DTO的ID语义一致）
/// </summary>
public readonly record struct ParcelId
{
    public long Value { get; }

    public ParcelId(long value)
    {
        if (value < 0)
        {
            throw new ArgumentException("ParcelId不能为负值", nameof(value));
        }
        Value = value;
    }
}

/// <summary>
/// 小车ID
/// </summary>
public readonly record struct CartId
{
    public long Value { get; }

    public CartId(long value)
    {
        if (value < 0)
        {
            throw new ArgumentException("CartId不能为负值", nameof(value));
        }
        Value = value;
    }
}

/// <summary>
/// 格口ID
/// </summary>
public readonly record struct ChuteId
{
    public long Value { get; }

    public ChuteId(long value)
    {
        if (value < 0)
        {
            throw new ArgumentException("ChuteId不能为负值", nameof(value));
        }
        Value = value;
    }
}

/// <summary>
/// 环形索引
/// </summary>
public readonly record struct CartIndex
{
    public int Value { get; }

    public CartIndex(int value)
    {
        if (value < 0)
        {
            throw new ArgumentException("CartIndex不能为负值", nameof(value));
        }
        Value = value;
    }
}

/// <summary>
/// 小车总数量
/// </summary>
public readonly record struct RingLength
{
    public int Value { get; }

    public RingLength(int value)
    {
        if (value < 0)
        {
            throw new ArgumentException("RingLength不能为负值", nameof(value));
        }
        Value = value;
    }
}
