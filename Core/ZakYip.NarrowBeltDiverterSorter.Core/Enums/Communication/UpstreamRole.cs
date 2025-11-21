namespace ZakYip.NarrowBeltDiverterSorter.Core.Enums.Communication;

/// <summary>
/// 上游通讯角色枚举
/// </summary>
public enum UpstreamRole
{
    /// <summary>
    /// 客户端模式：主动连接到上游服务器
    /// </summary>
    Client,

    /// <summary>
    /// 服务端模式：作为服务器接收上游连接
    /// </summary>
    Server
}
