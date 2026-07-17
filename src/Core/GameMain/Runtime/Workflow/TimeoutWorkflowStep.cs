namespace MengYou.Runtime;

/// <summary>为子步骤增加独立超时边界。</summary>
public sealed class TimeoutWorkflowStep : IWorkflowStep
{
    private readonly IWorkflowStep _inner;
    private readonly TimeSpan _timeout;

    public TimeoutWorkflowStep(string name, IWorkflowStep inner, TimeSpan timeout)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("步骤名不能为空。", nameof(name))
            : name;
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout));
        _timeout = timeout;
    }

    public string Name { get; }

    public async Task<WorkflowStepResult> ExecuteAsync(
        GameFeatureContext context,
        CancellationToken ct = default)
    {
        using var timeoutCts = new CancellationTokenSource(_timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            ct,
            timeoutCts.Token);

        try
        {
            var result = await _inner
                .ExecuteAsync(context, linkedCts.Token)
                .ConfigureAwait(false);
            if (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                return WorkflowStepResult.Create(
                    Name,
                    WorkflowStepStatus.TimedOut,
                    $"步骤超过超时时间：{_timeout}。",
                    new[] { result });
            }
            return result.IsSuccess
                ? WorkflowStepResult.Success(Name, new[] { result })
                : WorkflowStepResult.Create(Name, result.Status, result.Message, new[] { result });
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            return WorkflowStepResult.Create(
                Name,
                WorkflowStepStatus.TimedOut,
                $"步骤超过超时时间：{_timeout}。");
        }
    }
}
