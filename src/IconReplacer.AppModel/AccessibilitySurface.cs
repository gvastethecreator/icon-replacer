namespace IconReplacer.AppModel;

public sealed record AccessibilitySurface(
    string RouteId,
    string Title,
    string Purpose,
    IReadOnlyList<string> PrimaryCommands,
    bool RequiresKeyboardReachability,
    bool RequiresPersistentErrors);
