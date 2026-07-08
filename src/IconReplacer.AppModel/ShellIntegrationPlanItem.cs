namespace IconReplacer.AppModel;

public sealed record ShellIntegrationPlanItem(
    string Id,
    AppDiagnosticStatus Status,
    string Title,
    string Detail,
    bool RequiredForV1);
