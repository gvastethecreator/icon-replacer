using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable CA1416 // Every public COM entry point returns before these calls on non-Windows.

namespace IconReplacer.Core;

public sealed class ShellLinkClient
{
    private const int MaxPath = 260;
    private const int MaxDescription = 1024;

    public OperationResult<ShellLinkInfo> Read(string shortcutPath)
    {
        if (!EnsureWindows(out var error))
        {
            return OperationResult<ShellLinkInfo>.Failure(error);
        }

        var pathResult = NormalizeShortcutPath(shortcutPath, requireExists: true);
        if (!pathResult.Succeeded || pathResult.Value is null)
        {
            return OperationResult<ShellLinkInfo>.Failure(pathResult.Error);
        }

        object? shellLink = null;

        try
        {
            shellLink = CreateAndLoad(pathResult.Value);
            var link = (IShellLinkW)shellLink;

            return OperationResult<ShellLinkInfo>.Success(ReadInfo(pathResult.Value, link));
        }
        catch (UnauthorizedAccessException ex)
        {
            return OperationResult<ShellLinkInfo>.Failure(new IconReplacerError(
                ErrorCode.PermissionDenied,
                "The shortcut could not be read.",
                ex.Message));
        }
        catch (COMException ex)
        {
            return OperationResult<ShellLinkInfo>.Failure(new IconReplacerError(
                ErrorCode.Unknown,
                "The shortcut could not be read.",
                ex.Message));
        }
        finally
        {
            Release(shellLink);
        }
    }

    public OperationResult<ShellLinkInfo> CreateOrUpdate(ShellLinkInfo info)
    {
        if (!EnsureWindows(out var error))
        {
            return OperationResult<ShellLinkInfo>.Failure(error);
        }

        var pathResult = NormalizeShortcutPath(info.FullPath, requireExists: false);
        if (!pathResult.Succeeded || pathResult.Value is null)
        {
            return OperationResult<ShellLinkInfo>.Failure(pathResult.Error);
        }

        object? shellLink = null;

        try
        {
            shellLink = Activator.CreateInstance(Type.GetTypeFromCLSID(ShellLinkClassId, throwOnError: true)!)!;
            var link = (IShellLinkW)shellLink;
            link.SetPath(info.TargetPath);
            link.SetArguments(info.Arguments);
            link.SetWorkingDirectory(info.WorkingDirectory);
            link.SetDescription(info.Description);
            link.SetHotkey(info.Hotkey);

            if (!string.IsNullOrWhiteSpace(info.IconPath))
            {
                link.SetIconLocation(info.IconPath, info.IconIndex);
            }

            ((IPersistFile)shellLink).Save(pathResult.Value, remember: true);

            return OperationResult<ShellLinkInfo>.Success(ReadInfo(pathResult.Value, link));
        }
        catch (UnauthorizedAccessException ex)
        {
            return OperationResult<ShellLinkInfo>.Failure(new IconReplacerError(
                ErrorCode.PermissionDenied,
                "The shortcut could not be saved.",
                ex.Message));
        }
        catch (COMException ex)
        {
            return OperationResult<ShellLinkInfo>.Failure(new IconReplacerError(
                ErrorCode.Unknown,
                "The shortcut could not be saved.",
                ex.Message));
        }
        finally
        {
            Release(shellLink);
        }
    }

    public OperationResult<ShellLinkInfo> SetIconLocation(string shortcutPath, string? iconPath, int iconIndex)
    {
        if (!EnsureWindows(out var error))
        {
            return OperationResult<ShellLinkInfo>.Failure(error);
        }

        var pathResult = NormalizeShortcutPath(shortcutPath, requireExists: true);
        if (!pathResult.Succeeded || pathResult.Value is null)
        {
            return OperationResult<ShellLinkInfo>.Failure(pathResult.Error);
        }

        object? shellLink = null;

        try
        {
            shellLink = CreateAndLoad(pathResult.Value);
            var link = (IShellLinkW)shellLink;
            link.SetIconLocation(iconPath ?? string.Empty, iconIndex);
            ((IPersistFile)shellLink).Save(pathResult.Value, remember: true);

            return OperationResult<ShellLinkInfo>.Success(ReadInfo(pathResult.Value, link));
        }
        catch (UnauthorizedAccessException ex)
        {
            return OperationResult<ShellLinkInfo>.Failure(new IconReplacerError(
                ErrorCode.PermissionDenied,
                "The shortcut icon could not be changed.",
                ex.Message));
        }
        catch (COMException ex)
        {
            return OperationResult<ShellLinkInfo>.Failure(new IconReplacerError(
                ErrorCode.Unknown,
                "The shortcut icon could not be changed.",
                ex.Message));
        }
        finally
        {
            Release(shellLink);
        }
    }

