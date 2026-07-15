using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class AppDiagnosticsService
{
    private readonly SetupReadinessService _setupReadinessService;
    private readonly DashboardService _dashboardService;
    private readonly AppLocationService _appLocationService;

    public AppDiagnosticsService(
        SetupReadinessService? setupReadinessService = null,
        DashboardService? dashboardService = null,
        AppLocationService? appLocationService = null)
    {
        _dashboardService = dashboardService ?? new DashboardService();
        _setupReadinessService = setupReadinessService ?? new SetupReadinessService(_dashboardService);
        _appLocationService = appLocationService ?? new AppLocationService();
    }

    public OperationResult<AppDiagnosticsSnapshot> GetDiagnostics(
        IconLibraryPaths paths,
        WinUiToolingSnapshot? winUiTooling = null,
        NativeToolingSnapshot? nativeTooling = null)
    {
        var setup = _setupReadinessService.GetSnapshot(paths);
        if (!setup.Succeeded || setup.Value is null)
        {
            return OperationResult<AppDiagnosticsSnapshot>.Failure(setup.Error);
        }

        var dashboard = _dashboardService.GetSnapshot(paths);
        if (!dashboard.Succeeded || dashboard.Value is null)
        {
            return OperationResult<AppDiagnosticsSnapshot>.Failure(dashboard.Error);
        }

        var locations = _appLocationService.GetLocations(paths);
        if (!locations.Succeeded || locations.Value is null)
        {
            return OperationResult<AppDiagnosticsSnapshot>.Failure(locations.Error);
        }

        var tooling = winUiTooling ?? WinUiToolingSnapshot.NotChecked;
        var native = nativeTooling ?? NativeToolingSnapshot.NotChecked;
        var checks = BuildChecks(setup.Value, dashboard.Value, locations.Value, tooling, native);
        return OperationResult<AppDiagnosticsSnapshot>.Success(new AppDiagnosticsSnapshot(
            setup.Value,
            dashboard.Value,
            locations.Value,
            tooling,
            native,
            checks,
            DateTimeOffset.UtcNow));
    }

    public OperationResult<AppDiagnosticsSnapshot> GetDiagnosticsFromEnvironment(
        WinUiToolingSnapshot? winUiTooling = null,
        NativeToolingSnapshot? nativeTooling = null)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<AppDiagnosticsSnapshot>.Failure(paths.Error);
        }

        return GetDiagnostics(paths.Value, winUiTooling, nativeTooling);
    }

    private static IReadOnlyList<AppDiagnosticCheck> BuildChecks(
        SetupReadinessSnapshot setup,
        DashboardSnapshot dashboard,
        IReadOnlyList<AppLocationTarget> locations,
        WinUiToolingSnapshot winUiTooling,
        NativeToolingSnapshot nativeTooling)
    {
        var checks = new List<AppDiagnosticCheck>
        {
            setup.CanUseCoreFeatures
                ? Pass("core-features", "Core features ready", "Icon Library paths are available.")
                : Blocking("core-features", "Core features blocked", "Icon Library paths are not available."),
            setup.HasIcons
                ? Pass("icon-library-content", "Icon Library has icons", $"{setup.IconCount} icons are available.")
                : Warning("icon-library-content", "Icon Library is empty", "Import .ico files or add category folders under .icons."),
            dashboard.CatalogWarningCount == 0
                ? Pass("catalog-warnings", "Catalog has no warnings", "All scanned icons are readable.")
                : Warning("catalog-warnings", "Catalog has warnings", $"{dashboard.CatalogWarningCount} icon files could not be read."),
            dashboard.MissingTargetRecordCount == 0
                ? Pass("restore-targets", "Restore history targets exist", "No history entries point to missing targets.")
                : Info("restore-targets", "Restore history has missing targets", $"{dashboard.MissingTargetRecordCount} history entries point to missing targets."),
            dashboard.MissingAppliedIconRecordCount == 0
                ? Pass("restore-icons", "Applied icon references exist", "No history entries point to missing applied icon files.")
                : Info("restore-icons", "Restore history has missing icon references", $"{dashboard.MissingAppliedIconRecordCount} history entries point to missing applied icon files."),
            ShellIntegrationCheck(setup.ShellIntegration),
            WinUiTemplatesCheck(winUiTooling),
            WinAppCheck(winUiTooling),
            NativeToolingCheck(nativeTooling)
        };

        foreach (var location in locations)
        {
            checks.Add(location.Exists
                ? Pass($"location-{location.Kind}", $"{location.Label} exists", location.FullPath)
                : Info($"location-{location.Kind}", $"{location.Label} is missing", location.FullPath));
        }

        return checks;
    }

    private static AppDiagnosticCheck ShellIntegrationCheck(ShellIntegrationReadiness readiness)
    {
        return readiness switch
        {
            ShellIntegrationReadiness.Configured => Pass(
                "shell-integration",
                "Shell integration configured",
                "Explorer integration is configured."),
            ShellIntegrationReadiness.DecisionPending => Warning(
                "shell-integration",
                "Shell integration state is stale",
                "V1 already selected packaged modern and classic Explorer handlers; refresh setup state."),
            ShellIntegrationReadiness.NotConfigured => Warning(
                "shell-integration",
                "Shell integration not configured",
                "Explorer integration is not installed yet."),
            ShellIntegrationReadiness.Unavailable => Blocking(
                "shell-integration",
                "Shell integration unavailable",
                "Explorer integration cannot run in the current environment."),
            _ => Warning(
                "shell-integration",
                "Shell integration status unknown",
                readiness.ToString())
        };
    }

    private static AppDiagnosticCheck WinUiTemplatesCheck(WinUiToolingSnapshot tooling)
    {
        if (!tooling.IsChecked)
        {
            return Info("winui-templates", "WinUI templates not checked", tooling.WinUiTemplatesDetail);
        }

        return tooling.WinUiTemplatesAvailable
            ? Pass("winui-templates", "WinUI templates available", tooling.WinUiTemplatesDetail)
            : Blocking("winui-templates", "WinUI templates missing", "Run /winui-setup before scaffolding the WinUI app.");
    }

    private static AppDiagnosticCheck WinAppCheck(WinUiToolingSnapshot tooling)
    {
        if (!tooling.IsChecked)
        {
            return Info("winapp", "winapp CLI not checked", tooling.WinAppDetail);
        }

        return tooling.WinAppAvailable
            ? Pass("winapp", "winapp CLI available", tooling.WinAppDetail)
            : Blocking("winapp", "winapp CLI missing", "Run /winui-setup before scaffolding or running the WinUI app.");
    }

    private static AppDiagnosticCheck NativeToolingCheck(NativeToolingSnapshot tooling)
    {
        if (!tooling.IsChecked)
        {
            return Info("native-build-tools", "Native build tools not checked", tooling.Summary);
        }

        return tooling.CanBuildNativeExtension
            ? Pass("native-build-tools", "Native build tools available", tooling.Summary)
            : Blocking(
                "native-build-tools",
                "Native build tools missing",
                "Install Visual Studio C++ Build Tools or use a Developer Command Prompt before building IconReplacer.ShellExtension.dll. " + tooling.Summary);
    }

    private static AppDiagnosticCheck Pass(string id, string title, string detail)
    {
        return new AppDiagnosticCheck(id, AppDiagnosticStatus.Pass, title, detail);
    }

    private static AppDiagnosticCheck Info(string id, string title, string detail)
    {
        return new AppDiagnosticCheck(id, AppDiagnosticStatus.Info, title, detail);
    }

    private static AppDiagnosticCheck Warning(string id, string title, string detail)
    {
        return new AppDiagnosticCheck(id, AppDiagnosticStatus.Warning, title, detail);
    }

    private static AppDiagnosticCheck Blocking(string id, string title, string detail)
    {
        return new AppDiagnosticCheck(id, AppDiagnosticStatus.Blocking, title, detail);
    }
}
