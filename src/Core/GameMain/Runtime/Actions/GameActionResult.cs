namespace MengYou.Runtime;

/// <summary>游戏动作的终止状态。</summary>
public enum GameActionStatus
{
    Succeeded,
    NotApplicable,
    Timeout,
    VerificationFailed,
    Interrupted,
    RequiresHuman,
    Faulted,
}

/// <summary>
/// 游戏动作执行结果。动作失败是可建模的业务结果，不应只用 bool 或异常表达。
/// </summary>
public sealed record GameActionResult
{
    public required string ActionName { get; init; }

    public required GameActionStatus Status { get; init; }

    public string? Message { get; init; }

    public Exception? Exception { get; init; }

    public DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset CompletedAt { get; init; }

    public bool IsSuccess => Status == GameActionStatus.Succeeded;

    public TimeSpan Duration => CompletedAt >= StartedAt
        ? CompletedAt - StartedAt
        : TimeSpan.Zero;

    public static GameActionResult Success(string actionName, string? message = null)
        => Create(actionName, GameActionStatus.Succeeded, message);

    public static GameActionResult VerificationFailed(string actionName, string? message = null)
        => Create(actionName, GameActionStatus.VerificationFailed, message);

    public static GameActionResult Interrupted(string actionName, string? message = null)
        => Create(actionName, GameActionStatus.Interrupted, message);

    public static GameActionResult Faulted(string actionName, Exception exception, string? message = null)
        => Create(actionName, GameActionStatus.Faulted, message ?? exception.Message, exception);

    public static GameActionResult Create(
        string actionName,
        GameActionStatus status,
        string? message = null,
        Exception? exception = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new GameActionResult
        {
            ActionName = actionName,
            Status = status,
            Message = message,
            Exception = exception,
            StartedAt = now,
            CompletedAt = now,
        };
    }
}
