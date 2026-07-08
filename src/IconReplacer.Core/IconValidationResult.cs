namespace IconReplacer.Core;

public sealed record IconValidationResult(
    string FullPath,
    long LengthBytes,
    IReadOnlyList<IconImageEntry> Images);

