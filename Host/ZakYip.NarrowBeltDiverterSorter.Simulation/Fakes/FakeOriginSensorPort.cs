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
        // Simulate cart passing origin sensor with realistic timing
        // First sensor blocked
        _firstSensorState = true;
        _secondSensorState = false;
        await Task.Delay(25); // Front of cart passes first sensor
        
        if (isCartZero)
        {
            // 0号车：双IO都触发 (both sensors blocked simultaneously)
            _secondSensorState = true;
            await Task.Delay(50); // Both sensors blocked
        }
        else
        {
            // 普通车：只触发单个IO
            await Task.Delay(50); // Only first sensor blocked
        }
        
        // Reset both sensors (cart has completely passed)
        _firstSensorState = false;
        _secondSensorState = false;
        
        // Small delay to ensure monitor detects the unblocked state
        await Task.Delay(25);
    }
}
