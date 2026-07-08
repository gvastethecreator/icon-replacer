namespace IconReplacer.AppModel;

public sealed record AppNavigationRoute(
    string RouteId,
    string Title,
    AppNavigationSection Section,
    string Purpose,
    IReadOnlyList<string> PrimaryCommands,
    bool IsTopLevel,
    bool RequiresSelection,
    RestoreHistoryFilter? DefaultHistoryFilter);
