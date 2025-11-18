// This interface has been moved to ZakYip.NarrowBeltDiverterSorter.Core.Abstractions
// Keeping this file for backward compatibility - it now references the Core interface

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Panel;

/// <summary>
/// 面板 IO 协调器接口（已迁移到 Core.Abstractions）
/// </summary>
[Obsolete("请使用 ZakYip.NarrowBeltDiverterSorter.Core.Abstractions.IPanelIoCoordinator")]
public interface IPanelIoCoordinator : ZakYip.NarrowBeltDiverterSorter.Core.Abstractions.IPanelIoCoordinator
{
}
