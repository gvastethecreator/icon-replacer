using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class ReleaseReadinessServiceTests
{
    [Fact]
    public void GetReadinessReportsMissingReleaseEvidenceAndPackageBlockers()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Work", "blue.ico"));

        var readiness = new ReleaseReadinessService().GetReadiness(
            paths,
            ReleaseReadinessInputs.FromTooling(MissingWinAppTooling()));

        Assert.True(readiness.Succeeded, readiness.Error.Message);
        Assert.NotNull(readiness.Value);
        Assert.False(readiness.Value.IsReadyForRelease);
        Assert.Contains(readiness.Value.Items, item =>
            item.Id == "package-plan" &&
            item.Status == AppDiagnosticStatus.Blocking);
        Assert.Contains(readiness.Value.Items, item =>
            item.Id == "manual-explorer-proof" &&
            item.Status == AppDiagnosticStatus.Blocking);
        Assert.Contains(readiness.Value.Items, item =>
            item.Id == "accessibility-proof" &&
            item.Status == AppDiagnosticStatus.Blocking);
        Assert.True(readiness.Value.BlockingCount >= 1);
    }

    [Fact]
    public void GetReadinessPassesWhenAllRequiredProofAndPackageGatesAreProvided()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Work", "blue.ico"));
        var diagnostics = new AppDiagnosticsService(
            setupReadinessService: new SetupReadinessService(
                shellIntegrationReadiness: ShellIntegrationReadiness.Configured));
        var service = new ReleaseReadinessService(diagnosticsService: diagnostics);

        var readiness = service.GetReadiness(paths, AllProofReadyInputs());

        Assert.True(readiness.Succeeded, readiness.Error.Message);
        Assert.NotNull(readiness.Value);
        Assert.True(readiness.Value.IsReadyForRelease);
        Assert.Equal(0, readiness.Value.BlockingCount);
        Assert.Equal(0, readiness.Value.WarningCount);
        Assert.Contains(readiness.Value.Items, item =>
            item.Id == "package-plan" &&
            item.Status == AppDiagnosticStatus.Pass);
        Assert.Contains(readiness.Value.Items, item =>
            item.Id == "release-evidence-packet" &&
            item.Status == AppDiagnosticStatus.Pass);
    }

    private static ReleaseReadinessInputs AllProofReadyInputs()
    {
        return new ReleaseReadinessInputs(
            new PackagingPlanInputs(
                ReadyTooling(),
                ReadyNativeTooling(),
                PackageIdentityBuilt: true,
                NativeShellExtensionBuilt: true,
                DevSigningAvailable: true,
                InstallerBuilt: true,
                InstallProofCaptured: true,
                UninstallProofCaptured: true),
            BuildPassed: true,
            TestsPassed: true,
            CliProofCaptured: true,
            ManualExplorerProofCaptured: true,
            AccessibilityProofCaptured: true,
            ReleaseEvidenceCaptured: true);
    }

    private static NativeToolingSnapshot ReadyNativeTooling()
    {
        return new NativeToolingSnapshot(
            IsChecked: true,
            CompilerAvailable: true,
            MsBuildAvailable: true,
            CMakeAvailable: true,
            "cl.exe is available.",
            "Visual Studio MSBuild is available.",
            "CMake is available.");
    }

    private static WinUiToolingSnapshot ReadyTooling()
    {
        return new WinUiToolingSnapshot(
            IsChecked: true,
            WinUiTemplatesAvailable: true,
            WinAppAvailable: true,
            "WinUI templates are available.",
            "winapp CLI is available.");
    }

    private static WinUiToolingSnapshot MissingWinAppTooling()
    {
        return new WinUiToolingSnapshot(
            IsChecked: true,
            WinUiTemplatesAvailable: true,
            WinAppAvailable: false,
            "WinUI templates are available.",
            "winapp CLI is missing.");
    }
}
