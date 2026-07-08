using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class IconDetailsService
{
    public const string ExternalCategoryName = "(external)";

    private readonly IconCatalogService _catalogService;

    public IconDetailsService(IconCatalogService? catalogService = null)
    {
        _catalogService = catalogService ?? new IconCatalogService();
    }

    public OperationResult<IconDetailsSnapshot> GetDetails(
        string iconPath,
        IconLibraryPaths paths)
    {
        var validation = IconValidator.Validate(iconPath);
        if (!validation.Succeeded || validation.Value is null)
        {
            return OperationResult<IconDetailsSnapshot>.Failure(validation.Error);
        }

        var catalog = _catalogService.Scan(paths);
        if (!catalog.Succeeded || catalog.Value is null)
        {
            return OperationResult<IconDetailsSnapshot>.Failure(catalog.Error);
        }

        var catalogEntry = catalog.Value.Entries.FirstOrDefault(entry =>
            string.Equals(entry.FullPath, validation.Value.FullPath, StringComparison.OrdinalIgnoreCase));
        var imageDetails = validation.Value.Images
            .Select(ToImageDetail)
            .OrderByDescending(image => image.Width * image.Height)
            .ThenByDescending(image => image.BitCount)
            .ToArray();
        var recommendedImage = imageDetails
            .OrderByDescending(image => image.HasUsefulSize)
            .ThenByDescending(image => image.Width * image.Height)
            .ThenByDescending(image => image.BitCount)
            .First();

        return OperationResult<IconDetailsSnapshot>.Success(new IconDetailsSnapshot(
            validation.Value.FullPath,
            catalogEntry?.DisplayName ?? Path.GetFileNameWithoutExtension(validation.Value.FullPath),
            catalogEntry?.Category?.Name ?? ExternalCategoryName,
            catalogEntry is not null,
            validation.Value.LengthBytes,
            validation.Value.Images.Count,
            recommendedImage,
            imageDetails,
            DateTimeOffset.UtcNow));
    }

    public OperationResult<IconDetailsSnapshot> GetDetailsFromEnvironment(string iconPath)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<IconDetailsSnapshot>.Failure(paths.Error);
        }

        return GetDetails(iconPath, paths.Value);
    }

    private static IconImageDetail ToImageDetail(IconImageEntry image)
    {
        return new IconImageDetail(
            image.Width,
            image.Height,
            image.BitCount,
            image.BytesInResource,
            image.ImageOffset,
            image.HasUsefulSize);
    }
}
