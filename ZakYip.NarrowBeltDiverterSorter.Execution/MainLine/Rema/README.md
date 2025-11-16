# 雷马 LM1000H Modbus 通讯层

本模块提供了雷马 LM1000H 变频驱动器的 Modbus RTU 通讯接口封装。

## 架构概览

```
IRemaLm1000HClient (业务操作接口)
    ↓
RemaLm1000HClient (业务逻辑实现，带错误处理)
    ↓
IModbusClient (Modbus 协议抽象)
    ↓
ModbusClientAdapter (适配器)
    ↓
IRemaLm1000HTransport (底层传输)
    ↓
StubRemaLm1000HTransport (桩实现) / ModbusRtuTransport (真实实现)
```

## 核心组件

### 1. IRemaLm1000HClient - 业务操作接口

高级业务操作接口，提供面向应用的方法：

```csharp
public interface IRemaLm1000HClient : IDisposable
{
    // 连接管理
    Task<OperationResult> ConnectAsync(CancellationToken cancellationToken = default);
    Task<OperationResult> DisconnectAsync();
    bool IsConnected { get; }

    // 运行控制
    Task<OperationResult> StartForwardAsync(CancellationToken cancellationToken = default);
    Task<OperationResult> StopDecelerateAsync(CancellationToken cancellationToken = default);

    // 速度控制
    Task<OperationResult> SetTargetFrequencyAsync(decimal targetHz, CancellationToken cancellationToken = default);
    Task<OperationResult> SetTargetSpeedAsync(decimal targetMmps, CancellationToken cancellationToken = default);

    // 状态读取
    Task<OperationResult<decimal>> ReadCurrentFrequencyAsync(CancellationToken cancellationToken = default);
    Task<OperationResult<decimal>> ReadCurrentSpeedAsync(CancellationToken cancellationToken = default);
    Task<OperationResult<int>> ReadRunStatusAsync(CancellationToken cancellationToken = default);

    // 参数设置
    Task<OperationResult> SetTorqueLimitAsync(int torqueLimit, CancellationToken cancellationToken = default);
    Task<OperationResult> InitializeAsync(CancellationToken cancellationToken = default);
}
```

### 2. RemaLm1000HClient - 客户端实现

封装所有通讯异常，返回 `OperationResult` 类型，保证调用者不会受到异常影响：

```csharp
var result = await client.SetTargetSpeedAsync(1500); // 设置 1500 mm/s
if (result.IsSuccess)
{
    // 操作成功
}
else
{
    // 操作失败，检查 result.ErrorMessage 和 result.Exception
    logger.LogError("设置速度失败: {Error}", result.ErrorMessage);
}
```

### 3. OperationResult - 结果类型

用于封装操作结果，避免异常传播：

```csharp
// 无返回值的操作
public readonly struct OperationResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public Exception? Exception { get; }
}

// 有返回值的操作
public readonly struct OperationResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorMessage { get; }
    public Exception? Exception { get; }
}
```

### 4. RemaLm1000HConnectionOptions - 连接配置

Modbus RTU 连接参数配置：

```csharp
public sealed record class RemaLm1000HConnectionOptions
{
    public required string PortName { get; init; }        // "COM1" 或 "/dev/ttyS0"
    public required int BaudRate { get; init; }           // 9600, 19200, 38400 等
    public int DataBits { get; init; } = 8;               // 数据位
    public SerialParity Parity { get; init; }             // 奇偶校验
    public SerialStopBits StopBits { get; init; }         // 停止位
    public required byte SlaveAddress { get; init; }      // Modbus 从站地址 (1-247)
    public TimeSpan ReadTimeout { get; init; }            // 读超时
    public TimeSpan WriteTimeout { get; init; }           // 写超时
    public int MaxRetries { get; init; } = 3;             // 重试次数
}
```

### 5. RemaRegisters - 寄存器地址常量

所有寄存器地址定义，参考 LM1000H 说明书：

```csharp
public static class RemaRegisters
{
    // 控制字寄存器
    public const ushort ControlWord = 0x2000;

    // 设定参数（P组）
    public const ushort P0_01_RunCmdSource = 0xF001;
    public const ushort P0_07_LimitFrequency = 0xF007;
    public const ushort P3_10_TorqueRef = 0x030A;

    // 监控参数（C组）
    public const ushort C0_01_OutputCurrent = 0x5001;
    public const ushort C0_26_EncoderFrequency = 0x501A;
    public const ushort C0_32_RunStatus = 0x5020;

    // ... 更多寄存器定义
}
```

