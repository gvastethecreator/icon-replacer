using System.Diagnostics;

namespace IconReplacer.AppModel;

public static class NativeToolingDetector
{
    public static NativeToolingSnapshot Detect()
    {
        var installationPath = FindVisualStudioInstallationPath();
        var compilerPath = FindOnPath("cl.exe") ??
            FindPreferredFile(installationPath, "cl.exe", "Hostx64", "x64");
        var msBuildPath = FindOnPath("msbuild.exe") ??
            Existing(Path.Combine(
                installationPath ?? string.Empty,
                "MSBuild",
                "Current",
                "Bin",
                "amd64",
                "MSBuild.exe")) ??
            FindPreferredFile(installationPath, "MSBuild.exe", "MSBuild", "Current");
        var cmakePath = FindOnPath("cmake.exe") ??
            Existing(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "CMake",
                "bin",
                "cmake.exe")) ??
            Existing(Path.Combine(
                installationPath ?? string.Empty,
                "Common7",
                "IDE",
                "CommonExtensions",
                "Microsoft",
                "CMake",
                "CMake",
                "bin",
                "cmake.exe")) ??
            FindPreferredFile(installationPath, "cmake.exe", "CMake\\bin", "Microsoft\\CMake");

        return new NativeToolingSnapshot(
            IsChecked: true,
            compilerPath is not null,
            msBuildPath is not null,
            cmakePath is not null,
            Detail("cl.exe", compilerPath),
            Detail("Visual Studio MSBuild", msBuildPath),
            Detail("CMake", cmakePath));
    }

    private static string Detail(string tool, string? path)
    {
        return path is null ? $"{tool} was not found." : $"{tool} is available ({path}).";
    }

    private static string? FindOnPath(string fileName)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return path
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(folder => Existing(Path.Combine(folder.Trim('"'), fileName)))
            .FirstOrDefault(candidate => candidate is not null);
    }

    private static string? FindVisualStudioInstallationPath()
    {
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var vsWherePath = Path.Combine(
            programFilesX86,
            "Microsoft Visual Studio",
            "Installer",
            "vswhere.exe");
        if (File.Exists(vsWherePath))
        {
            var result = RunProcess(
                vsWherePath,
                "-latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath");
            var path = result
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault(Directory.Exists);
            if (path is not null)
            {
                return path;
            }
        }

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        return new[]
        {
            Path.Combine(programFilesX86, "Microsoft Visual Studio", "2022", "BuildTools"),
            Path.Combine(programFiles, "Microsoft Visual Studio", "18", "Community")
        }.FirstOrDefault(Directory.Exists);
    }

    private static string? FindPreferredFile(
        string? root,
        string fileName,
        string preferredSegment,
        string secondaryPreferredSegment)
    {
        if (root is null)
        {
            return null;
        }

        try
        {
            return Directory
                .EnumerateFiles(root, fileName, SearchOption.AllDirectories)
                .OrderByDescending(path => path.Contains(preferredSegment, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(path => path.Contains(secondaryPreferredSegment, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string RunProcess(string fileName, string arguments)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
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
                return string.Empty;
            }

            var output = process.StandardOutput.ReadToEnd();
            return process.WaitForExit(5000) ? output : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string? Existing(string path)
    {
        return File.Exists(path) ? path : null;
    }
}
