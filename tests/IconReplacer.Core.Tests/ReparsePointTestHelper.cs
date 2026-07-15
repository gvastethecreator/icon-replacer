using System.Diagnostics;

namespace IconReplacer.Core.Tests;

internal enum DirectoryLinkKind
{
    Junction,
    SymbolicLink
}

internal static class ReparsePointTestHelper
{
    public static void CreateDirectoryLink(
        DirectoryLinkKind kind,
        string linkPath,
        string targetPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(linkPath)!);

        if (kind == DirectoryLinkKind.SymbolicLink)
        {
            Directory.CreateSymbolicLink(linkPath, targetPath);
            return;
        }

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            ArgumentList =
            {
                "/d",
                "/c",
                "mklink",
                "/J",
                linkPath,
                targetPath
            }
        }) ?? throw new InvalidOperationException("The junction helper could not start.");

        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"The test junction could not be created. {standardOutput} {standardError}".Trim());
        }
    }

    public static void DeleteDirectoryLink(string linkPath)
    {
        if (new DirectoryInfo(linkPath).LinkTarget is not null || Directory.Exists(linkPath))
        {
            Directory.Delete(linkPath);
        }
    }
}
