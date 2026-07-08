namespace IconReplacer.AppModel;

public sealed record IconMenuCategory(
    string Name,
    string FullPath,
    int TotalIconCount,
    int OmittedIconCount,
    IReadOnlyList<IconMenuItem> Items)
{
    public bool IsTruncated => OmittedIconCount > 0;
}
