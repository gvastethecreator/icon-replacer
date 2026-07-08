using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record AppActivationSnapshot(
    AppActivationKind Kind,
    IReadOnlyList<string> Arguments,
    AppHomeSnapshot? Home,
    AppLaunchRequestSnapshot? LaunchRequest,
    bool CanContinue,
    IconReplacerError Error,
    DateTimeOffset RefreshedAt);
