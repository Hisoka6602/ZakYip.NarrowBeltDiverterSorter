using Microsoft.Extensions.Options;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;

namespace ZakYip.NarrowBeltDiverterSorter.Host;

/// <summary>
/// 主线控制工作器
/// 负责周期性调用控制服务并输出状态日志
/// </summary>
public class MainLineControlWorker : BackgroundService
{
    private readonly ILogger<MainLineControlWorker> _logger;
    private readonly IMainLineControlService _controlService;
    private readonly IMainLineSpeedProvider _speedProvider;
    private readonly MainLineControlOptions _options;

    public MainLineControlWorker(
        ILogger<MainLineControlWorker> logger,
        IMainLineControlService controlService,
        IMainLineSpeedProvider speedProvider,
        IOptions<MainLineControlOptions> options)
    {
        _logger = logger;
        _controlService = controlService;
        _speedProvider = speedProvider;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("主线控制工作器已启动");

        // 启动控制服务
        var started = await _controlService.StartAsync(stoppingToken);
        if (!started)
        {
            _logger.LogError("主线控制服务启动失败");
            return;
        }

        var loopPeriod = _options.LoopPeriod;
        var logCounter = 0;
        var logInterval = (int)(TimeSpan.FromSeconds(5).TotalMilliseconds / loopPeriod.TotalMilliseconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 执行控制循环
                var success = await _controlService.ExecuteControlLoopAsync(stoppingToken);
                
                if (!success && _controlService.IsRunning)
                {
                    _logger.LogWarning("控制循环执行失败");
                }

                // 定期输出状态日志（每5秒）
                logCounter++;
                if (logCounter >= logInterval)
                {
                    logCounter = 0;
                    LogCurrentStatus();
                }

                // 等待下一个控制周期
                await Task.Delay(loopPeriod, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // 正常停止
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "主线控制循环发生异常");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        // 停止控制服务
        await _controlService.StopAsync(CancellationToken.None);
        _logger.LogInformation("主线控制工作器已停止");
    }

    /// <summary>
    /// 输出当前状态日志（中文）
    /// </summary>
    private void LogCurrentStatus()
    {
        var currentSpeed = _speedProvider.CurrentMmps;
        var targetSpeed = _controlService.GetTargetSpeed();
        var isStable = _speedProvider.IsSpeedStable;
        var stableDuration = _speedProvider.StableDuration;

        if (isStable)
        {
            _logger.LogInformation(
                "主线运行状态 - 当前速度: {CurrentSpeed:F1} mm/s, 目标速度: {TargetSpeed:F1} mm/s, 速度稳定: 是 (已稳定 {StableDuration:F1}秒)",
                currentSpeed, targetSpeed, stableDuration.TotalSeconds);
        }
        else
        {
            _logger.LogInformation(
                "主线运行状态 - 当前速度: {CurrentSpeed:F1} mm/s, 目标速度: {TargetSpeed:F1} mm/s, 速度稳定: 否",
                currentSpeed, targetSpeed);
        }
    }
}
