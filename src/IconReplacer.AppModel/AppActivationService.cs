using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class AppActivationService
{
    private readonly AppHomeService _homeService;
    private readonly AppLaunchRequestService _launchRequestService;

    public AppActivationService(
        AppHomeService? homeService = null,
        AppLaunchRequestService? launchRequestService = null)
    {
        _homeService = homeService ?? new AppHomeService();
        _launchRequestService = launchRequestService ?? new AppLaunchRequestService();
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
                CanContinue: true,
                IconReplacerError.None,
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
