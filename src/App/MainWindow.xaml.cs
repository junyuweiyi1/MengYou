using System.Windows;
using MengYou.Abstractions;
using MengYou.Abstractions.Modules;
using MengYou.Core.Sessions;
using MengYou.Platform.Win32;
using Microsoft.Extensions.DependencyInjection;

namespace MengYou.App;

/// <summary>
/// 主窗口：Session 挂载 + 简单业务触发。
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>根 DI 容器。</summary>
    private readonly IServiceProvider _services;

    /// <summary>Session 管理器。</summary>
    private readonly SessionManager _sessions;

    /// <summary>构造。</summary>
    public MainWindow(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
        _sessions = services.GetRequiredService<SessionManager>();
    }

    /// <summary>刷新可用游戏窗口列表。</summary>
    private void OnRefreshWindows(object sender, RoutedEventArgs e)
    {
        // 过滤词可修改为"梦幻西游"
        var list = WindowEnumerator.Enumerate();
        WindowsList.ItemsSource = list;
    }

    /// <summary>把所选窗口挂载为一个 Session。</summary>
    private void OnAttachWindow(object sender, RoutedEventArgs e)
    {
        if (WindowsList.SelectedItem is not GameWindow gw)
        {
            MessageBox.Show("请先选择一个窗口");
            return;
        }
        var cfg = new SessionConfig
        {
            WindowHandle = gw.Handle,
            ProcessId = gw.ProcessId,
            DisplayName = gw.Title,
            InputMode = InputMode.Background,
            EnableHumanized = true,
        };
        var session = _sessions.Create(cfg);
        _ = session.StartAsync();
        SessionsList.ItemsSource = _sessions.All;
    }

    /// <summary>对选中 Session 启动加血模块。</summary>
    private void OnStartHeal(object sender, RoutedEventArgs e)
    {
        if (SessionsList.SelectedItem is not IGameSession s) return;
        var heal = s.Services.GetRequiredService<IHealModule>();
        _ = heal.StartAsync();
    }

    /// <summary>对选中 Session 触发寻路测试（占位坐标）。</summary>
    private void OnRunPath(object sender, RoutedEventArgs e)
    {
        if (SessionsList.SelectedItem is not IGameSession s) return;
        var path = s.Services.GetRequiredService<IPathModule>();
        _ = path.NavigateAsync(new PathTarget
        {
            TargetMapId = "傲来",
            TargetPosition = new Abstractions.Models.Point2D(35, 42),
        });
    }
}
