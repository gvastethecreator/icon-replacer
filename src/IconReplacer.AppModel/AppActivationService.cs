using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class AppActivationService
{
    private readonly AppHomeService _homeService;
    private readonly AppLaunchRequestService _launchRequestService;
    private readonly AppMenuApplyActivationService _menuApplyActivationService;

    public AppActivationService(
        AppHomeService? homeService = null,
        AppLaunchRequestService? launchRequestService = null,
        AppMenuApplyActivationService? menuApplyActivationService = null)
    {
        _homeService = homeService ?? new AppHomeService();
        _launchRequestService = launchRequestService ?? new AppLaunchRequestService();
        _menuApplyActivationService = menuApplyActivationService ?? new AppMenuApplyActivationService();
    }

    public OperationResult<AppActivationSnapshot> Activate(
        IReadOnlyList<string> arguments,
        IconLibraryPaths paths)
    {
        if (arguments is null)
        {
            return OperationResult<AppActivationSnapshot>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "Activation arguments are required."));
        }

        if (arguments.Count == 0)
        {
            var home = _homeService.GetSnapshot(paths);
            if (!home.Succeeded || home.Value is null)
            {
                return OperationResult<AppActivationSnapshot>.Failure(home.Error);
            }

            return OperationResult<AppActivationSnapshot>.Success(new AppActivationSnapshot(
                AppActivationKind.Home,
                [],
                home.Value,
                LaunchRequest: null,
                MenuApplyRequest: null,
                CanContinue: true,
                IconReplacerError.None,
                DateTimeOffset.UtcNow));
        }

        if (string.Equals(arguments[0], IconMenuCommandService.MenuApplyVerbName, StringComparison.OrdinalIgnoreCase))
        {
            var menuApply = _menuApplyActivationService.PreviewActivation(arguments, paths);
            if (!menuApply.Succeeded || menuApply.Value is null)
            {
                return OperationResult<AppActivationSnapshot>.Failure(menuApply.Error);
            }

            return OperationResult<AppActivationSnapshot>.Success(new AppActivationSnapshot(
                AppActivationKind.MenuApply,
                arguments.ToArray(),
                Home: null,
                LaunchRequest: null,
                menuApply.Value,
                menuApply.Value.CanApply,
                menuApply.Value.Error,
                DateTimeOffset.UtcNow));
        }

        var launch = _launchRequestService.ParseArguments(arguments, paths);
        if (!launch.Succeeded || launch.Value is null)
        {
            return OperationResult<AppActivationSnapshot>.Failure(launch.Error);
        }

        return OperationResult<AppActivationSnapshot>.Success(new AppActivationSnapshot(
            AppActivationKind.ChangeIcon,
            arguments.ToArray(),
            Home: null,
            launch.Value,
            MenuApplyRequest: null,
            launch.Value.CanLaunch,
            launch.Value.Error,
            DateTimeOffset.UtcNow));
    }

    public OperationResult<AppActivationSnapshot> ActivateFromEnvironment(
        IReadOnlyList<string> arguments)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<AppActivationSnapshot>.Failure(paths.Error);
        }

        return Activate(arguments, paths.Value);
    }
}
