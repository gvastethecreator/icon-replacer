namespace IconReplacer.Core;

public sealed record IconCatalog(
    string LibraryRoot,
    IReadOnlyList<IconLibraryEntry> Entries,
    IReadOnlyList<IconCategory> Categories,
    IReadOnlyList<IconCatalogWarning> Warnings);

