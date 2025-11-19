namespace ZakYip.NarrowBeltDiverterSorter.Core.Abstractions.Drivers;

/// <summary>
/// Drivers 抽象层 README
/// 
/// 本目录定义了统一的驱动器抽象层，为 Execution 层提供硬件无关的接口。
/// 
/// 设计原则：
/// 1. Execution 层仅依赖这些抽象接口，不直接依赖具体厂商实现
/// 2. 每个接口代表一类物理设备的控制抽象
/// 3. 具体实现可以是真实硬件驱动、仿真驱动或测试桩
/// 
/// 核心接口：
/// - IMainLineDrive: 主线驱动接口（已在 Core/Abstractions 根目录定义）
/// - ICartDrive: 小车驱动接口
/// - ICartParameterPort: 小车参数配置接口（已在 Core/Abstractions 根目录定义）
/// - IChuteIoService: 格口 IO 服务接口（已在 Core/Abstractions 根目录定义）
/// - IIoDevice: 通用 IO 设备接口
/// 
/// 实现示例：
/// - 仿真实现：Execution/Vendors/Simulated/SimulatedMainLineDrive
/// - 真实设备：Execution/Vendors/Rema/RemaLm1000HMainLineDrive
/// 
/// 扩展新厂商设备：
/// 1. 在 Execution/Vendors/{VendorName} 下创建新实现
/// 2. 实现对应的驱动接口
/// 3. 在 Host/Program.cs 中根据配置注册相应实现
/// 