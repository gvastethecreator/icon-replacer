using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record AppActionRequestSnapshot(
    SetupAction Action,
    AppActionKind Kind,
    bool CanExecute,
    string NavigationTarget,
    RestoreHistoryFilter? HistoryFilter,
    IconReplacerError Error,
    DateTimeOffset RefreshedAt);
