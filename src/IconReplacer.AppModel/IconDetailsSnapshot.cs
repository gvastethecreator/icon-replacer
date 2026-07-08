namespace IconReplacer.AppModel;

public sealed record IconDetailsSnapshot(
    string FullPath,
    string DisplayName,
    string CategoryName,
    bool IsInIconLibrary,
    long LengthBytes,
    int ImageCount,
    IconImageDetail RecommendedImage,
    IReadOnlyList<IconImageDetail> Images,
    DateTimeOffset RefreshedAt);
