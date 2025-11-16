using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using ZakYip.NarrowBeltDiverterSorter.Execution.MainLine.Rema;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Tests.MainLine.Rema;

/// <summary>
/// 雷马 LM1000H 客户端单元测试
/// </summary>
public class RemaLm1000HClientTests
{
    private readonly RemaLm1000HConnectionOptions _connectionOptions;
    private readonly RemaLm1000HOptions _driveOptions;

    public RemaLm1000HClientTests()
    {
        _connectionOptions = new RemaLm1000HConnectionOptions
        {
            PortName = "COM1",
            BaudRate = 9600,
            SlaveAddress = 1
        };

        _driveOptions = new RemaLm1000HOptions
        {
            LoopPeriod = TimeSpan.FromMilliseconds(100),
            TorqueMax = 1000,
            TorqueMaxWhenOverLimit = 800,
            TorqueMaxWhenOverCurrent = 600,
            TorqueMaxUnderHighLoad = 700,
            LimitHz = 50.0m,
            LimitOvershootHz = 0.35m,
            StableDeadbandMmps = 10m,
            StableHold = TimeSpan.FromSeconds(2),
            UnstableThresholdMmps = 50m,
            UnstableHold = TimeSpan.FromSeconds(5),
            MicroBandMmps = 20m,
            TorqueSlewPerLoop = 15,
            Pid = new PidGains { Kp = 1.0m, Ki = 0.1m, Kd = 0.01m },
            PidIntegralClamp = 1000m,
            MinMmps = 0m,
            MaxMmps = 5000m,
            RatedCurrentScale = 1.0m,
            FallbackRatedCurrentA = 6.0m,
            CurrentLimitRatio = 1.2m,
            OverCurrentIntegralDecay = 0.5m,
            HighLoadRatio = 0.9m,
            HighLoadHold = TimeSpan.FromSeconds(10),
            LowSpeedBandMmps = 350m,
            FrictionCmd = 50m,
            LowSpeedKiBoost = 1.5m,
            StartMoveCmdFloor = 60
        };
    }

    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Arrange
        var transport = new StubRemaLm1000HTransport(NullLogger<StubRemaLm1000HTransport>.Instance);
        var modbusClient = new ModbusClientAdapter(transport, NullLogger<ModbusClientAdapter>.Instance);
        var connectionOptions = Options.Create(_connectionOptions);
        var driveOptions = Options.Create(_driveOptions);
        var logger = NullLogger<RemaLm1000HClient>.Instance;

        // Act
        var client = new RemaLm1000HClient(modbusClient, connectionOptions, driveOptions, logger);

