using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class AppWindowService
{
    private readonly AppNavigationService _navigationService;
    private readonly AppActivationService _activationService;
    private readonly AppDiagnosticsService _diagnosticsService;

    public AppWindowService(
        AppNavigationService? navigationService = null,
        AppActivationService? activationService = null,
        AppDiagnosticsService? diagnosticsService = null)
    {
        _navigationService = navigationService ?? new AppNavigationService();
        _activationService = activationService ?? new AppActivationService();
        _diagnosticsService = diagnosticsService ?? new AppDiagnosticsService();
    }

    public OperationResult<AppWindowSnapshot> GetWindow(
        IReadOnlyList<string> activationArguments,
        IconLibraryPaths paths,
        WinUiToolingSnapshot? winUiTooling = null)
    {
        if (activationArguments is null)
        {
            return OperationResult<AppWindowSnapshot>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "Activation arguments are required."));
        }

        var navigation = _navigationService.GetPlan();
        var diagnostics = _diagnosticsService.GetDiagnostics(paths, winUiTooling);
        if (!diagnostics.Succeeded || diagnostics.Value is null)
        {
            return OperationResult<AppWindowSnapshot>.Failure(diagnostics.Error);
        }

        var activation = _activationService.Activate(activationArguments, paths);
        if (!activation.Succeeded || activation.Value is null)
        {
            return OperationResult<AppWindowSnapshot>.Success(CreateFallbackWindow(
                navigation,
                diagnostics.Value,
                activation.Error));
        }

        var selectedRouteId = activation.Value.Kind switch
        {
            AppActivationKind.Home => AppNavigationRouteIds.Home,
            AppActivationKind.ChangeIcon => AppNavigationRouteIds.ChangeIcon,
            AppActivationKind.MenuApply => AppNavigationRouteIds.Home,
            _ => navigation.FallbackRouteId
        };

        return OperationResult<AppWindowSnapshot>.Success(new AppWindowSnapshot(
            navigation,
            activation.Value,
            diagnostics.Value,
            selectedRouteId,
            ResolveTitle(navigation, selectedRouteId),
            activation.Value.CanContinue,
            activation.Value.Error,
            DateTimeOffset.UtcNow));
    }

    public OperationResult<AppWindowSnapshot> GetWindowFromEnvironment(
        IReadOnlyList<string> activationArguments,
        WinUiToolingSnapshot? winUiTooling = null)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<AppWindowSnapshot>.Failure(paths.Error);
        }

        return GetWindow(activationArguments, paths.Value, winUiTooling);
    }

    private static AppWindowSnapshot CreateFallbackWindow(
        AppNavigationPlanSnapshot navigation,
        AppDiagnosticsSnapshot diagnostics,
        IconReplacerError error)
    {
        return new AppWindowSnapshot(
            navigation,
            Activation: null,
            diagnostics,
            navigation.FallbackRouteId,
            ResolveTitle(navigation, navigation.FallbackRouteId),
            CanUseSelectedRoute: false,
            error,
            DateTimeOffset.UtcNow);
    }

    private static string ResolveTitle(AppNavigationPlanSnapshot navigation, string routeId)
    {
        return navigation.FindRoute(routeId)?.Title ?? "Icon Replacer";
    }
}
