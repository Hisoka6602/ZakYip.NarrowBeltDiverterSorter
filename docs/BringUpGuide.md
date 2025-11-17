# Bring-up 模式指南

本指南介绍如何使用不同的 Bring-up 模式来逐步调试和验证窄带分流分拣系统的各个子系统。

## 概述

Bring-up 模式允许您在系统集成的不同阶段，选择性地启动特定的后台服务和工作器，以便逐步验证硬件和软件功能。每个模式都对应一个特定的接线和调试阶段。

## 启动模式

### 1. normal - 正常模式

**用途**：全功能生产模式，所有 Worker 启动。

**适用阶段**：系统完全集成并验证后的正常运行阶段。

**启动的服务**：
- 主线控制工作器 (MainLineControlWorker)
- 原点传感器监控 (OriginSensorMonitor)
- 入口传感器监控 (InfeedSensorMonitor)
- 包裹装载协调器 (ParcelLoadCoordinator)
- 分拣执行工作器 (SortingExecutionWorker)
- 格口IO监视器 (ChuteIoMonitor)
- 包裹路由工作器 (ParcelRoutingWorker) - 与上游系统通信

**启动命令**：
```bash
dotnet run --project ZakYip.NarrowBeltDiverterSorter.Host --mode normal
# 或者不指定 mode（默认为 normal）
dotnet run --project ZakYip.NarrowBeltDiverterSorter.Host
```

**预期现象**：
- 系统日志显示所有 Worker 已启动
- 主线控制每5秒输出一次运行状态
- 入口传感器触发时，包裹自动分配格口并装载到小车
- 分拣执行工作器根据计划执行吐件动作
- 所有分拣结果上报到上游系统

---

### 2. bringup-mainline - 主线调试模式

**用途**：只启动主驱控制和原点监控，用于验证主线电机和原点传感器。

**适用阶段**：
- 主线驱动器（变频器）已接线
- 原点传感器已接线
- 小车环已安装并可运行

**启动的服务**：
- 主线控制工作器 (MainLineControlWorker)
- 原点传感器监控 (OriginSensorMonitor)

**启动命令**：
```bash
dotnet run --project ZakYip.NarrowBeltDiverterSorter.Host --mode bringup-mainline
```

**预期现象**：

1. **主线控制日志**（每秒输出一次）：
   ```
   [主线状态] 目标速度: 500.0 mm/s, 实际速度: 498.5 mm/s, 速度稳定: 是
   ```
   - `目标速度`：来自配置的期望速度
   - `实际速度`：从驱动器反馈的当前速度
   - `速度稳定`：速度是否在允许的误差范围内

2. **原点传感器日志**（每秒输出一次）：
   ```
   [原点状态] 小车环已构建: 是, 环长度: 120, ZeroCartId: 0
   ```
   - `小车环已构建`：是否完成小车环的构建（需要零号小车经过两次原点）
   - `环长度`：小车环中的小车总数
   - `ZeroCartId`：零号小车的 ID（通常为 0）

**调试步骤**：
1. 启动系统，观察主线是否启动运行
2. 检查实际速度是否接近目标速度
3. 观察原点传感器日志，等待零号小车经过两次原点
4. 确认小车环已构建且环长度正确

---

### 3. bringup-infeed - 入口调试模式

**用途**：在 mainline 基础上增加入口相关功能，验证入口传感器和包裹装载逻辑。

**适用阶段**：
- 主线和原点传感器已验证（完成 bringup-mainline）
- 入口传感器已接线
- 入口输送机已接线（如果有）

**启动的服务**：
- 主线控制工作器 (MainLineControlWorker)
- 原点传感器监控 (OriginSensorMonitor)
- 入口传感器监控 (InfeedSensorMonitor) ✨ 新增
- 包裹装载协调器 (ParcelLoadCoordinator) ✨ 新增

**启动命令**：
```bash
dotnet run --project ZakYip.NarrowBeltDiverterSorter.Host --mode bringup-infeed
```

**预期现象**：

1. **继承 bringup-mainline 的所有日志**

2. **入口触发日志**（当入口传感器触发时）：
   ```
   入口触发 ParcelId=P20241116001, 预计落车 CartId=45
   ```
   - `ParcelId`：新创建的包裹 ID
   - `预计落车 CartId`：根据触发时间计算出的目标小车 ID

**调试步骤**：
1. 启动系统，确认主线和原点传感器工作正常
2. 在入口放入测试件，触发入口传感器
3. 观察日志，确认包裹 ID 被创建
4. 验证预计落车的小车 ID 是否合理（根据当前小车位置和触发时间）

**注意事项**：
- 此模式下不会调用上游系统分配格口
- 包裹不会实际执行吐件动作
- 主要用于验证入口传感器和装载计算逻辑

---

### 4. bringup-chutes - 吐件调试模式

**用途**：在 infeed 基础上增加吐件相关功能，但可以关闭上游通信，独立验证吐件执行。

**适用阶段**：
- 入口相关已验证（完成 bringup-infeed）
- 格口发信器已接线
- 格口 IO 监视器已接线（如果有）

**启动的服务**：
- 主线控制工作器 (MainLineControlWorker)
- 原点传感器监控 (OriginSensorMonitor)
- 入口传感器监控 (InfeedSensorMonitor)
- 包裹装载协调器 (ParcelLoadCoordinator)
- 分拣执行工作器 (SortingExecutionWorker) ✨ 新增
- 格口IO监视器 (ChuteIoMonitor) ✨ 新增

**不启动的服务**：
- 包裹路由工作器 (ParcelRoutingWorker) - 上游通信关闭

**启动命令**：
```bash
dotnet run --project ZakYip.NarrowBeltDiverterSorter.Host --mode bringup-chutes
```

**预期现象**：

