namespace MengYou.Runtime;

/// <summary>工作流步骤终止状态。</summary>
public enum WorkflowStepStatus
{
    Succeeded,
    Failed,
    TimedOut,
    Cancelled,
}

/// <summary>工作流步骤结果，可携带子步骤与底层动作结果。</summary>
public sealed record WorkflowStepResult
{
    public required string StepName { get; init; }

    public required WorkflowStepStatus Status { get; init; }

    public string? Message { get; init; }

    public GameActionResult? ActionResult { get; init; }

    public IReadOnlyList<WorkflowStepResult> Children { get; init; }
        = Array.Empty<WorkflowStepResult>();

    public bool IsSuccess => Status == WorkflowStepStatus.Succeeded;

    public static WorkflowStepResult Success(
        string stepName,
        IReadOnlyList<WorkflowStepResult>? children = null)
        => new()
        {
            StepName = stepName,
            Status = WorkflowStepStatus.Succeeded,
            Children = children ?? Array.Empty<WorkflowStepResult>(),
        };

    public static WorkflowStepResult Create(
        string stepName,
        WorkflowStepStatus status,
        string? message = null,
        IReadOnlyList<WorkflowStepResult>? children = null)
        => new()
        {
            StepName = stepName,
            Status = status,
            Message = message,
            Children = children ?? Array.Empty<WorkflowStepResult>(),
        };
}

/// <summary>可组合工作流步骤。</summary>
public interface IWorkflowStep
{
    string Name { get; }

    Task<WorkflowStepResult> ExecuteAsync(
        GameFeatureContext context,
        CancellationToken ct = default);
}
