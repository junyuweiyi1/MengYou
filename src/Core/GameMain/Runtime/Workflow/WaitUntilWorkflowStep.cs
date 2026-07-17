namespace MengYou.Runtime;

/// <summary>等待状态存储出现满足条件的快照。</summary>
public sealed class WaitUntilWorkflowStep : IWorkflowStep
{
    private readonly Func<GameStateSnapshot, bool> _predicate;
    private readonly TimeSpan _timeout;

    public WaitUntilWorkflowStep(
        string name,
        Func<GameStateSnapshot, bool> predicate,
        TimeSpan timeout)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("步骤名不能为空。", nameof(name))
            : name;
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        if (timeout <= TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(timeout));
        _timeout = timeout;
    }

    public string Name { get; }

    public async Task<WorkflowStepResult> ExecuteAsync(
        GameFeatureContext context,
        CancellationToken ct = default)
    {
        try
        {
            await context
                .WaitForStateAsync(_predicate, _timeout, ct)
                .ConfigureAwait(false);
            return WorkflowStepResult.Success(Name);
        }
        catch (TimeoutException ex)
        {
            return WorkflowStepResult.Create(Name, WorkflowStepStatus.TimedOut, ex.Message);
        }
    }
}
