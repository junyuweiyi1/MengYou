namespace MengYou.Runtime;

/// <summary>按固定或指数退避间隔重试子步骤。</summary>
public sealed class RetryWorkflowStep : IWorkflowStep
{
    private readonly IWorkflowStep _inner;
    private readonly int _maxAttempts;
    private readonly TimeSpan _delay;
    private readonly bool _exponentialBackoff;

    public RetryWorkflowStep(
        string name,
        IWorkflowStep inner,
        int maxAttempts,
        TimeSpan? delay = null,
        bool exponentialBackoff = true)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("步骤名不能为空。", nameof(name))
            : name;
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        if (maxAttempts <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        _maxAttempts = maxAttempts;
        _delay = delay ?? TimeSpan.Zero;
        if (_delay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delay));
        _exponentialBackoff = exponentialBackoff;
    }

    public string Name { get; }

    public async Task<WorkflowStepResult> ExecuteAsync(
        GameFeatureContext context,
        CancellationToken ct = default)
    {
        var attempts = new List<WorkflowStepResult>(_maxAttempts);
        for (var attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            var result = await _inner.ExecuteAsync(context, ct).ConfigureAwait(false);
            attempts.Add(result);
            if (result.IsSuccess)
                return WorkflowStepResult.Success(Name, attempts);
            if (result.Status == WorkflowStepStatus.Cancelled)
                return WorkflowStepResult.Create(Name, result.Status, result.Message, attempts);

            if (attempt < _maxAttempts && _delay > TimeSpan.Zero)
            {
                var multiplier = _exponentialBackoff ? Math.Pow(2, attempt - 1) : 1;
                var wait = TimeSpan.FromMilliseconds(_delay.TotalMilliseconds * multiplier);
                await Task.Delay(wait, ct).ConfigureAwait(false);
            }
        }

        var last = attempts[^1];
        return WorkflowStepResult.Create(
            Name,
            last.Status,
            $"重试 {_maxAttempts} 次后仍失败：{last.Message}",
            attempts);
    }
}
