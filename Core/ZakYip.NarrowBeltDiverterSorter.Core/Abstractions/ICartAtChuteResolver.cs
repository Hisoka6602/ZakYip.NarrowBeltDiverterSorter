namespace ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

/// <summary>
/// 格口小车号解析器接口
/// 基于当前首车编号和配置，解析指定格口当前窗口的小车编号
/// </summary>
public interface ICartAtChuteResolver
{
    /// <summary>
    /// 基于当前首车编号和配置，解析指定格口当前窗口的小车编号
    /// </summary>
    /// <param name="chuteId">格口ID</param>
    /// <returns>当前窗口的小车编号（1 基索引），如果无法解析则返回失败结果</returns>
    /// <exception cref="InvalidOperationException">当配置未就绪、首车状态未知或配置不完整时抛出</exception>
    int ResolveCurrentCartNumberForChute(long chuteId);

    /// <summary>
    /// 返回当前首车编号（1 基索引）
    /// </summary>
    /// <returns>当前首车编号，如果当前首车未知则抛出受控异常</returns>
    /// <exception cref="InvalidOperationException">当首车状态未知时抛出</exception>
    int GetCurrentHeadCartNumber();
}
