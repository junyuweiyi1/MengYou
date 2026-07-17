namespace MengYou.Runtime;

/// <summary>把工作流根节点托管为一个会话功能。</summary>
public sealed class WorkflowFeature : IGameFeature
{
    private readonly IWorkflowStep _root;

    public WorkflowFeature(string id, string displayName, IWorkflowStep root)
    {
        Id = string.IsNullOrWhiteSpace(id)
            ? throw new ArgumentException("功能 Id 不能为空。", nameof(id))
            : id;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName;
        _root = root ?? throw new ArgumentNullException(nameof(root));
    }

    public string Id { get; }

    public string DisplayName { get; }

    public async Task RunAsync(GameFeatureContext context, CancellationToken ct = default)
    {
        var result = await _root.ExecuteAsync(context, ct).ConfigureAwait(false);
        if (result.IsSuccess) return;
        if (result.Status == WorkflowStepStatus.Cancelled && ct.IsCancellationRequested)
            throw new OperationCanceledException(ct);
        throw new WorkflowFailedException(result);
    }
}

public sealed class WorkflowFailedException : Exception
{
    public WorkflowFailedException(WorkflowStepResult result)
        : base(result.Message ?? $"工作流步骤失败：{result.StepName}")
    {
        Result = result;
    }

    public WorkflowStepResult Result { get; }
}
