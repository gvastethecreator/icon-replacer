namespace IconReplacer.Core;

public sealed class IconCatalogService
{
    public OperationResult<IconCatalog> Scan(
        IconLibraryPaths paths,
        IconValidationOptions? validationOptions = null)
    {
        try
        {
            Directory.CreateDirectory(paths.LibraryRoot);
            Directory.CreateDirectory(paths.ImportedRoot);
        }
        catch (UnauthorizedAccessException ex)
        {
            return OperationResult<IconCatalog>.Failure(new IconReplacerError(
                ErrorCode.PermissionDenied,
                "The icon library could not be created.",
                ex.Message));
        }
        catch (IOException ex)
        {
            return OperationResult<IconCatalog>.Failure(new IconReplacerError(
                ErrorCode.Unknown,
                "The icon library could not be created.",
                ex.Message));
        }

        var entries = new List<IconLibraryEntry>();
        var categories = new List<IconCategory>();
        var warnings = new List<IconCatalogWarning>();

        AddFiles(paths.LibraryRoot, category: null);

        foreach (var directory in Directory.EnumerateDirectories(paths.LibraryRoot).Order(StringComparer.OrdinalIgnoreCase))
        {
            if (HasReparsePoint(directory))
            {
                continue;
            }

            var category = new IconCategory(Path.GetFileName(directory), Path.GetFullPath(directory));
            categories.Add(category);
            AddFiles(directory, category);
        }

        return OperationResult<IconCatalog>.Success(new IconCatalog(
            paths.LibraryRoot,
            entries.OrderBy(entry => entry.Category?.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            categories.OrderBy(category => category.Name, StringComparer.OrdinalIgnoreCase).ToArray(),
            warnings.ToArray()));

        void AddFiles(string directory, IconCategory? category)
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*.ico").Order(StringComparer.OrdinalIgnoreCase))
            {
                if (HasReparsePoint(file))
                {
                    continue;
                }

                var validation = IconValidator.Validate(file, validationOptions);
                if (!validation.Succeeded || validation.Value is null)
                {
                    warnings.Add(new IconCatalogWarning(file, validation.Error));
                    continue;
                }

                entries.Add(new IconLibraryEntry(
                    Path.GetFileNameWithoutExtension(file),
                    validation.Value.FullPath,
                    category,
                    validation.Value.LengthBytes));
            }
        }
    }

    private static bool HasReparsePoint(string path)
    {
        return (File.GetAttributes(path) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
    }
}

