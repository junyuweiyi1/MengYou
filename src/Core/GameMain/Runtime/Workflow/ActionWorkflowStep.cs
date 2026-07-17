namespace MengYou.Runtime;

/// <summary>把一个游戏动作适配为工作流步骤。</summary>
public sealed class ActionWorkflowStep : IWorkflowStep
{
    private readonly IGameAction _action;

    public ActionWorkflowStep(IGameAction action, string? name = null)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        Name = string.IsNullOrWhiteSpace(name) ? action.Name : name;
    }

    public string Name { get; }

    public async Task<WorkflowStepResult> ExecuteAsync(
        GameFeatureContext context,
        CancellationToken ct = default)
    {
        var actionResult = await context.ExecuteAsync(_action, ct).ConfigureAwait(false);
        var status = actionResult.Status switch
        {
            GameActionStatus.Succeeded => WorkflowStepStatus.Succeeded,
            GameActionStatus.Timeout => WorkflowStepStatus.TimedOut,
            GameActionStatus.Interrupted when ct.IsCancellationRequested => WorkflowStepStatus.Cancelled,
            _ => WorkflowStepStatus.Failed,
        };

        return new WorkflowStepResult
        {
            StepName = Name,
            Status = status,
            Message = actionResult.Message,
            ActionResult = actionResult,
        };
    }
}
