namespace IconReplacer.AppModel;

public sealed record AppOperationFeedback(
    AppOperationFeedbackSeverity Severity,
    string Title,
    string Detail,
    string? ActionLabel = null,
    string? ActionTarget = null);
