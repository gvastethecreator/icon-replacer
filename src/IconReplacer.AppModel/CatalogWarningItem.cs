using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record CatalogWarningItem(
    string Path,
    string DisplayName,
    string? CategoryName,
    ErrorCode ErrorCode,
    string Message,
    string? Detail)
{
    public static CatalogWarningItem FromWarning(IconCatalogWarning warning, string libraryRoot)
    {
        var directory = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(warning.Path));
        var root = System.IO.Path.GetFullPath(libraryRoot);
        var categoryName = directory is null ||
            string.Equals(directory, root, StringComparison.OrdinalIgnoreCase)
                ? null
                : System.IO.Path.GetFileName(directory);

        return new CatalogWarningItem(
            warning.Path,
            System.IO.Path.GetFileNameWithoutExtension(warning.Path),
            categoryName,
            warning.Error.Code,
            warning.Error.Message,
            warning.Error.Detail);
    }
}
