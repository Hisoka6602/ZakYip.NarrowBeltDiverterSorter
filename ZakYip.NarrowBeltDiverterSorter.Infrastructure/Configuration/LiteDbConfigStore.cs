using LiteDB;
using Microsoft.Extensions.Logging;

namespace ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;

/// <summary>
/// 基于 LiteDB 的配置存储实现
/// </summary>
public class LiteDbConfigStore : IConfigStore, IDisposable
{
    private const string DatabaseFileName = "narrowbelt.config.db";
    private const string CollectionName = "Configs";
    private readonly ILogger<LiteDbConfigStore> _logger;
    private readonly LiteDatabase _database;
    private bool _disposed;

    /// <summary>
    /// 初始化 LiteDB 配置存储
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public LiteDbConfigStore(ILogger<LiteDbConfigStore> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        try
        {
            var connectionString = $"Filename={DatabaseFileName};Connection=shared";
            _database = new LiteDatabase(connectionString);
            _logger.LogInformation("已初始化配置数据库: {DatabaseFile}", DatabaseFileName);
        }
        catch (Exception ex)
        {
            var message = $"初始化配置数据库失败: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ConfigurationAccessException(message, ex);
        }
    }

    /// <summary>
    /// 异步加载配置
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
                var collection = _database.GetCollection<ConfigDocument>(CollectionName);
                var doc = collection.FindById(key);

                if (doc == null)
                {
                    _logger.LogDebug("配置键 {Key} 不存在", key);
                    return null;
                }

                var result = BsonMapper.Global.ToObject<T>(doc.Data);
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
    /// 异步保存配置
    /// </summary>
    public async Task SaveAsync<T>(string key, T options, CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("配置键不能为空", nameof(key));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options), "配置对象不能为 null");
        }

        await Task.Run(() =>
        {
            try
            {
                var collection = _database.GetCollection<ConfigDocument>(CollectionName);
                var bsonData = BsonMapper.Global.ToDocument(options);
                
                var doc = new ConfigDocument
                {
                    Key = key,
                    Data = bsonData
                };

                collection.Upsert(doc);
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
                var collection = _database.GetCollection<ConfigDocument>(CollectionName);
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
    /// 配置文档结构
    /// </summary>
    private class ConfigDocument
    {
        [BsonId]
        public string Key { get; set; } = string.Empty;
        
        public BsonDocument Data { get; set; } = new();
    }
}
