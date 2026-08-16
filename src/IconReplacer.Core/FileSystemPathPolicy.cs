namespace IconReplacer.Core;

public static class FileSystemPathPolicy
{
    private const int MaximumLinkDepth = 32;

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

    public static OperationResult<string> ValidateExistingLocalDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Failure(ErrorCode.InvalidArgument, "A folder target path is required.");
        }

        if (IsRemoteOrUnsupported(path))
        {
            return Failure(
                ErrorCode.RemotePathUnsupported,
                "Remote or web-backed folders are not supported in V1.",
                path);
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return Failure(ErrorCode.InvalidArgument, "The folder target path is not valid.", ex.Message);
        }

        try
        {
            var current = new DirectoryInfo(fullPath);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var depth = 0; depth < MaximumLinkDepth; depth++)
            {
                if (!visited.Add(current.FullName))
                {
                    return Failure(
                        ErrorCode.UnsupportedTarget,
                        "The folder link contains a cycle and cannot be changed safely.",
                        fullPath);
                }

                var linkTarget = current.ResolveLinkTarget(returnFinalTarget: false);
                if (linkTarget is null)
                {
                    break;
                }

                if (IsRemoteOrUnsupported(linkTarget.FullName))
                {
                    return Failure(
                        ErrorCode.RemotePathUnsupported,
                        "Directory links to remote or web-backed folders are not supported in V1.",
                        linkTarget.FullName);
                }

                current = new DirectoryInfo(linkTarget.FullName);

                if (depth == MaximumLinkDepth - 1)
                {
                    return Failure(
                        ErrorCode.UnsupportedTarget,
                        "The folder link chain is too deep to change safely.",
                        fullPath);
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            return Failure(
                ErrorCode.PermissionDenied,
                "The folder link target could not be inspected.",
                ex.Message);
        }
        catch (IOException ex)
        {
            return Failure(
                ErrorCode.PathNotFound,
                "The folder link target could not be resolved.",
                ex.Message);
        }

        if (!Directory.Exists(fullPath))
        {
            return Failure(
                ErrorCode.PathNotFound,
                "The folder target does not exist.",
                fullPath);
        }

        return OperationResult<string>.Success(fullPath);
    }

    private static OperationResult<string> Failure(
        ErrorCode code,
        string message,
        string? detail = null)
    {
        return OperationResult<string>.Failure(new IconReplacerError(code, message, detail));
    }
}
