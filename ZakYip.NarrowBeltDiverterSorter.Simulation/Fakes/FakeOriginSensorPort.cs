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

    public void SimulateCartPassing(bool isCartZero)
    {
        if (isCartZero)
        {
            // 0号车：双IO都触发
            _firstSensorState = true;
            _secondSensorState = true;
            Console.WriteLine($"[原点传感器] 0号车通过 - 双IO触发");
        }
        else
        {
            // 普通车：只触发单个IO
            _firstSensorState = true;
            _secondSensorState = false;
            Console.WriteLine($"[原点传感器] 普通车通过 - 单IO触发");
        }

        // 模拟传感器恢复
        Task.Delay(50).ContinueWith(_ =>
        {
            _firstSensorState = false;
            _secondSensorState = false;
        });
    }
}
