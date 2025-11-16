using ZakYip.NarrowBeltDiverterSorter.Communication;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;

/// <summary>
/// 模拟现场总线客户端
/// </summary>
public class FakeFieldBusClient : IFieldBusClient
{
    private bool _isConnected;
    private readonly Dictionary<int, bool> _coils = new();
    private readonly Dictionary<int, ushort> _registers = new();

    public Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        _isConnected = true;
        Console.WriteLine($"[总线] 已连接到现场总线");
        return Task.FromResult(true);
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _isConnected = false;
        Console.WriteLine($"[总线] 已断开现场总线");
        return Task.CompletedTask;
    }

    public Task<bool> WriteSingleCoilAsync(int address, bool value, CancellationToken cancellationToken = default)
    {
        _coils[address] = value;
        return Task.FromResult(true);
    }

    public Task<bool> WriteMultipleCoilsAsync(int startAddress, bool[] values, CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < values.Length; i++)
        {
            _coils[startAddress + i] = values[i];
        }
        return Task.FromResult(true);
    }

    public Task<bool> WriteSingleRegisterAsync(int address, ushort value, CancellationToken cancellationToken = default)
    {
        _registers[address] = value;
        return Task.FromResult(true);
    }

    public Task<bool> WriteMultipleRegistersAsync(int startAddress, ushort[] values, CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < values.Length; i++)
        {
            _registers[startAddress + i] = values[i];
        }
        return Task.FromResult(true);
    }

    public Task<bool[]?> ReadCoilsAsync(int address, int count, CancellationToken cancellationToken = default)
    {
        var result = new bool[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = _coils.TryGetValue(address + i, out var value) ? value : false;
        }
        return Task.FromResult<bool[]?>(result);
    }

    public Task<bool[]?> ReadDiscreteInputsAsync(int address, int count, CancellationToken cancellationToken = default)
    {
        var result = new bool[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = _coils.TryGetValue(address + i, out var value) ? value : false;
        }
        return Task.FromResult<bool[]?>(result);
    }

    public Task<ushort[]?> ReadHoldingRegistersAsync(int address, int count, CancellationToken cancellationToken = default)
    {
        var result = new ushort[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = _registers.TryGetValue(address + i, out var value) ? value : (ushort)0;
        }
        return Task.FromResult<ushort[]?>(result);
    }

    public Task<ushort[]?> ReadInputRegistersAsync(int address, int count, CancellationToken cancellationToken = default)
    {
        var result = new ushort[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = _registers.TryGetValue(address + i, out var value) ? value : (ushort)0;
        }
        return Task.FromResult<ushort[]?>(result);
    }

    public bool IsConnected()
    {
        return _isConnected;
    }
}
