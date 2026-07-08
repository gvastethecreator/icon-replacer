using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class AppDiagnosticsServiceTests
{
    [Fact]
    public void GetDiagnosticsReportsReadyCoreAndPendingShell()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Work", "blue.ico"));

        var diagnostics = new AppDiagnosticsService().GetDiagnostics(
            paths,
            new WinUiToolingSnapshot(
                IsChecked: true,
                WinUiTemplatesAvailable: true,
                WinAppAvailable: true,
                "templates ready",
                "winapp ready"));

        Assert.True(diagnostics.Succeeded, diagnostics.Error.Message);
        Assert.NotNull(diagnostics.Value);
        Assert.False(diagnostics.Value.HasBlockingIssues);
        Assert.Contains(diagnostics.Value.Checks, check =>
            check.Id == "core-features" && check.Status == AppDiagnosticStatus.Pass);
        Assert.Contains(diagnostics.Value.Checks, check =>
            check.Id == "shell-integration" && check.Status == AppDiagnosticStatus.Warning);
    }

    [Fact]
    public void GetDiagnosticsReportsEmptyLibraryAndMissingWinApp()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var diagnostics = new AppDiagnosticsService().GetDiagnostics(
            paths,
            new WinUiToolingSnapshot(
                IsChecked: true,
                WinUiTemplatesAvailable: true,
                WinAppAvailable: false,
                "templates ready",
                "winapp missing"),
            MissingNativeTooling());

        Assert.True(diagnostics.Succeeded, diagnostics.Error.Message);
        Assert.NotNull(diagnostics.Value);
        Assert.True(diagnostics.Value.HasBlockingIssues);
        Assert.Contains(diagnostics.Value.Checks, check =>
            check.Id == "icon-library-content" && check.Status == AppDiagnosticStatus.Warning);
        Assert.Contains(diagnostics.Value.Checks, check =>
            check.Id == "winapp" && check.Status == AppDiagnosticStatus.Blocking);
        Assert.Contains(diagnostics.Value.Checks, check =>
            check.Id == "native-build-tools" && check.Status == AppDiagnosticStatus.Blocking);
    }

    [Fact]
    public void GetDiagnosticsReportsCatalogWarnings()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WritePngHeader(Path.Combine(paths.LibraryRoot, "bad.ico"));

        var diagnostics = new AppDiagnosticsService().GetDiagnostics(paths);

        Assert.True(diagnostics.Succeeded, diagnostics.Error.Message);
        Assert.NotNull(diagnostics.Value);
        Assert.Contains(diagnostics.Value.Checks, check =>
            check.Id == "catalog-warnings" && check.Status == AppDiagnosticStatus.Warning);
        Assert.Contains(diagnostics.Value.Checks, check =>
            check.Id == "winapp" && check.Status == AppDiagnosticStatus.Info);
    }

    private static NativeToolingSnapshot MissingNativeTooling()
    {
        return new NativeToolingSnapshot(
            IsChecked: true,
            CompilerAvailable: false,
            MsBuildAvailable: false,
            CMakeAvailable: true,
            "cl.exe missing.",
            "Visual Studio MSBuild missing.",
            "CMake available.");
    }
}
