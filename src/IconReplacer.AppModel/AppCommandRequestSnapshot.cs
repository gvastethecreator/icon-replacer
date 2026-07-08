using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record AppCommandRequestSnapshot(
    string RouteId,
    string CommandId,
    AppCommandDescriptor Command,
    bool CanExecute,
    string Summary,
    string? NavigationTarget,
    AppLocationOpenRequest? LocationOpenRequest,
    string? WorkflowId,
    bool RequiresRefresh,
    IconReplacerError Error,
    DateTimeOffset RefreshedAt);