### 6. RemaScaling - 换算常量

所有单位换算常量，避免魔法数字：

```csharp
public static class RemaScaling
{
    // 频率换算
    public const decimal P005_HzPerCount = 0.01m;      // 寄存器值 × 0.01 = Hz
    public const decimal C026_HzPerCount = 0.01m;      // 寄存器值 × 0.01 = Hz
    public const decimal HzToMmps = 100m;              // Hz × 100 = mm/s
    public const decimal MmpsToHz = 0.01m;             // mm/s × 0.01 = Hz

    // 扭矩常量
    public const int TorqueMaxAbsolute = 1000;         // 1000 = 100% 额定电流

    // 运行状态代码
    public const int RunStatus_Forward = 1;
    public const int RunStatus_Stopped = 3;
    public const int RunStatus_Fault = 5;

    // 控制命令代码
    public const int ControlCmd_Forward = 1;
    public const int ControlCmd_Decelerate = 5;
}
```

## 使用示例

### 示例 1：基本使用

```csharp
// 1. 配置连接参数
var connectionOptions = new RemaLm1000HConnectionOptions
{
    PortName = "COM1",
    BaudRate = 9600,
    SlaveAddress = 1,
    ReadTimeout = TimeSpan.FromSeconds(1),
    WriteTimeout = TimeSpan.FromSeconds(1)
};

// 2. 配置驱动参数
var driveOptions = new RemaLm1000HOptions
{
    LoopPeriod = TimeSpan.FromMilliseconds(100),
    LimitHz = 50.0m,
    TorqueMax = 1000,
    // ... 其他参数
};

// 3. 创建传输层（使用桩实现用于测试）
var transport = new StubRemaLm1000HTransport(logger);
var modbusClient = new ModbusClientAdapter(transport, logger);

// 4. 创建客户端
var client = new RemaLm1000HClient(
    modbusClient,
    Options.Create(connectionOptions),
    Options.Create(driveOptions),
    logger);

// 5. 连接并初始化
var connectResult = await client.ConnectAsync();
if (!connectResult.IsSuccess)
{
    logger.LogError("连接失败: {Error}", connectResult.ErrorMessage);
    return;
}

var initResult = await client.InitializeAsync();
if (!initResult.IsSuccess)
{
    logger.LogError("初始化失败: {Error}", initResult.ErrorMessage);
    return;
}

// 6. 启动电机
var startResult = await client.StartForwardAsync();
if (!startResult.IsSuccess)
{
    logger.LogError("启动失败: {Error}", startResult.ErrorMessage);
    return;
}

// 7. 设置目标速度
var speedResult = await client.SetTargetSpeedAsync(1500); // 1500 mm/s
if (!speedResult.IsSuccess)
{
    logger.LogError("设置速度失败: {Error}", speedResult.ErrorMessage);
}

// 8. 读取当前速度
var currentSpeedResult = await client.ReadCurrentSpeedAsync();
if (currentSpeedResult.IsSuccess)
{
    logger.LogInformation("当前速度: {Speed} mm/s", currentSpeedResult.Value);
}

// 9. 停止电机
await client.StopDecelerateAsync();
await client.DisconnectAsync();
client.Dispose();
```

### 示例 2：依赖注入集成

```csharp
// 在 Program.cs 或 Startup.cs 中注册服务
services.Configure<RemaLm1000HConnectionOptions>(
    configuration.GetSection("RemaLm1000H:Connection"));

services.Configure<RemaLm1000HOptions>(
    configuration.GetSection("RemaLm1000H:Drive"));

// 注册传输层（开发/测试使用桩实现）
services.AddSingleton<IRemaLm1000HTransport, StubRemaLm1000HTransport>();

// 注册 Modbus 客户端
services.AddSingleton<IModbusClient, ModbusClientAdapter>();

// 注册雷马客户端
services.AddSingleton<IRemaLm1000HClient, RemaLm1000HClient>();
```

### 示例 3：错误处理

```csharp
// 方式 1：检查成功状态
var result = await client.SetTargetSpeedAsync(1500);
if (!result.IsSuccess)
{
    logger.LogError("操作失败: {Error}", result.ErrorMessage);
    // 根据错误类型进行处理
    if (result.Exception is TimeoutException)
    {
        // 处理超时
    }
    else if (result.Exception is IOException)
    {
        // 处理 IO 错误
    }
}

// 方式 2：使用模式匹配
var speedResult = await client.ReadCurrentSpeedAsync();
if (speedResult is { IsSuccess: true, Value: var speed })
{
    logger.LogInformation("当前速度: {Speed} mm/s", speed);
}
else
{
    logger.LogError("读取速度失败: {Error}", speedResult.ErrorMessage);
}
```

