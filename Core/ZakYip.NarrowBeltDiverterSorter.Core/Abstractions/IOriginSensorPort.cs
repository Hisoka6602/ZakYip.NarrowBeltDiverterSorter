namespace ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

/// <summary>
/// 原点传感器端口接口
/// 对应物理连接：两个原点IO传感器（用于检测小车和识别0号车）
/// </summary>
public interface IOriginSensorPort
{
    /// <summary>
    /// 获取第一个传感器的当前状态
    /// </summary>
    /// <returns>true表示有物体遮挡，false表示无遮挡</returns>
    bool GetFirstSensorState();

    /// <summary>
    /// 获取第二个传感器的当前状态
    /// </summary>
    /// <returns>true表示有物体遮挡，false表示无遮挡</returns>
    bool GetSecondSensorState();
}
