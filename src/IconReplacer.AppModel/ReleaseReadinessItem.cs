namespace IconReplacer.AppModel;

public sealed record ReleaseReadinessItem(
    string Id,
    AppDiagnosticStatus Status,
    string Title,
    string Detail,
    bool RequiredForRelease,
    string EvidenceCommand);
