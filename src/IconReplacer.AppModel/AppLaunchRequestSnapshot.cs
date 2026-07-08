using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record AppLaunchRequestSnapshot(
    AppLaunchVerb Verb,
    string VerbName,
    string RequestedTargetPath,
    ShellSelectionEvaluation Selection,
    IconPickerRequestSnapshot PickerRequest,
    bool CanLaunch,
    IReadOnlyList<string> AppArguments,
    string DisplayArguments,
    IconReplacerError Error,
    DateTimeOffset RefreshedAt);
