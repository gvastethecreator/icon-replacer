namespace IconReplacer.Core;

public sealed record IconLibraryEntry(
    string DisplayName,
    string FullPath,
    IconCategory? Category,
    long LengthBytes);

