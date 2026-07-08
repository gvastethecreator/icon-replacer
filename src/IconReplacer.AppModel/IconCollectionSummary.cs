namespace IconReplacer.AppModel;

public sealed record IconCollectionSummary(
    string Name,
    string FullPath,
    int IconCount,
    bool IsImportedCollection);
