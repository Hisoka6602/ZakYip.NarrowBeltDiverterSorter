using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Infrastructure.LiteDb;
using ZakYip.NarrowBeltDiverterSorter.Core.Configuration;
using ZakYip.NarrowBeltDiverterSorter.Core.Abstractions;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Carts;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Execution.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Execution.Mainline;
using ZakYip.NarrowBeltDiverterSorter.Execution.Feeding;
using ZakYip.NarrowBeltDiverterSorter.Core.Application;
using ZakYip.NarrowBeltDiverterSorter.Host.Controllers.Configuration;

namespace ZakYip.NarrowBeltDiverterSorter.E2ETests;

/// <summary>
/// 依赖注入验证测试
/// 确保所有已注册的服务都能正确构造，依赖关系完整
/// </summary>
public class DependencyInjectionValidationTests
{
    /// <summary>
    /// 测试核心服务的依赖注入配置
    /// 验证所有已注册的核心服务（如 ICartAtChuteResolver）能够成功构造
    /// </summary>
    [Fact]
    public void CoreServices_DependencyInjection_Should_BeValid()
    {
        // Arrange - 创建最小化的服务容器，仅注册核心服务及其依赖
        var services = new ServiceCollection();
        
        // 注册日志
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        // 注册配置存储
        var dbPath = Path.Combine(Path.GetTempPath(), $"di-test-{Guid.NewGuid()}.db");
        services.AddSingleton<ISorterConfigurationStore>(sp =>
            new LiteDbSorterConfigurationStore(sp.GetRequiredService<ILogger<LiteDbSorterConfigurationStore>>(), dbPath));
        
        // 注册小车环配置提供器（这是ICartAtChuteResolver的依赖）
        services.AddSingleton<ICartRingConfigurationProvider, CartRingConfigurationProvider>();
        
        // 注册格口配置提供器
        services.AddSingleton<IChuteConfigProvider>(sp => new ChuteConfigProvider());
        
        // 注册小车位置跟踪器（ICartAtChuteResolver的依赖）
        services.AddSingleton<ICartPositionTracker, CartPositionTracker>();
        
        // 注册格口小车号计算器（ICartAtChuteResolver的依赖）
        services.AddSingleton<IChuteCartNumberCalculator, ChuteCartNumberCalculator>();
        
        // 注册被测试的服务：ICartAtChuteResolver
        services.AddSingleton<ICartAtChuteResolver, CartAtChuteResolver>();
        
        // Act & Assert - 构建服务提供者并验证服务能够成功解析
        using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true // 关键：启用构建时验证，会检测所有依赖是否可解析
        });
        
        // 验证：尝试解析ICartAtChuteResolver，如果依赖缺失会抛出异常
        var resolver = serviceProvider.GetRequiredService<ICartAtChuteResolver>();
        Assert.NotNull(resolver);
        
        // 清理
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }

    /// <summary>
    /// 测试 ICartRingConfigurationProvider 能够被正确构造
    /// 这是之前遗漏的依赖注册，应该确保它能独立构造
    /// </summary>
    [Fact]
    public void CartRingConfigurationProvider_Should_BeConstructable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        var dbPath = Path.Combine(Path.GetTempPath(), $"di-test-{Guid.NewGuid()}.db");
        services.AddSingleton<ISorterConfigurationStore>(sp =>
            new LiteDbSorterConfigurationStore(sp.GetRequiredService<ILogger<LiteDbSorterConfigurationStore>>(), dbPath));
        
        // 注册被测试的服务
        services.AddSingleton<ICartRingConfigurationProvider, CartRingConfigurationProvider>();
        
        // Act & Assert
        using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true
        });
        
        var provider = serviceProvider.GetRequiredService<ICartRingConfigurationProvider>();
        Assert.NotNull(provider);
        
        // 验证provider可以返回配置（默认为0表示自动学习模式）
        var config = provider.Current;
        Assert.NotNull(config);
        Assert.True(config.TotalCartCount >= 0); // 0表示自动学习模式，>0表示已配置
        
        // 清理
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }

    /// <summary>
    /// 测试缺少依赖时应该抛出清晰的异常
    /// 这个测试验证了DI验证机制是否有效
    /// </summary>
    [Fact]
    public void MissingDependency_Should_ThrowDescriptiveException()
    {
        // Arrange - 故意不注册ICartRingConfigurationProvider
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        // 注册其他依赖，但故意遗漏ICartRingConfigurationProvider
        services.AddSingleton<IChuteConfigProvider>(sp => new ChuteConfigProvider());
        services.AddSingleton<ICartPositionTracker, CartPositionTracker>();
        services.AddSingleton<IChuteCartNumberCalculator, ChuteCartNumberCalculator>();
        
        // 注册需要ICartRingConfigurationProvider的服务
        services.AddSingleton<ICartAtChuteResolver, CartAtChuteResolver>();
        
        // Act & Assert - 验证会抛出异常，并且异常消息包含缺失的依赖类型
        var exception = Assert.Throws<AggregateException>(() =>
        {
            using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true
            });
        });
        
        // 验证异常消息提到了ICartRingConfigurationProvider
        Assert.Contains("ICartRingConfigurationProvider", exception.InnerException?.Message ?? exception.Message);
    }

    /// <summary>
    /// 测试 ChuteIoConfigurationController 能够被正确构造
    /// 验证修复后的控制器依赖注入配置正确
    /// </summary>
    [Fact]
    public void ChuteIoConfigurationController_Should_BeConstructable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        var dbPath = Path.Combine(Path.GetTempPath(), $"di-test-{Guid.NewGuid()}.db");
        
        // 直接注册 IChuteTransmitterConfigurationPort，避免依赖具体实现
        services.AddSingleton<IChuteTransmitterConfigurationPort>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<LiteDbSorterConfigurationStore>>();
            return new LiteDbSorterConfigurationStore(logger, dbPath);
        });
        
        // 注册控制器
        services.AddScoped<ChuteIoConfigurationController>();
        
        // Act & Assert
        using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true
        });
        
        var controller = serviceProvider.GetRequiredService<ChuteIoConfigurationController>();
        Assert.NotNull(controller);
        
        // 清理
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }

    /// <summary>
    /// 测试 UpstreamRoutingSettingsController 能够被正确构造
    /// 验证修复后的控制器不再需要具体类型转换
    /// </summary>
    [Fact]
    public void UpstreamRoutingSettingsController_Should_BeConstructable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        var dbPath = Path.Combine(Path.GetTempPath(), $"di-test-{Guid.NewGuid()}.db");
        services.AddSingleton<ISorterConfigurationStore>(sp =>
            new LiteDbSorterConfigurationStore(sp.GetRequiredService<ILogger<LiteDbSorterConfigurationStore>>(), dbPath));
        
        // 注册 IUpstreamRoutingConfigProvider
        services.AddSingleton<IUpstreamRoutingConfigProvider, LiteDbUpstreamRoutingConfigProvider>();
        
        // 注册控制器
        services.AddScoped<UpstreamRoutingSettingsController>();
        
        // Act & Assert
        using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true
        });
        
        var controller = serviceProvider.GetRequiredService<UpstreamRoutingSettingsController>();
        Assert.NotNull(controller);
        
        // 清理
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }
}