    private static OperationResult<string> NormalizeShortcutPath(string shortcutPath, bool requireExists)
    {
        if (FileSystemPathPolicy.IsRemoteOrUnsupported(shortcutPath))
        {
            return OperationResult<string>.Failure(new IconReplacerError(
                ErrorCode.RemotePathUnsupported,
                "Remote or web-backed shortcuts are not supported in V1."));
        }

        var targetResult = TargetItem.FromShellSelection(shortcutPath, isDirectory: false);
        if (!targetResult.Succeeded || targetResult.Value is null)
        {
            return OperationResult<string>.Failure(targetResult.Error);
        }

        if (requireExists && !File.Exists(targetResult.Value.FullPath))
        {
            return OperationResult<string>.Failure(new IconReplacerError(
                ErrorCode.PathNotFound,
                "The shortcut target does not exist.",
                targetResult.Value.FullPath));
        }

        return OperationResult<string>.Success(targetResult.Value.FullPath);
    }

    private static object CreateAndLoad(string shortcutPath)
    {
        var shellLink = Activator.CreateInstance(Type.GetTypeFromCLSID(ShellLinkClassId, throwOnError: true)!)!;
        ((IPersistFile)shellLink).Load(shortcutPath, 0);
        return shellLink;
    }

    private static ShellLinkInfo ReadInfo(string shortcutPath, IShellLinkW link)
    {
        var target = new StringBuilder(MaxPath);
        link.GetPath(target, target.Capacity, IntPtr.Zero, 0);

        var arguments = new StringBuilder(MaxPath);
        link.GetArguments(arguments, arguments.Capacity);

        var workingDirectory = new StringBuilder(MaxPath);
        link.GetWorkingDirectory(workingDirectory, workingDirectory.Capacity);

        var description = new StringBuilder(MaxDescription);
        link.GetDescription(description, description.Capacity);

        link.GetHotkey(out var hotkey);

        var iconPath = new StringBuilder(MaxPath);
        link.GetIconLocation(iconPath, iconPath.Capacity, out var iconIndex);

        return new ShellLinkInfo(
            shortcutPath,
            target.ToString(),
            arguments.ToString(),
            workingDirectory.ToString(),
            description.ToString(),
            hotkey,
            string.IsNullOrWhiteSpace(iconPath.ToString()) ? null : iconPath.ToString(),
            iconIndex);
    }

    private static bool EnsureWindows(out IconReplacerError error)
    {
        if (OperatingSystem.IsWindows())
        {
            error = IconReplacerError.None;
            return true;
        }

        error = new IconReplacerError(
            ErrorCode.UnsupportedTarget,
            ".lnk shortcut support is only available on Windows.");
        return false;
    }

    private static void Release(object? shellLink)
    {
        if (shellLink is not null && Marshal.IsComObject(shellLink))
        {
            Marshal.FinalReleaseComObject(shellLink);
        }
    }

    private static readonly Guid ShellLinkClassId = new("00021401-0000-0000-C000-000000000046");

    [ComImport]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellLinkW
    {
        void GetPath(
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile,
            int cchMaxPath,
            IntPtr pfd,
            uint fFlags);

        void GetIDList(out IntPtr ppidl);

        void SetIDList(IntPtr pidl);

        void GetDescription(
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName,
            int cchMaxName);

        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

        void GetWorkingDirectory(
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir,
            int cchMaxPath);

        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

        void GetArguments(
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs,
            int cchMaxPath);

        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

        void GetHotkey(out short pwHotkey);

        void SetHotkey(short wHotkey);

        void GetShowCmd(out int piShowCmd);

        void SetShowCmd(int iShowCmd);

        void GetIconLocation(
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath,
            int cchIconPath,
            out int piIcon);

        void SetIconLocation(
            [MarshalAs(UnmanagedType.LPWStr)] string pszIconPath,
            int iIcon);

        void SetRelativePath(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPathRel,
            uint dwReserved);

        void Resolve(IntPtr hwnd, uint fFlags);

        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [Guid("0000010B-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);

        void IsDirty();

        void Load(
            [MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
            uint dwMode);

        void Save(
            [MarshalAs(UnmanagedType.LPWStr)] string? pszFileName,
            [MarshalAs(UnmanagedType.Bool)] bool remember);

        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);

        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }
}

#pragma warning restore CA1416
