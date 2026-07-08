using System.Security.Cryptography;
using System.Text;

namespace IconReplacer.Core;

public sealed class IconLibraryImporter
{
    public OperationResult<IconLibraryEntry> Import(
        string sourceIconPath,
        IconLibraryPaths paths,
        string? preferredName = null,
        IconValidationOptions? validationOptions = null)
    {
        var validation = IconValidator.Validate(sourceIconPath, validationOptions);
        if (!validation.Succeeded || validation.Value is null)
        {
            return OperationResult<IconLibraryEntry>.Failure(validation.Error);
        }

        try
        {
            Directory.CreateDirectory(paths.ImportedRoot);

            var hash = ComputeSha256(validation.Value.FullPath);
            var existingImport = FindExistingImportedIcon(paths, hash);
            if (existingImport is not null)
            {
                return OperationResult<IconLibraryEntry>.Success(ToEntry(paths, existingImport));
            }

            var baseName = SanitizeFileName(
                string.IsNullOrWhiteSpace(preferredName)
                    ? Path.GetFileNameWithoutExtension(validation.Value.FullPath)
                    : preferredName);

            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = "icon";
            }

            var destination = GetAvailableDestination(paths, baseName, hash);
            File.Copy(validation.Value.FullPath, destination);

            return OperationResult<IconLibraryEntry>.Success(ToEntry(paths, destination));
        }
        catch (UnauthorizedAccessException ex)
        {
            return OperationResult<IconLibraryEntry>.Failure(new IconReplacerError(
                ErrorCode.PermissionDenied,
                "The icon could not be imported.",
                ex.Message));
        }
        catch (IOException ex)
        {
            return OperationResult<IconLibraryEntry>.Failure(new IconReplacerError(
                ErrorCode.Unknown,
                "The icon could not be imported.",
                ex.Message));
        }
    }

    private static byte[] ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return SHA256.HashData(stream);
    }

    private static string? FindExistingImportedIcon(IconLibraryPaths paths, byte[] sourceHash)
    {
        var hashPrefix = GetHashPrefix(sourceHash);
        foreach (var file in Directory.EnumerateFiles(paths.ImportedRoot, $"*-{hashPrefix}.ico")
            .Order(StringComparer.OrdinalIgnoreCase))
        {
            var existingHash = ComputeSha256(file);
            if (existingHash.SequenceEqual(sourceHash))
            {
                return file;
            }
        }

        return null;
    }

    private static string GetAvailableDestination(IconLibraryPaths paths, string baseName, byte[] sourceHash)
    {
        var hashPrefix = GetHashPrefix(sourceHash);
        var destination = Path.Combine(paths.ImportedRoot, $"{baseName}-{hashPrefix}.ico");
        if (!File.Exists(destination))
        {
            return destination;
        }

        for (var index = 1; ; index++)
        {
            destination = Path.Combine(paths.ImportedRoot, $"{baseName}-{hashPrefix}-{index}.ico");
            if (!File.Exists(destination))
            {
                return destination;
            }
        }
    }

    private static string GetHashPrefix(byte[] hash)
    {
        return Convert.ToHexString(hash, 0, 4).ToLowerInvariant();
    }

    private static IconLibraryEntry ToEntry(IconLibraryPaths paths, string destination)
    {
        var importedCategory = IconCategory.Imported(paths.LibraryRoot);
        return new IconLibraryEntry(
            Path.GetFileNameWithoutExtension(destination),
            Path.GetFullPath(destination),
            importedCategory,
            new FileInfo(destination).Length);
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars().ToHashSet();
        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            if (!invalidChars.Contains(character))
            {
                builder.Append(character);
            }
            else
            {
                builder.Append('-');
            }
        }

        return builder.ToString().Trim(' ', '.', '-');
    }
}
