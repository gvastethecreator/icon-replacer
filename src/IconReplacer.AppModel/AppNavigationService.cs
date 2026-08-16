namespace IconReplacer.AppModel;

public sealed class AppNavigationService
{
    public AppNavigationPlanSnapshot GetPlan()
    {
        return new AppNavigationPlanSnapshot(
            GetRoutes(),
            GetActionTargets(),
            AppNavigationRouteIds.Home,
            AppNavigationRouteIds.Diagnostics,
            DateTimeOffset.UtcNow);
    }

    private static IReadOnlyList<AppNavigationRoute> GetRoutes()
    {
        return new[]
        {
            Route(
                AppNavigationRouteIds.Home,
                "Home",
                AppNavigationSection.Home,
                "First screen with readiness, setup actions, locations, menu counts, and history counts.",
                isTopLevel: true,
                requiresSelection: false,
                defaultHistoryFilter: null,
                "Import icons",
                "Browse Icon Library",
                "Recent changes",
                "Diagnostics"),
            Route(
                AppNavigationRouteIds.IconBrowser,
                "Icon Browser",
                AppNavigationSection.Library,
                "Search, category filters, warning review, icon details, and icon selection.",
                isTopLevel: true,
                requiresSelection: false,
                defaultHistoryFilter: null,
                "Search icons",
                "Filter by category",
                "Open icon details",
                "Import icons"),
            Route(
                AppNavigationRouteIds.IconDetails,
                "Icon Details",
                AppNavigationSection.Library,
                "Selected icon metadata, image entries, and recommended image.",
                isTopLevel: false,
                requiresSelection: true,
                defaultHistoryFilter: null,
                "Use selected icon",
                "Copy icon path",
                "Open containing folder"),
            Route(
                AppNavigationRouteIds.ImportIcons,
                "Import Icons",
                AppNavigationSection.Library,
                "Multi-select .ico import workflow for Imported or user-created collections.",
                isTopLevel: false,
                requiresSelection: false,
                defaultHistoryFilter: null,
                "Choose .ico files",
                "Choose collection",
                "Review import results"),
            Route(
                AppNavigationRouteIds.Collections,
                "Collections",
                AppNavigationSection.Library,
                "One-level Icon Library folders, create-collection actions, and collection imports.",
                isTopLevel: true,
                requiresSelection: false,
                defaultHistoryFilter: null,
                "Create collection",
                "Import into collection",
                "Open collection"),
            Route(
                AppNavigationRouteIds.History,
                "Recent Changes",
                AppNavigationSection.History,
                "Filtered restore history with enabled, warning, and disabled restore actions.",
                isTopLevel: true,
                requiresSelection: false,
                defaultHistoryFilter: RestoreHistoryFilter.All,
                "Filter history",
                "Preview restore",
                "Restore selected change"),
            Route(
                AppNavigationRouteIds.RestorePreview,
                "Restore Preview",
                AppNavigationSection.History,
                "Confirmation state before restore mutation.",
                isTopLevel: false,
                requiresSelection: true,
                defaultHistoryFilter: null,
                "Confirm restore",
                "Cancel restore",
                "Review disabled reason"),
            Route(
                AppNavigationRouteIds.Diagnostics,
                "Diagnostics",
                AppNavigationSection.Diagnostics,
                "Readiness, blockers, app locations, shell status, and tooling checks.",
                isTopLevel: true,
                requiresSelection: false,
                defaultHistoryFilter: null,
                "Refresh diagnostics",
                "Open app data",
                "Review blockers"),
            Route(
                AppNavigationRouteIds.ShellPlan,
                "Shell Integration Plan",
                AppNavigationSection.Setup,
                "Packaged modern and classic Explorer paths, prerequisites, and manifest contract.",
                isTopLevel: false,
                requiresSelection: false,
                defaultHistoryFilter: null,
                "Review shell bridge",
                "Review manifest contract",
                "Review package plan",
                "Open diagnostics"),
            Route(
                AppNavigationRouteIds.ShellBridge,
                "Shell Bridge",
                AppNavigationSection.Setup,
                "Native Explorer command bridge preview, resolved arguments, menu caps, and safety rules.",
                isTopLevel: false,
                requiresSelection: false,
                defaultHistoryFilter: null,
                "Review resolved commands",
                "Review safety rules",
                "Review shell plan",
                "Open diagnostics"),
            Route(
                AppNavigationRouteIds.PackagePlan,
                "Package Plan",
                AppNavigationSection.Setup,
                "Install, uninstall, signing, proof, and data-preservation gates.",
                isTopLevel: false,
                requiresSelection: false,
                defaultHistoryFilter: null,
                "Review install blockers",
                "Review uninstall policy",
                "Open diagnostics"),
            Route(
                AppNavigationRouteIds.AccessibilityPlan,
                "Accessibility Plan",
                AppNavigationSection.Diagnostics,
                "Keyboard, names, visual adaptation, and manual proof requirements.",
                isTopLevel: false,
                requiresSelection: false,
                defaultHistoryFilter: null,
                "Review requirements",
                "Review app surfaces",
                "Capture manual proof"),
            Route(
                AppNavigationRouteIds.ChangeIcon,
                "Change Icon",
                AppNavigationSection.Library,
                "Explorer-launched target validation, icon picker, preview, and apply workflow.",
                isTopLevel: false,
                requiresSelection: true,
                defaultHistoryFilter: null,
                "Choose icon",
                "Preview change",
                "Apply change")
        };
    }

    private static IReadOnlyDictionary<AppActionKind, string> GetActionTargets()
    {
        return new Dictionary<AppActionKind, string>
        {
            [AppActionKind.ImportIcons] = AppNavigationRouteIds.ImportIcons,
            [AppActionKind.ShowIconBrowser] = AppNavigationRouteIds.IconBrowser,
            [AppActionKind.ShowRestoreHistory] = AppNavigationRouteIds.History,
            [AppActionKind.ShowDiagnostics] = AppNavigationRouteIds.Diagnostics,
            [AppActionKind.ShowShellIntegrationPlan] = AppNavigationRouteIds.ShellPlan,
            [AppActionKind.ShowPackagePlan] = AppNavigationRouteIds.PackagePlan
        };
    }

    private static AppNavigationRoute Route(
        string routeId,
        string title,
        AppNavigationSection section,
        string purpose,
        bool isTopLevel,
        bool requiresSelection,
        RestoreHistoryFilter? defaultHistoryFilter,
        params string[] primaryCommands)
    {
        return new AppNavigationRoute(
            routeId,
            title,
            section,
            purpose,
            primaryCommands,
            isTopLevel,
            requiresSelection,
            defaultHistoryFilter);
    }
}
