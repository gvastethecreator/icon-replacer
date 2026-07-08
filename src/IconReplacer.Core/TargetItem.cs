namespace IconReplacer.Core;

public sealed record TargetItem(TargetKind Kind, string FullPath)
{
    public static OperationResult<TargetItem> FromShellSelection(string path, bool isDirectory)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return OperationResult<TargetItem>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "A target path is required."));
        }

        string fullPath;

        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return OperationResult<TargetItem>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "The target path is not valid.",
                ex.Message));
        }

        if (isDirectory)
        {
            return OperationResult<TargetItem>.Success(new TargetItem(TargetKind.Folder, fullPath));
        }

        if (string.Equals(Path.GetExtension(fullPath), ".lnk", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<TargetItem>.Success(new TargetItem(TargetKind.Shortcut, fullPath));
        }

        return OperationResult<TargetItem>.Failure(new IconReplacerError(
            ErrorCode.UnsupportedTarget,
            "Icon Replacer supports folders and .lnk shortcuts in V1."));
    }
}

