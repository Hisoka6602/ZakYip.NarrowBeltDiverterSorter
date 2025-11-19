using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ZakYip.NarrowBeltDiverterSorter.Observability.Logging;

/// <summary>
/// 日志清理服务实现，根据配置的保留天数删除过期日志文件
/// </summary>
public sealed class LogCleanupService : ILogCleanupService
{
    private readonly LogRetentionOptions _options;
    private readonly ILogger<LogCleanupService> _logger;
    private readonly Func<DateTime> _timeProvider;

    /// <summary>
    /// 创建日志清理服务实例
    /// </summary>
    /// <param name="options">日志保留配置选项</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="timeProvider">时间提供器，用于测试注入</param>
    public LogCleanupService(
        IOptions<LogRetentionOptions> options,
        ILogger<LogCleanupService> logger,
        Func<DateTime>? timeProvider = null)
    {
        _options = options.Value;
        _logger = logger;
        _timeProvider = timeProvider ?? (() => DateTime.Now);
    }

    /// <summary>
    /// 清理过期日志文件
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理的文件数量</returns>
    public async Task<int> CleanupExpiredLogsAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.LogDirectory))
        {
            _logger.LogWarning("日志目录未配置，跳过清理");
            return 0;
        }

        if (!Directory.Exists(_options.LogDirectory))
        {
            _logger.LogWarning("日志目录不存在: {LogDirectory}", _options.LogDirectory);
            return 0;
        }

        var cutoffDate = _timeProvider().AddDays(-_options.RetentionDays);
        _logger.LogInformation("开始清理早于 {CutoffDate:yyyy-MM-dd} 的日志文件", cutoffDate);

        var cleanedCount = 0;

        try
        {
            var files = Directory.GetFiles(_options.LogDirectory, "*.log", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    var fileInfo = new FileInfo(file);
                    
                    // 使用文件最后写入时间判断是否过期
                    if (fileInfo.LastWriteTime < cutoffDate)
                    {
                        await Task.Run(() => File.Delete(file), cancellationToken);
                        cleanedCount++;
                        _logger.LogDebug("已删除过期日志文件: {FileName}", fileInfo.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "删除日志文件失败: {FileName}", file);
                }
            }

            _logger.LogInformation("日志清理完成，共删除 {Count} 个文件", cleanedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理日志文件时发生错误");
        }

        return cleanedCount;
    }
}