## 配置示例

### appsettings.json

```json
{
  "RemaLm1000H": {
    "Connection": {
      "PortName": "COM1",
      "BaudRate": 9600,
      "DataBits": 8,
      "Parity": "None",
      "StopBits": "One",
      "SlaveAddress": 1,
      "ReadTimeout": "00:00:01",
      "WriteTimeout": "00:00:01",
      "ConnectTimeout": "00:00:03",
      "MaxRetries": 3,
      "RetryDelay": "00:00:00.200"
    },
    "Drive": {
      "LoopPeriod": "00:00:00.100",
      "LimitHz": 50.0,
      "TorqueMax": 1000,
      "TorqueMaxWhenOverLimit": 800,
      "MinMmps": 0,
      "MaxMmps": 5000,
      "StableDeadbandMmps": 10,
      "StableHold": "00:00:02"
    }
  }
}
```

## 单元测试

所有组件都有完整的单元测试覆盖，参见 `RemaLm1000HClientTests.cs`：

```csharp
[Fact]
public async Task SetTargetSpeedAsync_ConvertsCorrectly()
{
    // Arrange
    var client = CreateClient();

    // Act
    var result = await client.SetTargetSpeedAsync(1500); // 1500 mm/s

    // Assert
    Assert.True(result.IsSuccess);
    // 验证寄存器值 = 1500 × 0.01 / 0.01 = 1500
}

[Fact]
public async Task ReadCurrentSpeedAsync_ReturnsCorrectValue()
{
    // Arrange
    var client = CreateClient();
    // 设置模拟的编码器反馈：1500 = 15.00 Hz = 1500 mm/s

    // Act
    var result = await client.ReadCurrentSpeedAsync();

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(1500m, result.Value);
}
```

## 扩展真实 Modbus RTU 实现

要接入真实的 Modbus RTU 硬件，需要实现 `IRemaLm1000HTransport` 接口，例如使用 `NModbus` 或 `FluentModbus` 库：

```csharp
public class ModbusRtuTransport : IRemaLm1000HTransport
{
    private readonly IModbusMaster _master; // 来自第三方库
    private readonly byte _slaveAddress;

    public async Task WriteRegisterAsync(ushort address, ushort value, CancellationToken ct)
    {
        await _master.WriteSingleRegisterAsync(_slaveAddress, address, value);
    }

    public async Task<ushort> ReadRegisterAsync(ushort address, CancellationToken ct)
    {
        var result = await _master.ReadHoldingRegistersAsync(_slaveAddress, address, 1);
        return result[0];
    }

    // ... 实现其他方法
}
```

然后在依赖注入中替换：

```csharp
// 生产环境使用真实实现
services.AddSingleton<IRemaLm1000HTransport, ModbusRtuTransport>();
```

## 参考文档

- **LM1000H 说明书** - 变频驱动器参数和寄存器定义
- **Modbus 应用协议规范 V1.1b3** - Modbus 协议标准
- **Modbus RTU 实现指南** - RTU 串行传输模式

## 常见问题

### Q: 如何切换到真实的 Modbus RTU 通讯？
A: 实现 `IRemaLm1000HTransport` 接口，使用第三方 Modbus 库（如 NModbus, FluentModbus），然后在依赖注入中替换 `StubRemaLm1000HTransport`。

### Q: 超时和重试如何处理？
A: 在 `RemaLm1000HConnectionOptions` 中配置 `ReadTimeout`, `WriteTimeout`, `MaxRetries` 和 `RetryDelay`。真实实现应在传输层处理重试逻辑。

### Q: 如何监控通讯状态？
A: 使用 `IRemaLm1000HClient.IsConnected` 属性，并检查 `ReadRunStatusAsync()` 返回的运行状态。所有操作都返回 `OperationResult`，可以检查 `IsSuccess` 判断通讯是否正常。

### Q: 寄存器地址从哪里来？
A: 所有寄存器地址都定义在 `RemaRegisters` 类中，参考 LM1000H 说明书的 Modbus 地址映射表。

### Q: 单位换算是否正确？
A: 所有换算系数都定义在 `RemaScaling` 类中，基于 LM1000H 说明书：
  - 频率：寄存器值 × 0.01 = Hz
  - 线速：Hz × 100 = mm/s
  - 扭矩：1000 = 100% 额定电流
