using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class AppHomeService
{
    private readonly SetupReadinessService _setupReadinessService;
    private readonly DashboardService _dashboardService;
    private readonly IconMenuService _iconMenuService;
    private readonly RestoreHistoryService _restoreHistoryService;
    private readonly AppLocationService _appLocationService;

    public AppHomeService(
        SetupReadinessService? setupReadinessService = null,
        DashboardService? dashboardService = null,
        IconMenuService? iconMenuService = null,
        RestoreHistoryService? restoreHistoryService = null,
        AppLocationService? appLocationService = null)
    {
        _dashboardService = dashboardService ?? new DashboardService();
        _setupReadinessService = setupReadinessService ?? new SetupReadinessService(_dashboardService);
        _iconMenuService = iconMenuService ?? new IconMenuService();
        _restoreHistoryService = restoreHistoryService ?? new RestoreHistoryService();
        _appLocationService = appLocationService ?? new AppLocationService();
    }

    public OperationResult<AppHomeSnapshot> GetSnapshot(
        IconLibraryPaths paths,
        RestoreHistoryFilter historyFilter = RestoreHistoryFilter.All,
        IconMenuOptions? menuOptions = null,
        PackagingPlanInputs? packagingInputs = null)
    {
        var setup = _setupReadinessService.GetSnapshot(paths, packagingInputs);
        if (!setup.Succeeded || setup.Value is null)
        {
            return OperationResult<AppHomeSnapshot>.Failure(setup.Error);
        }

        var dashboard = _dashboardService.GetSnapshot(paths);
        if (!dashboard.Succeeded || dashboard.Value is null)
        {
            return OperationResult<AppHomeSnapshot>.Failure(dashboard.Error);
        }

        var menu = _iconMenuService.BuildSnapshot(paths, menuOptions);
        if (!menu.Succeeded || menu.Value is null)
        {
            return OperationResult<AppHomeSnapshot>.Failure(menu.Error);
        }

        var history = _restoreHistoryService.GetHistory(paths, historyFilter);
        if (!history.Succeeded || history.Value is null)
        {
            return OperationResult<AppHomeSnapshot>.Failure(history.Error);
        }

        var locations = _appLocationService.GetLocations(paths);
        if (!locations.Succeeded || locations.Value is null)
        {
            return OperationResult<AppHomeSnapshot>.Failure(locations.Error);
        }

        return OperationResult<AppHomeSnapshot>.Success(new AppHomeSnapshot(
            setup.Value,
            dashboard.Value,
            menu.Value,
            history.Value,
            locations.Value,
            DateTimeOffset.UtcNow));
    }

    public OperationResult<AppHomeSnapshot> GetSnapshotFromEnvironment(
        RestoreHistoryFilter historyFilter = RestoreHistoryFilter.All,
        IconMenuOptions? menuOptions = null,
        PackagingPlanInputs? packagingInputs = null)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<AppHomeSnapshot>.Failure(paths.Error);
        }

        return GetSnapshot(paths.Value, historyFilter, menuOptions, packagingInputs);
    }
}
