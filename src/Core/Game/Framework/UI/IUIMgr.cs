namespace iFramework;
public interface IUIMgr
{
    void SetProvider(IUIMgrProvider provider);

    Task<bool> IsUIShown(string uiName, CancellationToken ct = default);

    /// <summary>
    /// 显示指定 UI，并在发出操作后轮询确认 UI 已真正打开。
    /// 返回 true 表示已确认打开；重试用尽仍未确认则返回 false。
    /// </summary>
    Task<bool> ShowUI(string uiName, CancellationToken ct = default);

    /// <summary>
    /// 关闭指定 UI，并在发出操作后轮询确认 UI 已真正关闭。
    /// 返回 true 表示已确认关闭；重试用尽仍未确认则返回 false。
    /// </summary>
    Task<bool> CloseUI(string uiName, CancellationToken ct = default);

    void Dispose();
}
