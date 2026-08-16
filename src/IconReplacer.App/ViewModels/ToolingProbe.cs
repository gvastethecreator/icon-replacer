using IconReplacer.AppModel;

namespace IconReplacer.App.ViewModels;

internal static class ToolingProbe
{
    public static PackagingPlanInputs GetPackagingInputs()
    {
        return PackagingPlanInputs.FromTooling(DetectWinUiTooling(), NativeToolingDetector.Detect()) with
        {
            NativeShellExtensionBuilt = NativeShellExtensionExists()
        };
    }

    private static WinUiToolingSnapshot DetectWinUiTooling()
    {
        var templatesAvailable = RunProcess("dotnet", "new list winui").ExitCode == 0;
        var winAppPath = FindOnPath("winapp.exe") ?? FindWindowsAppsTool("winapp.exe");

        return new WinUiToolingSnapshot(
            IsChecked: true,
            templatesAvailable,
            winAppPath is not null,
            templatesAvailable
                ? "WinUI templates are available."
                : "WinUI templates were not found.",
            winAppPath is not null
                ? $"winapp CLI is available ({winAppPath})."
                : "winapp CLI was not found.");
    }

    private static string? FindOnPath(string fileName)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        foreach (var folder in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(folder.Trim(), fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string? FindWindowsAppsTool(string fileName)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return string.IsNullOrWhiteSpace(localAppData)
            ? null
            : Existing(Path.Combine(localAppData, "Microsoft", "WindowsApps", fileName));
    }

    private static string? Existing(string path)
    {
        return File.Exists(path) ? path : null;
    }

    private static (int ExitCode, string Output) RunProcess(string fileName, string arguments)
    {
        try
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });

            if (process is null)
            {
                return (-1, string.Empty);
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);
            return (process.ExitCode, output);
        }
        catch
        {
            return (-1, string.Empty);
        }
    }

    private static bool NativeShellExtensionExists()
    {
        return CandidateRepositoryRoots().Any(root =>
            File.Exists(Path.Combine(root, "artifacts", "native", "x64", "Debug", "IconReplacer.ShellExtension.dll")) ||
            File.Exists(Path.Combine(root, "artifacts", "native", "x64", "Release", "IconReplacer.ShellExtension.dll")));
    }

    private static IEnumerable<string> CandidateRepositoryRoots()
    {
        yield return Directory.GetCurrentDirectory();

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "IconReplacer.slnx")))
            {
                yield return directory.FullName;
            }

            directory = directory.Parent;
        }
    }
}
