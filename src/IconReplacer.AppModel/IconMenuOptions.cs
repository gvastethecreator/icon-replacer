namespace IconReplacer.AppModel;

public sealed record IconMenuOptions(
    int MaxRootIconItems = 50,
    int MaxCategoryCount = 24,
    int MaxIconItemsPerCategory = 50)
{
    public static IconMenuOptions Default { get; } = new();

    public IconMenuOptions Normalize()
    {
        return new IconMenuOptions(
            Math.Max(0, MaxRootIconItems),
            Math.Max(0, MaxCategoryCount),
            Math.Max(0, MaxIconItemsPerCategory));
    }
}
