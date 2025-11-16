namespace ZakYip.NarrowBeltDiverterSorter.Drivers.Chute;

/// <summary>
/// 格口映射配置
/// 将ChuteId映射为总线上的位/寄存器地址
/// </summary>
public class ChuteMappingConfiguration
{
    /// <summary>
    /// 格口地址映射
    /// Key: ChuteId (格口ID)
    /// Value: CoilAddress (线圈地址)
    /// </summary>
    public Dictionary<long, int> ChuteAddressMap { get; set; } = new();

    /// <summary>
    /// 获取格口对应的线圈地址
    /// </summary>
    /// <param name="chuteId">格口ID</param>
    /// <returns>线圈地址，如果未找到返回null</returns>
    public int? GetCoilAddress(long chuteId)
    {
        return ChuteAddressMap.TryGetValue(chuteId, out var address) ? address : null;
    }
}
