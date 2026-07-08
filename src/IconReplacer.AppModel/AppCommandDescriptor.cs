using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record AppCommandDescriptor(
    string Id,
    string Label,
    AppCommandKind Kind,
    bool IsPrimary,
    bool IsEnabled,
    string? TargetRouteId,
    AppLocationKind? LocationKind,
    string Detail,
    IconReplacerError Error);
