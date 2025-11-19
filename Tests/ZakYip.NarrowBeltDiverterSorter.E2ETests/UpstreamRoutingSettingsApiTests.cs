using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Host.Contracts.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Host.Controllers.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure.LiteDb;

namespace ZakYip.NarrowBeltDiverterSorter.E2ETests;

/// <summary>
/// 上游路由配置 API 端到端测试
/// 测试通过 API 更新配置并验证配置立即生效
/// </summary>
public class UpstreamRoutingSettingsApiTests : IDisposable
{
    private readonly LiteDbSorterConfigurationStore _configStore;
    private readonly LiteDbUpstreamRoutingConfigProvider _configProvider;
    private readonly UpstreamRoutingSettingsController _controller;
    private readonly string _testDbPath;

    public UpstreamRoutingSettingsApiTests()
    {
        // 为每个测试使用一个唯一的数据库文件
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_api_upstream_routing.{Guid.NewGuid()}.db");
        
        var storeLogger = NullLogger<LiteDbSorterConfigurationStore>.Instance;
        var providerLogger = NullLogger<LiteDbUpstreamRoutingConfigProvider>.Instance;
        var controllerLogger = NullLogger<UpstreamRoutingSettingsController>.Instance;
        
        // 删除测试数据库（如果存在）
        if (File.Exists(_testDbPath))
        {
            File.Delete(_testDbPath);
        }
        
        _configStore = new LiteDbSorterConfigurationStore(storeLogger, _testDbPath);
        _configProvider = new LiteDbUpstreamRoutingConfigProvider(_configStore, providerLogger);
        _controller = new UpstreamRoutingSettingsController(_configProvider, controllerLogger);
    }

    [Fact]
    public void GetSettings_Should_Return_Default_Configuration()
    {
        // Act
        var result = _controller.GetSettings();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<UpstreamRoutingSettingsDto>(okResult.Value);
        
        Assert.Equal(30, dto.UpstreamResultTtlSeconds);
        Assert.Equal(9999, dto.ErrorChuteId);
    }

    [Fact]
    public async Task UpdateSettings_Should_Update_Configuration_Successfully()
    {
        // Arrange
        var updateDto = new UpstreamRoutingSettingsDto
        {
            UpstreamResultTtlSeconds = 60,
            ErrorChuteId = 8888
        };

        // Act
        var updateResult = await _controller.UpdateSettings(updateDto, CancellationToken.None);

        // Assert - 验证更新响应
        var okResult = Assert.IsType<OkObjectResult>(updateResult);
        Assert.NotNull(okResult.Value);

        // 验证配置已更新（通过 GET 验证）
        var getResult = _controller.GetSettings();
        var getOkResult = Assert.IsType<OkObjectResult>(getResult);
        var dto = Assert.IsType<UpstreamRoutingSettingsDto>(getOkResult.Value);
        
        Assert.Equal(60, dto.UpstreamResultTtlSeconds);
        Assert.Equal(8888, dto.ErrorChuteId);
    }

    [Fact]
    public async Task UpdateSettings_Should_Persist_Configuration_To_Database()
    {
        // Arrange
        var updateDto = new UpstreamRoutingSettingsDto
        {
            UpstreamResultTtlSeconds = 90,
            ErrorChuteId = 7777
        };

        // Act
        await _controller.UpdateSettings(updateDto, CancellationToken.None);

        // 创建新的控制器实例（模拟应用重启）
        var newProvider = new LiteDbUpstreamRoutingConfigProvider(
            _configStore, 
            NullLogger<LiteDbUpstreamRoutingConfigProvider>.Instance);
        var newController = new UpstreamRoutingSettingsController(
            newProvider, 
            NullLogger<UpstreamRoutingSettingsController>.Instance);

        var getResult = newController.GetSettings();

        // Assert - 验证配置持久化
        var okResult = Assert.IsType<OkObjectResult>(getResult);
        var dto = Assert.IsType<UpstreamRoutingSettingsDto>(okResult.Value);
        
        Assert.Equal(90, dto.UpstreamResultTtlSeconds);
        Assert.Equal(7777, dto.ErrorChuteId);
    }