        // Assert
        Assert.NotNull(client);
        Assert.True(client.IsConnected);
    }

    [Fact]
    public async Task StartForwardAsync_WritesCorrectControlWord()
    {
        // Arrange
        var transport = new StubRemaLm1000HTransport(NullLogger<StubRemaLm1000HTransport>.Instance);
        var modbusClient = new ModbusClientAdapter(transport, NullLogger<ModbusClientAdapter>.Instance);
        var client = new RemaLm1000HClient(
            modbusClient,
            Options.Create(_connectionOptions),
            Options.Create(_driveOptions),
            NullLogger<RemaLm1000HClient>.Instance);

        // Act
        var result = await client.StartForwardAsync();

        // Assert
        Assert.True(result.IsSuccess);
        var controlWord = await transport.ReadRegisterAsync(RemaRegisters.ControlWord);
        Assert.Equal(RemaScaling.ControlCmd_Forward, controlWord);
    }

    [Fact]
    public async Task StopDecelerateAsync_WritesCorrectControlWord()
    {
        // Arrange
        var transport = new StubRemaLm1000HTransport(NullLogger<StubRemaLm1000HTransport>.Instance);
        var modbusClient = new ModbusClientAdapter(transport, NullLogger<ModbusClientAdapter>.Instance);
        var client = new RemaLm1000HClient(
            modbusClient,
            Options.Create(_connectionOptions),
            Options.Create(_driveOptions),
            NullLogger<RemaLm1000HClient>.Instance);

        // Act
        var result = await client.StopDecelerateAsync();

        // Assert
        Assert.True(result.IsSuccess);
        var controlWord = await transport.ReadRegisterAsync(RemaRegisters.ControlWord);
        Assert.Equal(RemaScaling.ControlCmd_Decelerate, controlWord);
    }

    [Theory]
    [InlineData(10.0, 1000)]   // 10 Hz = 寄存器值 1000
    [InlineData(15.0, 1500)]   // 15 Hz = 寄存器值 1500
    [InlineData(20.0, 2000)]   // 20 Hz = 寄存器值 2000
    [InlineData(5.0, 500)]     // 5 Hz = 寄存器值 500
    public async Task SetTargetFrequencyAsync_ConvertsCorrectly(decimal hz, int expectedRegisterValue)
    {
        // Arrange
        var transport = new StubRemaLm1000HTransport(NullLogger<StubRemaLm1000HTransport>.Instance);
        var modbusClient = new ModbusClientAdapter(transport, NullLogger<ModbusClientAdapter>.Instance);
        var client = new RemaLm1000HClient(
            modbusClient,
            Options.Create(_connectionOptions),
            Options.Create(_driveOptions),
            NullLogger<RemaLm1000HClient>.Instance);

        // Act
        var result = await client.SetTargetFrequencyAsync(hz);

        // Assert
        Assert.True(result.IsSuccess);
        var registerValue = await transport.ReadRegisterAsync(RemaRegisters.P0_07_LimitFrequency);
        Assert.Equal(expectedRegisterValue, registerValue);
    }

    [Theory]
    [InlineData(1000, 10.0)]   // 1000 mm/s = 10 Hz
    [InlineData(1500, 15.0)]   // 1500 mm/s = 15 Hz
    [InlineData(2000, 20.0)]   // 2000 mm/s = 20 Hz
    public async Task SetTargetSpeedAsync_ConvertsCorrectly(decimal mmps, decimal expectedHz)
    {
        // Arrange
        var transport = new StubRemaLm1000HTransport(NullLogger<StubRemaLm1000HTransport>.Instance);
        var modbusClient = new ModbusClientAdapter(transport, NullLogger<ModbusClientAdapter>.Instance);
        var client = new RemaLm1000HClient(
            modbusClient,
            Options.Create(_connectionOptions),
            Options.Create(_driveOptions),
            NullLogger<RemaLm1000HClient>.Instance);

        // Act
        var result = await client.SetTargetSpeedAsync(mmps);

        // Assert
        Assert.True(result.IsSuccess);
        var registerValue = await transport.ReadRegisterAsync(RemaRegisters.P0_07_LimitFrequency);
        var actualHz = registerValue * RemaScaling.P005_HzPerCount;
        Assert.Equal(expectedHz, actualHz);
    }

    [Fact]
    public async Task ReadCurrentFrequencyAsync_ReturnsCorrectValue()
    {
        // Arrange
        var transport = new StubRemaLm1000HTransport(NullLogger<StubRemaLm1000HTransport>.Instance);
        var modbusClient = new ModbusClientAdapter(transport, NullLogger<ModbusClientAdapter>.Instance);
        var client = new RemaLm1000HClient(
            modbusClient,
            Options.Create(_connectionOptions),
            Options.Create(_driveOptions),
            NullLogger<RemaLm1000HClient>.Instance);

        // 设置模拟的编码器反馈频率：1500 = 15.00 Hz
        await transport.WriteRegisterAsync(RemaRegisters.C0_26_EncoderFrequency, 1500);

        // Act
        var result = await client.ReadCurrentFrequencyAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(15.0m, result.Value);
    }

    [Fact]
    public async Task ReadCurrentSpeedAsync_ReturnsCorrectValue()
    {
        // Arrange
        var transport = new StubRemaLm1000HTransport(NullLogger<StubRemaLm1000HTransport>.Instance);
        var modbusClient = new ModbusClientAdapter(transport, NullLogger<ModbusClientAdapter>.Instance);
        var client = new RemaLm1000HClient(
            modbusClient,
            Options.Create(_connectionOptions),
            Options.Create(_driveOptions),
            NullLogger<RemaLm1000HClient>.Instance);

        // 设置模拟的编码器反馈频率：1500 = 15.00 Hz = 1500 mm/s
        await transport.WriteRegisterAsync(RemaRegisters.C0_26_EncoderFrequency, 1500);

        // Act
        var result = await client.ReadCurrentSpeedAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1500m, result.Value);
    }

    [Theory]
    [InlineData(800)]
    [InlineData(1000)]
    [InlineData(1200)]  // 应该被限制到 1000
    public async Task SetTorqueLimitAsync_ClampsToMaxValue(int torqueLimit)
    {
        // Arrange
        var transport = new StubRemaLm1000HTransport(NullLogger<StubRemaLm1000HTransport>.Instance);
        var modbusClient = new ModbusClientAdapter(transport, NullLogger<ModbusClientAdapter>.Instance);
        var client = new RemaLm1000HClient(
            modbusClient,
            Options.Create(_connectionOptions),
            Options.Create(_driveOptions),
            NullLogger<RemaLm1000HClient>.Instance);

        // Act
        var result = await client.SetTorqueLimitAsync(torqueLimit);

        // Assert
        Assert.True(result.IsSuccess);
        var registerValue = await transport.ReadRegisterAsync(RemaRegisters.P3_10_TorqueRef);
        Assert.InRange(registerValue, 0, RemaScaling.TorqueMaxAbsolute);
    }

    [Fact]
    public async Task InitializeAsync_SetsAllRequiredParameters()
    {
        // Arrange
        var transport = new StubRemaLm1000HTransport(NullLogger<StubRemaLm1000HTransport>.Instance);
        var modbusClient = new ModbusClientAdapter(transport, NullLogger<ModbusClientAdapter>.Instance);
        var client = new RemaLm1000HClient(
            modbusClient,
            Options.Create(_connectionOptions),
            Options.Create(_driveOptions),
            NullLogger<RemaLm1000HClient>.Instance);

        // Act
        var result = await client.InitializeAsync();

        // Assert
        Assert.True(result.IsSuccess);

        // 验证 P0.01 - 运行命令源选择 (RS485 通讯 = 2)
        var runCmdSource = await transport.ReadRegisterAsync(RemaRegisters.P0_01_RunCmdSource);
        Assert.Equal(2, runCmdSource);

        // 验证 P0.07 - 限速频率
        var limitFreq = await transport.ReadRegisterAsync(RemaRegisters.P0_07_LimitFrequency);
        var expectedLimitRegister = (ushort)Math.Round(_driveOptions.LimitHz / RemaScaling.P005_HzPerCount);
        Assert.Equal(expectedLimitRegister, limitFreq);

        // 验证 P3.10 - 转矩给定值
        var torqueRef = await transport.ReadRegisterAsync(RemaRegisters.P3_10_TorqueRef);
        Assert.Equal(_driveOptions.TorqueMax, torqueRef);
    }

    [Fact]
    public async Task ReadOutputCurrentAsync_ReturnsCorrectValue()
    {
        // Arrange
        var transport = new StubRemaLm1000HTransport(NullLogger<StubRemaLm1000HTransport>.Instance);
        var modbusClient = new ModbusClientAdapter(transport, NullLogger<ModbusClientAdapter>.Instance);
        var client = new RemaLm1000HClient(
            modbusClient,
            Options.Create(_connectionOptions),
            Options.Create(_driveOptions),
            NullLogger<RemaLm1000HClient>.Instance);

        // 设置模拟的输出电流：600 = 6.00 A
        await transport.WriteRegisterAsync(RemaRegisters.C0_01_OutputCurrent, 600);

        // Act
        var result = await client.ReadOutputCurrentAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(6.0m, result.Value);
    }

    [Fact]
    public async Task ReadRunStatusAsync_ReturnsCorrectValue()
    {
        // Arrange
        var transport = new StubRemaLm1000HTransport(NullLogger<StubRemaLm1000HTransport>.Instance);
        var modbusClient = new ModbusClientAdapter(transport, NullLogger<ModbusClientAdapter>.Instance);
        var client = new RemaLm1000HClient(
            modbusClient,
            Options.Create(_connectionOptions),
            Options.Create(_driveOptions),
            NullLogger<RemaLm1000HClient>.Instance);

        // 设置模拟的运行状态：1 = 正转
        await transport.WriteRegisterAsync(RemaRegisters.C0_32_RunStatus, (ushort)RemaScaling.RunStatus_Forward);

        // Act
        var result = await client.ReadRunStatusAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(RemaScaling.RunStatus_Forward, result.Value);
    }
}
