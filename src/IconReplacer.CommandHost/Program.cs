using IconReplacer.AppModel;
using IconReplacer.Core;
using System.Runtime.InteropServices;

namespace IconReplacer.CommandHost;

internal static class Program
{
    private const uint MessageBoxError = 0x00000010;

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            return Execute(args);
        }
        catch (Exception ex)
        {
            return Fail("Icon Replacer could not complete the command.", ex.Message);
        }
    }

    private static int Execute(IReadOnlyList<string> args)
    {
        var pathsResult = IconLibraryPaths.FromEnvironment();
        if (!pathsResult.Succeeded || pathsResult.Value is null)
        {
            return Fail(pathsResult.Error);
        }

        var paths = pathsResult.Value;
        var ensure = new IconLibraryService().EnsureLibrary(paths);
        if (!ensure.Succeeded)
        {
            return Fail(ensure.Error);
        }

        var activation = new AppActivationService().Activate(args, paths);
        if (!activation.Succeeded || activation.Value is null)
        {
            return Fail(activation.Error);
        }

        return activation.Value.Kind switch
        {
            AppActivationKind.ChangeIcon => RunChangeIcon(args, activation.Value, paths),
            AppActivationKind.MenuApply => RunMenuApply(args, paths),
            _ => Fail("Icon Replacer received an unsupported Explorer command.", null)
        };
    }

    private static int RunChangeIcon(
        IReadOnlyList<string> args,
        AppActivationSnapshot activation,
        IconLibraryPaths paths)
    {
        if (activation.LaunchRequest?.PickerRequest is not { CanOpenPicker: true } picker)
        {
            return Fail(activation.Error);
        }

        var selectedIconPath = NativeIconFileDialog.PickFiles(
            ownerWindow: nint.Zero,
            picker.FileExtensions,
            picker.InitialDirectory,
            "Change icon",
            allowMultiple: false).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(selectedIconPath))
        {
            return 0;
        }

        var apply = new ActivatedIconChangeService().ApplySelectedIcon(
            args,
            selectedIconPath,
            paths);
        return apply.Succeeded ? 0 : Fail(apply.Error);
    }

    private static int RunMenuApply(
        IReadOnlyList<string> args,
        IconLibraryPaths paths)
    {
        var apply = new ActivatedMenuApplyService().ApplyActivation(args, paths);
        return apply.Succeeded ? 0 : Fail(apply.Error);
    }

    private static int Fail(IconReplacerError error)
    {
        return Fail(error.Message, error.Detail);
    }

    private static int Fail(string message, string? detail)
    {
        var body = string.IsNullOrWhiteSpace(detail)
            ? message
            : $"{message}\n\n{detail}";
        MessageBox(nint.Zero, body, "Icon Replacer", MessageBoxError);
        return 1;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(
        nint window,
        string text,
        string caption,
        uint type);
}
