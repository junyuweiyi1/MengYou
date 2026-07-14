using System.IO;
using System.Windows;
using MengYou.Abstractions;
using MengYou.Abstractions.Modules;
using MengYou.Core;
using MengYou.Core.Logging;
using MengYou.Input.Simulated;
using MengYou.Modules.Combat;
using MengYou.Modules.Heal;
using MengYou.Modules.Path;
using MengYou.Modules.Path.Interaction;
using MengYou.Modules.Path.Local;
using MengYou.Modules.Path.Teleport;
using MengYou.Modules.Path.World;
using MengYou.Modules.Quest;
using MengYou.Recognition.Image;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MengYou.App;

/// <summary>
/// WPF 应用入口：负责构建 DI 容器与显示主窗口。
/// </summary>
public partial class App : Application
{
    /// <summary>全局根 DI 容器。</summary>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <inheritdoc/>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Services = BuildServices();
        var main = new MainWindow(Services);
        main.Show();
    }

    /// <summary>构建根 DI 容器。</summary>
    private static IServiceProvider BuildServices()
    {
        var baseDir = AppContext.BaseDirectory;
        var logDir = Path.Combine(baseDir, "logs");
        var templateDir = Path.Combine(baseDir, "Config", "Templates");
        var layoutFile = Path.Combine(baseDir, "Config", "UILayout.json");
        var worldFile = Path.Combine(baseDir, "Config", "WorldGraph.yaml");
        var questDir = Path.Combine(baseDir, "Config", "Quests");
        var combatDir = Path.Combine(baseDir, "Config", "Combat");

        var services = new ServiceCollection();

        // 日志：Serilog → Microsoft.Extensions.Logging
        var loggerFactory = LoggingFactory.Create(logDir);
        services.AddSingleton(loggerFactory);
        services.AddLogging(b =>
        {
            b.ClearProviders();
            b.AddSerilog(dispose: false);
        });

        // Core 基础设施
        services.AddMengYouCore();

        // 图像识别 + 模拟输入（默认后端）
        Directory.CreateDirectory(templateDir);
        Directory.CreateDirectory(questDir);
        Directory.CreateDirectory(combatDir);
        EnsureFile(layoutFile, "{\"points\":{},\"regions\":{}}");
        EnsureFile(worldFile, "maps: {}\n");
        services.AddImageRecognition(templateDir, layoutFile);
        services.AddSimulatedInput();

        // 世界图 & 寻路
        services.AddSingleton(_ => WorldGraph.LoadFromFile(worldFile));
        services.AddSingleton<PathPlanner>();
        services.AddScoped<LocalPathFinder>();
        services.AddScoped<NpcInteractor>();
        services.AddScoped<ITeleportProvider, FlyingCharmProvider>();
        services.AddScoped<ITeleportProvider, FlyingFlagProvider>();
        services.AddScoped<ITeleportProvider, NpcTeleportProvider>();
        services.AddScoped<IPathModule, AutoPathModule>();

        // 加血
        services.AddScoped<IHealModule, AutoHealModule>();

        return services.BuildServiceProvider();
    }

    /// <summary>若文件不存在则写入默认内容。</summary>
    private static void EnsureFile(string path, string defaultContent)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (!File.Exists(path)) File.WriteAllText(path, defaultContent);
    }
}
