using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;

/// <summary>
/// 模拟原点传感器端口
/// </summary>
public class FakeOriginSensorPort : IOriginSensorPort
{
    private bool _firstSensorState;
    private bool _secondSensorState;

    public bool GetFirstSensorState()
    {
        return _firstSensorState;
    }

    public bool GetSecondSensorState()
    {
        return _secondSensorState;
    }

    public async Task SimulateCartPassingAsync(bool isCartZero)
    {
        if (isCartZero)
        {
            // 0号车：双IO都触发
            _firstSensorState = true;
            _secondSensorState = true;
        }
        else
        {
            // 普通车：只触发单个IO
            _firstSensorState = true;
            _secondSensorState = false;
        }

        // Hold sensor state for enough time for OriginSensorMonitor to detect 
        // Polling interval is 10ms by default, so 50ms gives 5 polling cycles for detection
        await Task.Delay(50);
        
        // Reset sensor state
        _firstSensorState = false;
        _secondSensorState = false;
    }
}
