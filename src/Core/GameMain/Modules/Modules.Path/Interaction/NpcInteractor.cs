using MengYou.Abstractions;
using MengYou.Abstractions.Models;
using Microsoft.Extensions.Logging;

namespace MengYou.Modules.Path.Interaction;

/// <summary>
/// NPC 对话推进器：定位 NPC → 点击 → 循环点击对话选项直到符合期望。
/// </summary>
public sealed class NpcInteractor
{
    /// <summary>Reader。</summary>
    private readonly IGameReader _reader;

    /// <summary>Controller。</summary>
    private readonly IGameController _controller;

    /// <summary>UI 定位。</summary>
    private readonly IUIElementLocator _locator;

    /// <summary>视觉服务：动态定位 NPC 与选项。</summary>
    private readonly IVisionService _vision;

    /// <summary>日志。</summary>
    private readonly ILogger<NpcInteractor> _logger;

    /// <summary>单次对话最大回合数。</summary>
    private const int MaxDialogTurns = 20;

    /// <summary>每次对话动作后等待毫秒。</summary>
    private const int TurnDelayMs = 600;

    /// <summary>构造。</summary>
    public NpcInteractor(IGameReader reader, IGameController controller, IUIElementLocator locator, IVisionService vision, ILogger<NpcInteractor> logger)
    {
        _reader = reader;
        _controller = controller;
        _locator = locator;
        _vision = vision;
        _logger = logger;
    }

    /// <summary>通过模板匹配定位 NPC 并点击开启对话。</summary>
    /// <param name="npcTemplateKey">NPC 模板名（如 "NPC.李靖"）。</param>
    public async Task<bool> ClickNpcAsync(string npcTemplateKey, CancellationToken ct = default)
    {
        _vision.Refresh();
        var pt = _vision.FindTemplate(npcTemplateKey);
        if (pt == null)
        {
            _logger.LogWarning("未找到 NPC 模板 {Key}", npcTemplateKey);
            return false;
        }
        await _controller.ClickAsync(pt.Value, MouseButton.Left, ct);
        return true;
    }

    /// <summary>推进对话：按顺序尝试点击 optionTemplates 中的每个选项模板，直到对话消失。</summary>
    /// <param name="optionTemplates">对话选项模板顺序（例："Dialog.下一页", "Dialog.接受"）。</param>
    public async Task<bool> AdvanceDialogAsync(IReadOnlyList<string> optionTemplates, CancellationToken ct = default)
    {
        for (var i = 0; i < optionTemplates.Count && i < MaxDialogTurns; i++)
        {
            await Task.Delay(TurnDelayMs, ct);
            _vision.Refresh();
            // 对话已消失即完成
            if (_reader.GetSceneType() != SceneType.Dialog)
            {
                _logger.LogDebug("对话已结束");
                return true;
            }
            var optKey = optionTemplates[i];
            var pt = _vision.FindTemplate(optKey);
            if (pt == null)
            {
                _logger.LogWarning("未找到对话选项 {Key}", optKey);
                return false;
            }
            await _controller.ClickAsync(pt.Value, MouseButton.Left, ct);
        }
        return true;
    }
}
