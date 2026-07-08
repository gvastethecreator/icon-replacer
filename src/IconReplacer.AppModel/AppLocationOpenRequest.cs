using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record AppLocationOpenRequest(
    AppLocationTarget Location,
    bool CanOpen,
    string TargetPath,
    string ShellVerb,
    IconReplacerError Error);
