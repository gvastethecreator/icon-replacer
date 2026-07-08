using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record IconLibraryStatus(
    string LibraryRoot,
    string ImportedRoot,
    int CategoryCount,
    int IconCount,
    int WarningCount,
    IReadOnlyList<IconCatalogWarning> Warnings);
