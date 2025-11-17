using LiteDB;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;

namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.LiteDb;

/// <summary>
/// 基于 LiteDB 的分拣机配置存储实现
/// 此实现仅用于存储系统配置对象，不用于日志、事件或统计数据
/// </summary>
public sealed class LiteDbSorterConfigurationStore : ISorterConfigurationStore, IDisposable
{
    private const string DatabaseFileName = "narrowbelt.config.db";
    private const string CollectionName = "config_entries";
    private readonly ILogger<LiteDbSorterConfigurationStore> _logger;
    private readonly LiteDatabase _database;
    private bool _disposed;

    /// <summary>
    /// 初始化 LiteDB 配置存储
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public LiteDbSorterConfigurationStore(ILogger<LiteDbSorterConfigurationStore> logger)
        : this(logger, DatabaseFileName)
    {
    }

    /// <summary>
    /// 初始化 LiteDB 配置存储（测试友好）
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="databaseFileName">数据库文件名</param>
    public LiteDbSorterConfigurationStore(ILogger<LiteDbSorterConfigurationStore> logger, string databaseFileName)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        try
        {
            var connectionString = $"Filename={databaseFileName};Connection=shared";
            _database = new LiteDatabase(connectionString);
            _logger.LogInformation("已初始化配置数据库: {DatabaseFile}", databaseFileName);
        }
        catch (Exception ex)
        {
            var message = $"初始化配置数据库失败: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ConfigurationAccessException(message, ex);
        }
    }

    /// <summary>
    /// 异步加载配置对象
    /// </summary>
    public async Task<T?> LoadAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("配置键不能为空", nameof(key));
        }

        return await Task.Run(() =>
        {
            try
            {
                var collection = _database.GetCollection<ConfigEntry>(CollectionName);
                var entry = collection.FindById(key);

                if (entry == null)
                {
                    _logger.LogDebug("配置键 {Key} 不存在", key);
                    return null;
                }

                var result = BsonMapper.Global.ToObject<T>(entry.Data);
                _logger.LogDebug("已加载配置键 {Key}", key);
                return result;
            }
            catch (Exception ex)
            {
                var message = $"加载配置失败，键: {key}, 错误: {ex.Message}";
                _logger.LogError(ex, message);
                throw new ConfigurationAccessException(message, ex);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// 异步保存配置对象
    /// </summary>
    public async Task SaveAsync<T>(string key, T value, CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("配置键不能为空", nameof(key));
        }

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value), "配置对象不能为 null");
        }

        await Task.Run(() =>
        {
            try
            {
                var collection = _database.GetCollection<ConfigEntry>(CollectionName);
                var bsonData = BsonMapper.Global.ToDocument(value);
                
                var entry = new ConfigEntry
                {
                    Key = key,
                    Data = bsonData
                };

                collection.Upsert(entry);
                _logger.LogDebug("已保存配置键 {Key}", key);
            }
            catch (Exception ex)
            {
                var message = $"保存配置失败，键: {key}, 错误: {ex.Message}";
                _logger.LogError(ex, message);
                throw new ConfigurationAccessException(message, ex);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// 检查配置是否存在
    /// </summary>
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("配置键不能为空", nameof(key));
        }

        return await Task.Run(() =>
        {
            try
            {
                var collection = _database.GetCollection<ConfigEntry>(CollectionName);
                var exists = collection.Exists(Query.EQ("_id", key));
                _logger.LogDebug("配置键 {Key} 存在性检查结果: {Exists}", key, exists);
                return exists;
            }
            catch (Exception ex)
            {
                var message = $"检查配置存在性失败，键: {key}, 错误: {ex.Message}";
                _logger.LogError(ex, message);
                throw new ConfigurationAccessException(message, ex);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _database?.Dispose();
        _disposed = true;
        _logger.LogInformation("已释放配置数据库资源");
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 配置条目结构
    /// </summary>
    private class ConfigEntry
    {
        [BsonId]
        public string Key { get; set; } = string.Empty;
        
        public BsonDocument Data { get; set; } = new();
    }
}
