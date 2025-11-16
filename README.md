# ZakYip.NarrowBeltDiverterSorter

针对直线窄带分拣系统的专用项目 (Dedicated project for narrow belt diverter sorting system)

## 项目结构 (Project Structure)

本解决方案采用分层架构设计，各层职责清晰：

### 核心层 (Core Layers)

- **ZakYip.NarrowBeltDiverterSorter.Core**  
  领域模型与契约层，定义核心业务实体、接口和数据契约

- **ZakYip.NarrowBeltDiverterSorter.Execution**  
  执行逻辑层，包含主驱控制、PID 算法、小车追踪等核心执行逻辑

- **ZakYip.NarrowBeltDiverterSorter.Ingress**  
  入站处理层，负责 IO 监听、传感器数据解读和事件转发

- **ZakYip.NarrowBeltDiverterSorter.Drivers**  
  硬件驱动层，封装具体硬件设备的驱动接口

- **ZakYip.NarrowBeltDiverterSorter.Communication**  
  通信协议层，封装与上游系统/WCS 和驱动板的通信协议

- **ZakYip.NarrowBeltDiverterSorter.Observability**  
  可观测性层，提供日志、指标和追踪功能

- **ZakYip.NarrowBeltDiverterSorter.Host**  
  运行宿主层，支持 Windows 服务和控制台模式的应用程序入口

- **ZakYip.NarrowBeltDiverterSorter.Simulation**  
  仿真程序层，使用 Fake 实现替代真实硬件，模拟完整的分拣流程

### 测试项目 (Test Projects)

- **ZakYip.NarrowBeltDiverterSorter.Core.Tests**  
  Core 层单元测试

- **ZakYip.NarrowBeltDiverterSorter.Execution.Tests**  
  Execution 层单元测试

- **ZakYip.NarrowBeltDiverterSorter.Ingress.Tests**  
  Ingress 层单元测试

- **ZakYip.NarrowBeltDiverterSorter.Observability.Tests**  
  Observability 层单元测试

- **ZakYip.NarrowBeltDiverterSorter.E2ETests**  
  端到端集成测试

## 技术栈 (Technology Stack)

- .NET 8.0
- C# (with nullable reference types enabled)
- xUnit (for testing)

## 开发指南 (Development Guide)

### 构建项目 (Build)

```bash
dotnet build
```

### 运行测试 (Run Tests)

```bash
dotnet test
```

### 运行宿主程序 (Run Host)

```bash
cd ZakYip.NarrowBeltDiverterSorter.Host
dotnet run
```

### 运行仿真程序 (Run Simulation)

仿真程序提供了一个完整的模拟环境，使用 Fake 实现替代真实硬件：

```bash
cd ZakYip.NarrowBeltDiverterSorter.Simulation
dotnet run
```

仿真功能包括：
- 模拟主线驱动和反馈
- 模拟小车运动和原点传感器
- 模拟入口传感器和包裹生成
- 模拟格口发信器和分拣过程
- 中文控制台日志输出

详细设计文档请参阅 [docs/NarrowBeltDesign.md](docs/NarrowBeltDesign.md)

## 文档 (Documentation)

- [NarrowBeltDesign.md](docs/NarrowBeltDesign.md) - 窄带分拣机设计文档，包含：
  - 与 WheelDiverterSorter 的异同对比
  - 双 IO 数小车算法说明
  - 入口 IO 到落车的时间-位置换算
  - 主驱稳速与格口发信器窗口控制
  - 强排口清空策略

- [SORTING_SYSTEM.md](SORTING_SYSTEM.md) - 分拣系统实现文档