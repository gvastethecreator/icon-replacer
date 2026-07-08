namespace IconReplacer.AppModel;

public sealed record SetupAction(
    string Id,
    SetupActionSeverity Severity,
    string Title,
    string Detail);
