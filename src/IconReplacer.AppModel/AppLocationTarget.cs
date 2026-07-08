namespace IconReplacer.AppModel;

public sealed record AppLocationTarget(
    AppLocationKind Kind,
    string Label,
    string FullPath,
    bool Exists,
    bool IsDirectory);
