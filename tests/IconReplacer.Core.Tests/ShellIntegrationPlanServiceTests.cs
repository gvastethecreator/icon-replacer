using IconReplacer.AppModel;

namespace IconReplacer.Core.Tests;

public sealed class ShellIntegrationPlanServiceTests
{
    [Fact]
    public void GetPlanSelectsPackagedDualIntegrationForV1()
    {
        var plan = new ShellIntegrationPlanService().GetPlan(
            ShellIntegrationReadiness.NotConfigured,
            new WinUiToolingSnapshot(
                IsChecked: true,
                WinUiTemplatesAvailable: true,
                WinAppAvailable: true,
                "templates ready",
                "winapp ready"));

        Assert.True(plan.DecisionFinal);
        Assert.Equal(ShellIntegrationMode.PackagedDualExplorerCommands, plan.SelectedMode);
        Assert.Equal(ShellIntegrationMode.None, plan.FallbackMode);
        Assert.Equal(ShellIntegrationPlanService.PackagedDualModeName, plan.SelectedModeName);
        Assert.Equal(ShellIntegrationPlanService.NoFallbackModeName, plan.FallbackModeName);
        Assert.False(plan.HasBlockingIssues);
        Assert.Contains(plan.Items, item =>
            item.Id == "decision" && item.Status == AppDiagnosticStatus.Pass && item.RequiredForV1);
        Assert.Contains(plan.Items, item =>
            item.Id == "manifest-contract" && item.Status == AppDiagnosticStatus.Pass && item.RequiredForV1);
        Assert.Contains(plan.Items, item =>
            item.Id == "classic-handler" && item.Status == AppDiagnosticStatus.Pass && item.RequiredForV1);
        Assert.DoesNotContain(plan.Items, item => item.Id == "classic-fallback");
        Assert.Contains(plan.Items, item =>
            item.Id == "explorer-registration" && item.Status == AppDiagnosticStatus.Warning);
    }

    [Fact]
    public void GetPlanTreatsPendingDecisionAsStaleReadiness()
    {
        var plan = new ShellIntegrationPlanService().GetPlan(ShellIntegrationReadiness.DecisionPending);

        Assert.True(plan.DecisionFinal);
        var registration = Assert.Single(plan.Items, item => item.Id == "explorer-registration");
        Assert.Equal(AppDiagnosticStatus.Warning, registration.Status);
        Assert.Contains("stale", registration.Title, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetPlanReportsMissingWinAppAsBlockingForModernPath()
    {
        var plan = new ShellIntegrationPlanService().GetPlan(
            ShellIntegrationReadiness.NotConfigured,
            new WinUiToolingSnapshot(
                IsChecked: true,
                WinUiTemplatesAvailable: true,
                WinAppAvailable: false,
                "templates ready",
                "winapp missing"));

        Assert.True(plan.HasBlockingIssues);
        Assert.Contains(plan.Items, item =>
            item.Id == "winapp" &&
            item.Status == AppDiagnosticStatus.Blocking &&
            item.RequiredForV1);
    }

    [Fact]
    public void GetPlanReportsConfiguredExplorerRegistration()
    {
        var plan = new ShellIntegrationPlanService().GetPlan(
            ShellIntegrationReadiness.Configured,
            new WinUiToolingSnapshot(
                IsChecked: true,
                WinUiTemplatesAvailable: true,
                WinAppAvailable: true,
                "templates ready",
                "winapp ready"));

        Assert.Contains(plan.Items, item =>
            item.Id == "explorer-registration" &&
            item.Status == AppDiagnosticStatus.Pass);
        Assert.Equal(ShellIntegrationReadiness.Configured, plan.Readiness);
    }
}
