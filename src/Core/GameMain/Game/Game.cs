using iFramework;

/// <summary>
/// 游戏对象：单个游戏窗口对应一个 Game，聚合背包/状态读取等业务组件。
/// </summary>
public class Game
{
    /// <summary>输入</summary>
    public IInputMgr InputMgr { get; private set; }
    /// <summary>UI位置</summary>
    public IUIElementLocateMgr UIElementLocateMgr { get; private set; }

    /// <summary>背包管理器。</summary>
    public BagMgr BagMgr { get; }

    public IGameReader GameReader { get; }
    public IGameControl GameControl { get; }

    /// <summary>构造。</summary>
    /// <param name="reader">状态读取器。</param>
    /// <param name="controller">输入控制器。</param>
    /// <param name="locator">UI 元素定位器。</param>
    public Game(IGameReader reader, IInputMgr controller, IUIElementLocateMgr locator)
    {
        GameReader = reader;
        BagMgr = new BagMgr(reader, controller, locator);
    }
}