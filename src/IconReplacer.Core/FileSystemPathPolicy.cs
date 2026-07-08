namespace IconReplacer.Core;

public static class FileSystemPathPolicy
{
    public static bool IsRemoteOrUnsupported(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("webdav://", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (path.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return path.StartsWith(@"\\", StringComparison.Ordinal) &&
            !path.StartsWith(@"\\?\", StringComparison.OrdinalIgnoreCase);
    }
}