    [Fact]
    public async Task UpdateSettings_Should_Trigger_ConfigChanged_Event()
    {
        // Arrange
        UpstreamRoutingConfigChangedEventArgs? eventArgs = null;
        _configProvider.ConfigChanged += (sender, args) => eventArgs = args;

        var updateDto = new UpstreamRoutingSettingsDto
        {
            UpstreamResultTtlSeconds = 45,
            ErrorChuteId = 6666
        };

        // Act
        await _controller.UpdateSettings(updateDto, CancellationToken.None);

        // Assert - 验证事件触发
        Assert.NotNull(eventArgs);
        Assert.Equal(TimeSpan.FromSeconds(45), eventArgs.NewOptions.UpstreamResultTtl);
        Assert.Equal(6666, eventArgs.NewOptions.ErrorChuteId);
    }

    [Fact]
    public async Task UpdateSettings_Should_Return_BadRequest_For_Invalid_TTL()
    {
        // Arrange - TTL 超出有效范围
        var invalidDto = new UpstreamRoutingSettingsDto
        {
            UpstreamResultTtlSeconds = 0, // 无效：小于最小值 1
            ErrorChuteId = 9999
        };

        // 手动验证模型（因为控制器不会自动验证）
        _controller.ModelState.AddModelError("UpstreamResultTtlSeconds", "字段 UpstreamResultTtlSeconds 必须在 1 和 300 之间");

        // Act
        var result = await _controller.UpdateSettings(invalidDto, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateSettings_Should_Allow_Multiple_Updates_In_Sequence()
    {
        // Arrange
        var updates = new[]
        {
            new UpstreamRoutingSettingsDto { UpstreamResultTtlSeconds = 20, ErrorChuteId = 1111 },
            new UpstreamRoutingSettingsDto { UpstreamResultTtlSeconds = 40, ErrorChuteId = 2222 },
            new UpstreamRoutingSettingsDto { UpstreamResultTtlSeconds = 60, ErrorChuteId = 3333 }
        };

        // Act - 连续更新多次
        foreach (var updateDto in updates)
        {
            var result = await _controller.UpdateSettings(updateDto, CancellationToken.None);
            Assert.IsType<OkObjectResult>(result);
        }

        // Assert - 验证最后一次更新生效
        var getResult = _controller.GetSettings();
        var okResult = Assert.IsType<OkObjectResult>(getResult);
        var dto = Assert.IsType<UpstreamRoutingSettingsDto>(okResult.Value);
        
        Assert.Equal(60, dto.UpstreamResultTtlSeconds);
        Assert.Equal(3333, dto.ErrorChuteId);
    }

    [Fact]
    public async Task UpdateSettings_Should_Make_New_Parcels_Use_New_TTL()
    {
        // Arrange
        var originalOptions = _configProvider.GetCurrentOptions();
        var originalTtl = originalOptions.UpstreamResultTtl;

        var updateDto = new UpstreamRoutingSettingsDto
        {
            UpstreamResultTtlSeconds = 120,
            ErrorChuteId = 9999
        };

        // Act
        await _controller.UpdateSettings(updateDto, CancellationToken.None);
        var updatedOptions = _configProvider.GetCurrentOptions();

        // Assert - 验证新配置与旧配置不同
        Assert.NotEqual(originalTtl, updatedOptions.UpstreamResultTtl);
        Assert.Equal(TimeSpan.FromSeconds(120), updatedOptions.UpstreamResultTtl);
    }

    [Fact]
    public async Task UpdateSettings_Should_Update_ErrorChuteId_For_New_Timeout_Parcels()
    {
        // Arrange
        var updateDto = new UpstreamRoutingSettingsDto
        {
            UpstreamResultTtlSeconds = 30,
            ErrorChuteId = 5555
        };

        // Act
        await _controller.UpdateSettings(updateDto, CancellationToken.None);
        var updatedOptions = _configProvider.GetCurrentOptions();

        // Assert - 验证异常格口已更新
        Assert.Equal(5555, updatedOptions.ErrorChuteId);
    }

    public void Dispose()
    {
        _configStore?.Dispose();
        
        // 清理测试数据库
        if (File.Exists(_testDbPath))
        {
            try
            {
                File.Delete(_testDbPath);
            }
            catch
            {
                // 忽略清理错误
            }
        }
    }
}
