namespace IconReplacer.AppModel;

public sealed record AppDiagnosticCheck(
    string Id,
    AppDiagnosticStatus Status,
    string Title,
    string Detail);