1. **继承 bringup-infeed 的所有日志**

2. **吐件计划执行日志**（执行吐件动作时）：
   ```
   [吐件计划执行] ParcelId=P20241116001, CartId=45, 目标格口=10, 当前格口=8, 是否强排=否
   ```
   - `ParcelId`：要吐出的包裹 ID
   - `CartId`：承载包裹的小车 ID
   - `目标格口`：包裹的目标格口编号
   - `当前格口`：小车当前经过的格口编号
   - `是否强排`：是否为强制排出（包裹未能及时路由或路由失败）

**调试步骤**：
1. 启动系统，确认入口相关功能正常
2. 在入口放入测试件
3. 手动为包裹分配格口（通过配置或数据库）
4. 观察吐件计划执行日志
5. 验证格口发信器是否在正确的时间打开

**注意事项**：
- 此模式下不会调用上游系统进行路由
- 需要手动为包裹分配格口 ID，否则包裹会被强制排出
- 主要用于验证吐件执行逻辑和格口硬件

---

## 配置要求

所有 Bring-up 模式都依赖 LiteDB 配置仓储来加载配置选项，而不是硬编码。确保以下配置已正确设置：

### 必需的配置项

1. **MainLineControlOptions**（主线控制选项）
   - 存储位置：LiteDB 数据库
   - 加载方式：`IMainLineOptionsRepository.LoadAsync()`
   - 关键参数：目标速度、控制周期、稳定性阈值等

2. **InfeedLayoutOptions**（入口布局选项）
   - 存储位置：LiteDB 数据库
   - 加载方式：`IInfeedLayoutOptionsRepository.LoadAsync()`
   - 关键参数：入口位置、触发延迟等

3. **ChuteConfig**（格口配置）
   - 存储位置：LiteDB 数据库
   - 加载方式：`IChuteConfigRepository.LoadAsync()`
   - 关键参数：格口位置、开窗时长等

4. **其他配置**
   - CartParameterRegisterConfiguration（小车参数寄存器）
   - ChuteMappingConfiguration（格口映射）
   - FieldBusClientConfiguration（现场总线客户端）

### 配置文件示例

参考 `appsettings.json` 和 `appsettings.Development.json` 中的配置示例。

---

## Rema LM1000H 实机调试

### 切换到 Rema 模式

如果使用 Rema LM1000H 变频驱动器进行实机调试，需要在 `appsettings.json` 中切换主线驱动实现：

```json
{
  "Sorter": {
    "MainLine": {
      "Mode": "RemaLm1000H",  // 切换为 Rema 驱动
      "Rema": {
        "PortName": "COM3",              // 修改为实际串口号
        "BaudRate": 38400,               // 与变频器设置一致
        "SlaveAddress": 1,               // 与变频器站号一致
        "ReadTimeout": "00:00:01.200",
        "WriteTimeout": "00:00:01.200"
      }
    }
  }
}
```

### Rema Bring-up 模式特性

在 Bring-up 模式下使用 Rema 驱动时，系统会额外输出以下诊断信息：

1. **串口配置和站号**
   ```
   [Rema 连接] 串口: COM3, 波特率: 38400, 站号: 1
   ```

2. **最近一次成功下发的目标速度**
   ```
   [Rema 命令] 最后成功下发速度: 1000.0 mm/s (2.3秒前)
   ```

3. **C0.26 反馈频率和换算后的线速度**
   ```
   [Rema 反馈] C0.26 寄存器值: 1654, 反馈频率: 16.54 Hz, 换算线速: 998.5 mm/s
   ```

这些信息可用于：
- 排查串口通讯问题
- 验证命令下发是否成功
- 检查编码器反馈是否正常
- 调试速度换算系数

### Rema 实机调试详细指南

详细的 Rema LM1000H 实机调试步骤请参考：[RemaLm1000HBringUpGuide.md](RemaLm1000HBringUpGuide.md)

包括：
- 变频器参数配置
- 串口通讯验证
- 速度标定步骤
- 常见问题排查
- PID 参数整定

---

## 常见问题

### Q: 如何切换启动模式？

A: 使用 `--mode` 参数指定启动模式：
```bash
dotnet run --project ZakYip.NarrowBeltDiverterSorter.Host --mode bringup-mainline
```

### Q: 为什么某些服务显示 "[待实现]"？

A: 这些服务依赖的硬件端口接口（如 `IOriginSensorPort`、`IInfeedSensorPort`）尚未完全实现。它们已在 Program.cs 中注释掉，等待硬件接口实现后启用。

### Q: 如何修改日志输出频率？

A: 在 Bring-up 模式下，日志输出频率固定为每秒一次。如需修改，可以调整各 Worker 中的日志输出逻辑。

### Q: 如何验证配置是否正确加载？

A: 启动系统后，检查日志中是否有配置加载相关的错误信息。如果配置加载失败，系统会记录错误日志并可能无法正常启动。

---

## 调试技巧

1. **逐步验证**：按照 mainline → infeed → chutes 的顺序逐步验证各子系统。

2. **日志分析**：
   - 使用 `grep` 过滤关键日志：
     ```bash
     dotnet run --mode bringup-mainline | grep "主线状态"
     ```
   - 检查时间戳，验证日志输出频率是否正常

3. **配置调整**：
   - 如果主线速度不稳定，调整 MainLineControlOptions 中的 PID 参数
   - 如果入口触发不准确，调整 InfeedLayoutOptions 中的触发延迟

4. **硬件检查**：
   - 使用万用表验证传感器信号
   - 使用示波器检查信号波形
   - 确认现场总线通信正常

---

## 下一步

完成所有 Bring-up 模式的验证后，即可切换到 `normal` 模式，进行完整的系统集成测试和生产运行。

如有问题，请参考系统日志或联系技术支持。
