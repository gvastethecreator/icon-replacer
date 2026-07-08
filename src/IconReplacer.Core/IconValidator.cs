namespace IconReplacer.Core;

public static class IconValidator
{
    private const ushort IcoType = 1;
    private const int HeaderLength = 6;
    private const int DirectoryEntryLength = 16;

    public static OperationResult<IconValidationResult> Validate(
        string path,
        IconValidationOptions? options = null)
    {
        options ??= IconValidationOptions.Default;

        if (string.IsNullOrWhiteSpace(path))
        {
            return Invalid("An icon path is required.");
        }

        if (FileSystemPathPolicy.IsRemoteOrUnsupported(path))
        {
            return OperationResult<IconValidationResult>.Failure(new IconReplacerError(
                ErrorCode.RemotePathUnsupported,
                "Remote or web-backed icon paths are not supported in V1."));
        }

        string fullPath;

        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return OperationResult<IconValidationResult>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "The icon path is not valid.",
                ex.Message));
        }

        if (!string.Equals(Path.GetExtension(fullPath), ".ico", StringComparison.OrdinalIgnoreCase))
        {
            return Invalid("Only .ico files are supported in V1.");
        }

        if (!File.Exists(fullPath))
        {
            return OperationResult<IconValidationResult>.Failure(new IconReplacerError(
                ErrorCode.PathNotFound,
                "The icon file does not exist.",
                fullPath));
        }

        FileInfo fileInfo;

        try
        {
            fileInfo = new FileInfo(fullPath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return OperationResult<IconValidationResult>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "The icon path is not valid.",
                ex.Message));
        }

        if (fileInfo.Length == 0)
        {
            return Invalid("The icon file is empty.");
        }

        if (fileInfo.Length > options.MaxLengthBytes)
        {
            return Invalid($"The icon file is larger than the {options.MaxLengthBytes} byte limit.");
        }

        try
        {
            using var stream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(stream);

            if (stream.Length < HeaderLength)
            {
                return Invalid("The icon header is incomplete.");
            }

            var reserved = reader.ReadUInt16();
            var type = reader.ReadUInt16();
            var count = reader.ReadUInt16();

            if (reserved != 0 || type != IcoType)
            {
                return Invalid("The file is not an ICO image.");
            }

            if (count == 0 || count > options.MaxImageCount)
            {
                return Invalid($"The icon image count must be between 1 and {options.MaxImageCount}.");
            }

            var directoryLength = HeaderLength + count * DirectoryEntryLength;
            if (stream.Length < directoryLength)
            {
                return Invalid("The icon directory is incomplete.");
            }

            var images = new List<IconImageEntry>(count);
            for (var i = 0; i < count; i++)
            {
                var width = DecodeIconDimension(reader.ReadByte());
                var height = DecodeIconDimension(reader.ReadByte());
                var colorCount = reader.ReadByte();
                var reservedEntryByte = reader.ReadByte();
                var planes = reader.ReadUInt16();
                var bitCount = reader.ReadUInt16();
                var bytesInResource = reader.ReadUInt32();
                var imageOffset = reader.ReadUInt32();

                if (reservedEntryByte != 0)
                {
                    return Invalid("An icon directory entry has a non-zero reserved byte.");
                }

                if (bytesInResource == 0)
                {
                    return Invalid("An icon image entry has zero bytes.");
                }

                if (imageOffset < directoryLength)
                {
                    return Invalid("An icon image overlaps the icon directory.");
                }

                if (imageOffset > stream.Length || bytesInResource > stream.Length - imageOffset)
                {
                    return Invalid("An icon image entry points outside the file.");
                }

                images.Add(new IconImageEntry(
                    width,
                    height,
                    colorCount,
                    planes,
                    bitCount,
                    bytesInResource,
                    imageOffset));
            }

            if (!images.Any(image => image.HasUsefulSize))
            {
                return Invalid("The icon does not contain a common Windows icon size.");
            }

            return OperationResult<IconValidationResult>.Success(new IconValidationResult(
                fullPath,
                stream.Length,
                images));
        }
        catch (UnauthorizedAccessException ex)
        {
            return OperationResult<IconValidationResult>.Failure(new IconReplacerError(
                ErrorCode.PermissionDenied,
                "The icon file could not be read.",
                ex.Message));
        }
        catch (IOException ex)
        {
            return OperationResult<IconValidationResult>.Failure(new IconReplacerError(
                ErrorCode.Unknown,
                "The icon file could not be read.",
                ex.Message));
        }

        static OperationResult<IconValidationResult> Invalid(string message)
        {
            return OperationResult<IconValidationResult>.Failure(new IconReplacerError(ErrorCode.InvalidIcon, message));
        }
    }

    private static int DecodeIconDimension(byte value)
    {
        return value == 0 ? 256 : value;
    }
}

