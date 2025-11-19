using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Simulation.Fakes;

namespace ZakYip.NarrowBeltDiverterSorter.Simulation;

/// <summary>
/// 安全场景仿真运行器
/// 验证启动和停止时的格口安全行为
/// </summary>
public class SafetyScenarioRunner
{
    private readonly IChuteSafetyService _chuteSafetyService;
    private readonly FakeChuteTransmitterPort _fakeChuteTransmitter;
    private readonly ILogger<SafetyScenarioRunner> _logger;

    public SafetyScenarioRunner(
        IChuteSafetyService chuteSafetyService,
        IChuteTransmitterPort chuteTransmitterPort,
        ILogger<SafetyScenarioRunner> logger)
    {
        _chuteSafetyService = chuteSafetyService ?? throw new ArgumentNullException(nameof(chuteSafetyService));
        _fakeChuteTransmitter = (chuteTransmitterPort as FakeChuteTransmitterPort) 
            ?? throw new ArgumentException("Safety scenario requires FakeChuteTransmitterPort", nameof(chuteTransmitterPort));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SafetyScenarioReport> RunAsync(int totalChutes, CancellationToken cancellationToken)
    {
        var report = new SafetyScenarioReport
        {
            TotalChutes = totalChutes
        };

        try
        {
            // Step 1: 检查启动前的状态
            _logger.LogInformation("安全场景: 检查启动前状态");
            var chutesBefore = _fakeChuteTransmitter.GetOpenChuteCount();
            report.ChutesOpenBeforeStartup = chutesBefore;

            // Step 2: 执行启动时的安全关闭
            _logger.LogInformation("安全场景: 执行启动时安全关闭");
            await _chuteSafetyService.CloseAllChutesAsync(cancellationToken);
            await Task.Delay(100, cancellationToken); // Give time for state to update

            var chutesAfterStartupClose = _fakeChuteTransmitter.GetOpenChuteCount();
            report.ChutesOpenAfterStartupClose = chutesAfterStartupClose;
            report.StartupCloseExecuted = true;

            // Step 3: 模拟运行期间的格口开合
            _logger.LogInformation("安全场景: 模拟运行期间格口操作");
            report.ChutesTriggeredDuringRun = await SimulateRuntimeChuteTriggers(totalChutes, cancellationToken);

            // Step 4: 等待一小段时间让格口自然关闭
            await Task.Delay(500, cancellationToken);

            // Step 5: 执行停止时的安全关闭
            _logger.LogInformation("安全场景: 执行停止时安全关闭");
            await _chuteSafetyService.CloseAllChutesAsync(cancellationToken);
            await Task.Delay(100, cancellationToken); // Give time for state to update

            var chutesAfterShutdownClose = _fakeChuteTransmitter.GetOpenChuteCount();
            report.ChutesOpenAfterShutdown = chutesAfterShutdownClose;
            report.ShutdownCloseExecuted = true;

            // Step 6: 验证最终状态
            report.FinalVerificationPassed = (report.ChutesOpenAfterShutdown == 0);

            _logger.LogInformation("安全场景: 运行完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "安全场景: 运行失败");
            report.ErrorMessage = ex.Message;
        }

        return report;
    }

    private async Task<int> SimulateRuntimeChuteTriggers(int totalChutes, CancellationToken cancellationToken)
    {
        // Simulate opening and closing a few chutes during runtime
        int triggeredCount = Math.Min(3, totalChutes); // Trigger 3 chutes or all if less than 3
        
        for (int i = 1; i <= triggeredCount; i++)
        {
            var chuteId = new ZakYip.NarrowBeltDiverterSorter.Core.Domain.ChuteId(i);
            await _fakeChuteTransmitter.OpenWindowAsync(chuteId, TimeSpan.FromMilliseconds(200), cancellationToken);
            _logger.LogInformation("安全场景: 触发格口 {ChuteId}", i);
            await Task.Delay(50, cancellationToken); // Small delay between triggers
        }

        return triggeredCount;
    }
}

/// <summary>
/// 安全场景仿真报告
/// </summary>
public class SafetyScenarioReport
{
    /// <summary>
    /// 总格口数
    /// </summary>
    public int TotalChutes { get; set; }

    /// <summary>
    /// 启动前检测到打开的格口数量
    /// </summary>
    public int ChutesOpenBeforeStartup { get; set; }

    /// <summary>
    /// 启动安全关闭后仍打开的格口数量
    /// </summary>
    public int ChutesOpenAfterStartupClose { get; set; }

    /// <summary>
    /// 是否执行了启动时的安全关闭
    /// </summary>
    public bool StartupCloseExecuted { get; set; }

    /// <summary>
    /// 运行期间触发的格口数量
    /// </summary>
    public int ChutesTriggeredDuringRun { get; set; }

    /// <summary>
    /// 停止后检测到打开的格口数量
    /// </summary>
    public int ChutesOpenAfterShutdown { get; set; }

    /// <summary>
    /// 是否执行了停止时的安全关闭
    /// </summary>
    public bool ShutdownCloseExecuted { get; set; }

    /// <summary>
    /// 最终验证是否通过（所有格口都关闭）
    /// </summary>
    public bool FinalVerificationPassed { get; set; }

    /// <summary>
    /// 错误信息（如果有）
    /// </summary>
    public string? ErrorMessage { get; set; }
}
