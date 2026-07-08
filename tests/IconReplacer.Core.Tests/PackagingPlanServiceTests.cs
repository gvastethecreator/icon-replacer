using IconReplacer.AppModel;

namespace IconReplacer.Core.Tests;

public sealed class PackagingPlanServiceTests
{
    [Fact]
    public void GetPlanReportsCurrentMissingPackagingGates()
    {
        var plan = new PackagingPlanService().GetPlan(PackagingPlanInputs.FromTooling(
            new WinUiToolingSnapshot(
                IsChecked: true,
                WinUiTemplatesAvailable: true,
                WinAppAvailable: false,
                "templates ready",
                "winapp missing"),
            MissingNativeTooling()));

        Assert.True(plan.HasBlockingIssues);
        Assert.Equal("Per-user MSIX package", plan.InstallMode);
        Assert.True(plan.RequiresPackageIdentity);
        Assert.True(plan.RequiresDevSigning);
        Assert.Contains(plan.Items, item =>
            item.Id == "winapp" && item.Status == AppDiagnosticStatus.Blocking);
        Assert.Contains(plan.Items, item =>
            item.Id == "native-build-tools" && item.Status == AppDiagnosticStatus.Blocking);
        Assert.Contains(plan.Items, item =>
            item.Id == "package-identity" && item.Status == AppDiagnosticStatus.Blocking);
        Assert.Contains(plan.Items, item =>
            item.Id == "manifest-contract" && item.Status == AppDiagnosticStatus.Pass);
    }

    [Fact]
    public void GetPlanPassesBuildGatesWhenInputsAreReady()
    {
        var plan = new PackagingPlanService().GetPlan(new PackagingPlanInputs(
            new WinUiToolingSnapshot(
                IsChecked: true,
                WinUiTemplatesAvailable: true,
                WinAppAvailable: true,
                "templates ready",
                "winapp ready"),
            ReadyNativeTooling(),
            PackageIdentityBuilt: true,
            NativeShellExtensionBuilt: true,
            DevSigningAvailable: true,
            InstallerBuilt: true,
            InstallProofCaptured: true,
            UninstallProofCaptured: true));

        Assert.False(plan.HasBlockingIssues);
        Assert.Equal(0, plan.WarningCount);
        Assert.All(plan.Items, item => Assert.NotEqual(AppDiagnosticStatus.Blocking, item.Status));
        Assert.All(plan.Items, item => Assert.NotEqual(AppDiagnosticStatus.Warning, item.Status));
    }

    [Fact]
    public void GetPlanPreservesUserDataOnUninstallByDefault()
    {
        var plan = new PackagingPlanService().GetPlan();

        Assert.True(plan.RemovesShellIntegrationOnUninstall);
        Assert.True(plan.PreservesIconLibraryOnUninstall);
        Assert.True(plan.PreservesRestoreHistoryByDefault);
        Assert.Contains(plan.Items, item =>
            item.Id == "uninstall-policy" &&
            item.Status == AppDiagnosticStatus.Pass &&
            item.Detail.Contains(".icons", StringComparison.OrdinalIgnoreCase));
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

    private static NativeToolingSnapshot MissingNativeTooling()
    {
        return new NativeToolingSnapshot(
            IsChecked: true,
            CompilerAvailable: false,
            MsBuildAvailable: false,
            CMakeAvailable: true,
            "cl.exe missing.",
            "MSBuild missing.",
            "CMake available.");
    }
}
