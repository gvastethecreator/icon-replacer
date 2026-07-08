namespace IconReplacer.AppModel;

public sealed record IconBrowserOptions(
    string? SearchText = null,
    string? CategoryName = null,
    int MaxItems = 200)
{
    public static IconBrowserOptions Default { get; } = new();

    public IconBrowserOptions Normalize()
    {
        return new IconBrowserOptions(
            NormalizeText(SearchText),
            NormalizeText(CategoryName),
            Math.Max(0, MaxItems));
    }

    private static string? NormalizeText(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
