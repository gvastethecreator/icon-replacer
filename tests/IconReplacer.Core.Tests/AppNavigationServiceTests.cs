using IconReplacer.AppModel;

namespace IconReplacer.Core.Tests;

public sealed class AppNavigationServiceTests
{
    [Fact]
    public void GetPlanListsTopLevelWinUiRoutes()
    {
        var plan = new AppNavigationService().GetPlan();

        Assert.Equal(AppNavigationRouteIds.Home, plan.DefaultRouteId);
        Assert.Equal(AppNavigationRouteIds.Diagnostics, plan.FallbackRouteId);
        Assert.Contains(plan.Routes, route =>
            route.RouteId == AppNavigationRouteIds.Home &&
            route.IsTopLevel &&
            route.PrimaryCommands.Contains("Import icons"));
        Assert.Contains(plan.Routes, route =>
            route.RouteId == AppNavigationRouteIds.IconBrowser &&
            route.IsTopLevel &&
            route.Section == AppNavigationSection.Library);
        Assert.Contains(plan.Routes, route =>
            route.RouteId == AppNavigationRouteIds.Collections &&
            route.IsTopLevel);
        Assert.Contains(plan.Routes, route =>
            route.RouteId == AppNavigationRouteIds.History &&
            route.IsTopLevel &&
            route.DefaultHistoryFilter == RestoreHistoryFilter.All);
        Assert.Contains(plan.Routes, route =>
            route.RouteId == AppNavigationRouteIds.Diagnostics &&
            route.IsTopLevel);
    }

    [Fact]
    public void GetPlanRegistersEveryAppActionTarget()
    {
        var plan = new AppNavigationService().GetPlan();
        var routeIds = plan.Routes.Select(route => route.RouteId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var actionKind in Enum.GetValues<AppActionKind>())
        {
            Assert.True(plan.ActionTargets.TryGetValue(actionKind, out var routeId), $"{actionKind} has no route target.");
            Assert.True(routeIds.Contains(routeId), $"{actionKind} targets missing route {routeId}.");
        }

        Assert.Equal(AppNavigationRouteIds.ImportIcons, plan.ActionTargets[AppActionKind.ImportIcons]);
        Assert.Equal(AppNavigationRouteIds.ShellPlan, plan.ActionTargets[AppActionKind.ShowShellIntegrationPlan]);
        Assert.Equal(AppNavigationRouteIds.PackagePlan, plan.ActionTargets[AppActionKind.ShowPackagePlan]);
    }

    [Fact]
    public void GetPlanIncludesWorkflowRoutesForShellRestoreAndProof()
    {
        var plan = new AppNavigationService().GetPlan();

        Assert.Contains(plan.Routes, route =>
            route.RouteId == AppNavigationRouteIds.ChangeIcon &&
            route.RequiresSelection &&
            route.PrimaryCommands.Contains("Preview change"));
        Assert.Contains(plan.Routes, route =>
            route.RouteId == AppNavigationRouteIds.RestorePreview &&
            route.RequiresSelection &&
            route.PrimaryCommands.Contains("Confirm restore"));
        Assert.Contains(plan.Routes, route =>
            route.RouteId == AppNavigationRouteIds.PackagePlan &&
            !route.IsTopLevel &&
            route.Section == AppNavigationSection.Setup);
        Assert.Contains(plan.Routes, route =>
            route.RouteId == AppNavigationRouteIds.ShellBridge &&
            !route.IsTopLevel &&
            route.PrimaryCommands.Contains("Review resolved commands"));
        Assert.Contains(plan.Routes, route =>
            route.RouteId == AppNavigationRouteIds.AccessibilityPlan &&
            route.PrimaryCommands.Contains("Capture manual proof"));
    }
}
