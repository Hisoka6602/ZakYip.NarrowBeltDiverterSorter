namespace ZakYip.NarrowBeltDiverterSorter.Simulator.Tests.Simulation.PanelStartToChuteDropSimulation;

/// <summary>
/// 仿真IO板 - 用于在测试中模拟IO输入输出
/// </summary>
public sealed class SimulatedIoBoard
{
    private readonly Dictionary<int, bool> _inputs = new();
    private readonly Dictionary<int, bool> _outputs = new();
    private readonly Dictionary<int, List<IoEvent>> _outputHistory = new();

    /// <summary>
    /// IO事件记录
    /// </summary>
    public sealed record IoEvent(int Tick, bool Value);

    /// <summary>
    /// 设置输入通道状态（仿真专用）
    /// </summary>
    /// <param name="channel">通道号</param>
    /// <param name="value">状态值</param>
    public void SetInput(int channel, bool value)
    {
        _inputs[channel] = value;
    }

    /// <summary>
    /// 读取输入通道状态
    /// </summary>
    /// <param name="channel">通道号</param>
    /// <returns>状态值</returns>
    public bool ReadInput(int channel)
    {
        return _inputs.TryGetValue(channel, out var value) && value;
    }

    /// <summary>
    /// 写入输出通道状态
    /// </summary>
    /// <param name="channel">通道号</param>
    /// <param name="value">状态值</param>
    /// <param name="currentTick">当前仿真时刻</param>
    public void WriteOutput(int channel, bool value, int currentTick)
    {
        _outputs[channel] = value;
        
        if (!_outputHistory.ContainsKey(channel))
        {
            _outputHistory[channel] = new List<IoEvent>();
        }
        
        _outputHistory[channel].Add(new IoEvent(currentTick, value));
    }

    /// <summary>
    /// 读取输出通道状态
    /// </summary>
    /// <param name="channel">通道号</param>
    /// <returns>状态值</returns>
    public bool ReadOutput(int channel)
    {
        return _outputs.TryGetValue(channel, out var value) && value;
    }

    /// <summary>
    /// 获取输出历史记录
    /// </summary>
    /// <param name="channel">通道号</param>
    /// <returns>事件列表</returns>
    public IReadOnlyList<IoEvent> GetOutputHistory(int channel)
    {
        return _outputHistory.TryGetValue(channel, out var history) 
            ? history 
            : Array.Empty<IoEvent>();
    }

    /// <summary>
    /// 获取所有输出历史记录
    /// </summary>
    /// <returns>通道到事件列表的映射</returns>
    public IReadOnlyDictionary<int, IReadOnlyList<IoEvent>> GetAllOutputHistory()
    {
        return _outputHistory.ToDictionary(
            kvp => kvp.Key, 
            kvp => (IReadOnlyList<IoEvent>)kvp.Value);
    }
}
