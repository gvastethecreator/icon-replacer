using System.Runtime.InteropServices;

namespace IconReplacer.App;

internal static class NativeIconFileDialog
{
    private const int BufferCharCount = 65536;
    private const int OfnAllowMultiSelect = 0x00000200;
    private const int OfnExplorer = 0x00080000;
    private const int OfnFileMustExist = 0x00001000;
    private const int OfnHideReadOnly = 0x00000004;
    private const int OfnNoChangeDir = 0x00000008;
    private const int OfnPathMustExist = 0x00000800;
    private const int OfnDontAddToRecent = 0x02000000;
    private const int OfnEnableSizing = 0x00800000;

    public static IReadOnlyList<string> PickFiles(
        nint ownerWindow,
        IReadOnlyList<string> fileExtensions,
        string initialDirectory,
        string title,
        bool allowMultiple)
    {
        var fileBuffer = Marshal.AllocHGlobal(BufferCharCount * sizeof(char));
        var filterBuffer = nint.Zero;
        var initialDirectoryBuffer = nint.Zero;
        var titleBuffer = nint.Zero;
        var defaultExtensionBuffer = nint.Zero;
        try
        {
            var empty = new byte[BufferCharCount * sizeof(char)];
            Marshal.Copy(empty, 0, fileBuffer, empty.Length);
            var effectiveInitialDirectory = Directory.Exists(initialDirectory)
                ? initialDirectory
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            filterBuffer = Marshal.StringToHGlobalUni(BuildFilter(fileExtensions));
            initialDirectoryBuffer = Marshal.StringToHGlobalUni(effectiveInitialDirectory);
            titleBuffer = Marshal.StringToHGlobalUni(title);
            defaultExtensionBuffer = StringToHGlobalUniOrZero(GetDefaultExtension(fileExtensions));

            var flags = OfnExplorer |
                OfnFileMustExist |
                OfnPathMustExist |
                OfnHideReadOnly |
                OfnNoChangeDir |
                OfnDontAddToRecent |
                OfnEnableSizing;
            if (allowMultiple)
            {
                flags |= OfnAllowMultiSelect;
            }

            var dialog = new OpenFileName
            {
                StructSize = Marshal.SizeOf<OpenFileName>(),
                Owner = ownerWindow,
                Filter = filterBuffer,
                File = fileBuffer,
                MaxFile = BufferCharCount,
                InitialDirectory = initialDirectoryBuffer,
                Title = titleBuffer,
                Flags = flags,
                DefaultExtension = defaultExtensionBuffer
            };

            if (!GetOpenFileName(ref dialog))
            {
                var error = CommDlgExtendedError();
                if (error == 0)
                {
                    return [];
                }

                throw new InvalidOperationException(
                    $"The Windows file dialog failed with error 0x{error:X}.");
            }

            var raw = Marshal.PtrToStringUni(fileBuffer, BufferCharCount) ?? string.Empty;
            return ParseSelectionBuffer(raw, allowMultiple);
        }
        finally
        {
            Marshal.FreeHGlobal(fileBuffer);
            Marshal.FreeHGlobal(filterBuffer);
            Marshal.FreeHGlobal(initialDirectoryBuffer);
            Marshal.FreeHGlobal(titleBuffer);
            Marshal.FreeHGlobal(defaultExtensionBuffer);
        }
    }

    private static IReadOnlyList<string> ParseSelectionBuffer(string raw, bool allowMultiple)
    {
        var parts = raw.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return [];
        }

        if (!allowMultiple || parts.Length == 1)
        {
            return [parts[0]];
        }

        var directory = parts[0];
        return parts
            .Skip(1)
            .Select(fileName => Path.Combine(directory, fileName))
            .ToArray();
    }

    private static string BuildFilter(IReadOnlyList<string> fileExtensions)
    {
        var normalized = NormalizeExtensions(fileExtensions);
        if (normalized.Length == 0)
        {
            return "All files (*.*)\0*.*\0\0";
        }

        var patterns = string.Join(";", normalized.Select(extension => "*" + extension));
        var label = normalized.Length == 1
            ? $"{normalized[0].TrimStart('.').ToUpperInvariant()} files ({patterns})"
            : $"Icon files ({patterns})";
        return $"{label}\0{patterns}\0All files (*.*)\0*.*\0\0";
    }

    private static string? GetDefaultExtension(IReadOnlyList<string> fileExtensions)
    {
        return NormalizeExtensions(fileExtensions)
            .FirstOrDefault()
            ?.TrimStart('.');
    }

    private static string[] NormalizeExtensions(IReadOnlyList<string> fileExtensions)
    {
        return fileExtensions
            .Where(extension => !string.IsNullOrWhiteSpace(extension))
            .Select(extension => extension.StartsWith('.') ? extension : "." + extension)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static nint StringToHGlobalUniOrZero(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? nint.Zero
            : Marshal.StringToHGlobalUni(value);
    }

    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetOpenFileName(ref OpenFileName openFileName);

    [DllImport("comdlg32.dll")]
    private static extern int CommDlgExtendedError();

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OpenFileName
    {
        public int StructSize;
        public nint Owner;
        public nint Instance;
        public nint Filter;
        public nint CustomFilter;
        public int MaxCustomFilter;
        public int FilterIndex;
        public nint File;
        public int MaxFile;
        public nint FileTitle;
        public int MaxFileTitle;
        public nint InitialDirectory;
        public nint Title;
        public int Flags;
        public short FileOffset;
        public short FileExtension;
        public nint DefaultExtension;
        public nint CustomData;
        public nint Hook;
        public nint TemplateName;
        public nint Reserved;
        public int ReservedFlags;
        public int FlagsEx;
    }
}
