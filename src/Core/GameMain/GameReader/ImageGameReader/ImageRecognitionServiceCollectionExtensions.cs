using MengYou.Abstractions;
using MengYou.Core.Sessions;
using MengYou.Recognition.Image.Bag;
using MengYou.Recognition.Image.Combat;
using MengYou.Recognition.Image.Matching;
using MengYou.Recognition.Image.Ocr;
using Microsoft.Extensions.DependencyInjection;

namespace MengYou.Recognition.Image;

/// <summary>
/// 图像识别层 DI 注册扩展。
/// </summary>
public static class ImageRecognitionServiceCollectionExtensions
{
    /// <summary>注册图像识别服务。</summary>
    /// <param name="templateDir">模板目录（相对/绝对路径）。</param>
    /// <param name="layoutFile">UI 布局 JSON 文件路径。</param>
    public static IServiceCollection AddImageRecognition(this IServiceCollection services, string templateDir, string layoutFile)
    {
        // 全局单例：模板匹配器（缓存模板）
        services.AddSingleton(new TemplateMatcher(templateDir));
        // 全局单例：OCR 引擎（占位，后续替换真实实现）
        services.AddSingleton<IOcrEngine, NoopOcrEngine>();
        // 全局单例：UI 布局
        services.AddSingleton(_ => UILayoutLocator.LoadFromFile(layoutFile));
        services.AddSingleton<IUIElementLocator>(sp => sp.GetRequiredService<UILayoutLocator>());

        // Scoped：视觉服务与 Reader 绑定到某个窗口
        services.AddScoped<IVisionService>(sp =>
        {
            var holder = sp.GetRequiredService<SessionConfigHolder>();
            return new VisionService(
                holder.Config.WindowHandle,
                sp.GetRequiredService<TemplateMatcher>(),
                sp.GetRequiredService<IOcrEngine>());
        });

        // Scoped：战场与背包识别的子读取器
        services.AddScoped<HpBarReader>(sp => new HpBarReader(sp.GetRequiredService<IVisionService>()));
        services.AddScoped<UnitPanelReader>();
        services.AddScoped<TeamPanelReader>();
        services.AddScoped<EnemyPanelReader>();
        services.AddScoped<BagPanelReader>(sp => new BagPanelReader(
            sp.GetRequiredService<IVisionService>(),
            sp.GetRequiredService<IUIElementLocator>()));

        // Scoped：总 Reader
        services.AddScoped<IGameReader, ImageGameReader>();

        return services;
    }
}
