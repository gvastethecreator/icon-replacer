using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class SetupReadinessServiceTests
{
    [Fact]
    public void GetSnapshotCreatesLibraryAndReportsEmptyFirstRunActions()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var snapshot = new SetupReadinessService().GetSnapshot(paths);

        Assert.True(snapshot.Succeeded, snapshot.Error.Message);
        Assert.NotNull(snapshot.Value);
        Assert.True(snapshot.Value.IconLibraryExists);
        Assert.True(snapshot.Value.ImportedIconsFolderExists);
        Assert.True(snapshot.Value.CanUseCoreFeatures);
        Assert.False(snapshot.Value.HasIcons);
        Assert.Equal(ShellIntegrationReadiness.NotConfigured, snapshot.Value.ShellIntegration);
        Assert.Contains(snapshot.Value.Actions, action => action.Id == "import-icons");
        Assert.Contains(snapshot.Value.Actions, action => action.Id == "configure-shell-integration");
    }

    [Fact]
    public void GetSnapshotReportsReadyLibraryCounts()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Work", "blue.ico"));

        var snapshot = new SetupReadinessService(
            shellIntegrationReadiness: ShellIntegrationReadiness.NotConfigured).GetSnapshot(paths);

        Assert.True(snapshot.Succeeded, snapshot.Error.Message);
        Assert.NotNull(snapshot.Value);
        Assert.True(snapshot.Value.HasIcons);
        Assert.Equal(2, snapshot.Value.CategoryCount);
        Assert.Equal(1, snapshot.Value.IconCount);
        Assert.DoesNotContain(snapshot.Value.Actions, action => action.Id == "import-icons");
        Assert.Contains(snapshot.Value.Actions, action => action.Id == "configure-shell-integration");
    }

    [Fact]
    public void GetSnapshotAddsPackagePlanActionWhenModernPackagePathIsBlocked()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Work", "blue.ico"));

        var snapshot = new SetupReadinessService().GetSnapshot(
            paths,
            PackagingPlanInputs.FromTooling(MissingWinAppTooling()));

        Assert.True(snapshot.Succeeded, snapshot.Error.Message);
        Assert.NotNull(snapshot.Value);
        var action = Assert.Single(snapshot.Value.Actions, action =>
            action.Id == SetupActionIds.ReviewPackagePlan);
        Assert.Equal(SetupActionSeverity.Blocking, action.Severity);
        Assert.Contains("package blockers", action.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(snapshot.Value.Actions, action => action.Id == SetupActionIds.ConfigureShellIntegration);
    }

    [Fact]
    public void GetSnapshotCanStillReportPendingDecisionWhenInjected()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var snapshot = new SetupReadinessService(
            shellIntegrationReadiness: ShellIntegrationReadiness.DecisionPending).GetSnapshot(paths);

        Assert.True(snapshot.Succeeded, snapshot.Error.Message);
        Assert.NotNull(snapshot.Value);
        Assert.Equal(ShellIntegrationReadiness.DecisionPending, snapshot.Value.ShellIntegration);
        Assert.Contains(snapshot.Value.Actions, action => action.Id == "resolve-shell-integration");
    }

    [Fact]
    public void GetSnapshotReportsCatalogWarningsWithoutBlockingCoreUse()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WritePngHeader(Path.Combine(paths.LibraryRoot, "bad.ico"));

        var snapshot = new SetupReadinessService().GetSnapshot(paths);

        Assert.True(snapshot.Succeeded, snapshot.Error.Message);
        Assert.NotNull(snapshot.Value);
        Assert.True(snapshot.Value.CanUseCoreFeatures);
        Assert.Equal(1, snapshot.Value.CatalogWarningCount);
        Assert.Contains(snapshot.Value.Actions, action => action.Id == "review-catalog-warnings");
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
