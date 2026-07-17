namespace MengYou.UI;

/// <summary>强类型 UI 标识，避免业务代码散落字符串常量。</summary>
public readonly record struct UiId
{
    public UiId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("UI 标识不能为空。", nameof(value));
        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value ?? string.Empty;
}

/// <summary>游戏内已知面板标识。</summary>
public static class GameUiIds
{
    public static readonly UiId Bag = new("道具行囊");

    public static readonly UiId PlayerState = new("人物状态");

    public static UiId FromName(string name) => new(name);
}

/// <summary>UI 可见性。Unknown 与 Hidden 必须区分，避免识别失败被当成面板关闭。</summary>
public enum UiVisibility
{
    Unknown,
    Hidden,
    Visible,
}

/// <summary>一次 UI 状态观测。</summary>
public sealed record UiObservation
{
    public required UiId UiId { get; init; }

    public required UiVisibility Visibility { get; init; }

    public required double Confidence { get; init; }

    public required DateTimeOffset ObservedAt { get; init; }

    public string? Evidence { get; init; }

    public static UiObservation Unknown(UiId uiId, string? evidence = null)
        => new()
        {
            UiId = uiId,
            Visibility = UiVisibility.Unknown,
            Confidence = 0,
            ObservedAt = DateTimeOffset.UtcNow,
            Evidence = evidence,
        };
}
