namespace MengYou.Runtime;

/// <summary>按顺序执行子步骤，首个失败即终止。</summary>
public sealed class SequenceWorkflowStep : IWorkflowStep
{
    private readonly IReadOnlyList<IWorkflowStep> _steps;

    public SequenceWorkflowStep(string name, params IWorkflowStep[] steps)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("步骤名不能为空。", nameof(name))
            : name;
        _steps = steps?.ToArray() ?? throw new ArgumentNullException(nameof(steps));
        if (_steps.Count == 0)
            throw new ArgumentException("Sequence 至少需要一个子步骤。", nameof(steps));
    }

    public string Name { get; }

    public async Task<WorkflowStepResult> ExecuteAsync(
        GameFeatureContext context,
        CancellationToken ct = default)
    {
        var results = new List<WorkflowStepResult>(_steps.Count);
        foreach (var step in _steps)
        {
            ct.ThrowIfCancellationRequested();
            var result = await step.ExecuteAsync(context, ct).ConfigureAwait(false);
            results.Add(result);
            if (!result.IsSuccess)
            {
                return WorkflowStepResult.Create(
                    Name,
                    result.Status,
                    $"子步骤失败：{result.StepName}",
                    results);
            }
        }

        return WorkflowStepResult.Success(Name, results);
    }
}
