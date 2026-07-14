namespace MengYou.Abstractions.Models;

/// <summary>
/// NPC 对话框信息。
/// </summary>
public sealed class DialogInfo
{
    /// <summary>NPC 名字。</summary>
    public string NpcName { get; init; } = string.Empty;

    /// <summary>对话正文。</summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>可选选项文本列表。</summary>
    public IReadOnlyList<string> Options { get; init; } = Array.Empty<string>();
}
