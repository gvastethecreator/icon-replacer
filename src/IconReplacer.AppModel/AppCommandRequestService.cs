using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class AppCommandRequestService
{
    private readonly AppCommandService _commandService;
    private readonly AppLocationService _locationService;

    public AppCommandRequestService(
        AppCommandService? commandService = null,
        AppLocationService? locationService = null)
    {
        _commandService = commandService ?? new AppCommandService();
        _locationService = locationService ?? new AppLocationService();
    }

    public OperationResult<AppCommandRequestSnapshot> CreateRequest(
        AppWindowSnapshot window,
        IconLibraryPaths paths,
        string commandId,
        string? routeId = null,
        AppRestoreWorkflowSnapshot? restoreWorkflow = null,
        AppChangeIconWorkflowSnapshot? changeIconWorkflow = null,
        IconDetailsSnapshot? iconDetails = null)
    {
        if (string.IsNullOrWhiteSpace(commandId))
        {
            return OperationResult<AppCommandRequestSnapshot>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "An app command id is required."));
        }

        var commands = _commandService.GetCommands(
            window,
            routeId,
            restoreWorkflow,
            changeIconWorkflow,
            iconDetails);
        if (!commands.Succeeded || commands.Value is null)
        {
            return OperationResult<AppCommandRequestSnapshot>.Failure(commands.Error);
        }

        return CreateRequest(commands.Value, paths, commandId);
    }

    public OperationResult<AppCommandRequestSnapshot> CreateRequestFromEnvironment(
        IReadOnlyList<string> activationArguments,
        string commandId,
        string? routeId = null,
        WinUiToolingSnapshot? winUiTooling = null,
        Guid? restoreRecordId = null,
        RestoreHistoryFilter restoreHistoryFilter = RestoreHistoryFilter.All,
        string? selectedIconPath = null)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<AppCommandRequestSnapshot>.Failure(paths.Error);
        }

        var commands = _commandService.GetCommandsFromEnvironment(
            activationArguments,
            routeId,
            winUiTooling,
            restoreRecordId,
            restoreHistoryFilter,
            selectedIconPath);
        if (!commands.Succeeded || commands.Value is null)
        {
            return OperationResult<AppCommandRequestSnapshot>.Failure(commands.Error);
        }

        return CreateRequest(commands.Value, paths.Value, commandId);
    }

    private OperationResult<AppCommandRequestSnapshot> CreateRequest(
        AppCommandStateSnapshot commands,
        IconLibraryPaths paths,
        string commandId)
    {
        var command = commands.Commands.FirstOrDefault(item =>
            string.Equals(item.Id, commandId, StringComparison.OrdinalIgnoreCase));
        if (command is null)
        {
            return OperationResult<AppCommandRequestSnapshot>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "The app command is not registered for the current route.",
                commandId));
        }

        if (!command.IsEnabled)
        {
            return OperationResult<AppCommandRequestSnapshot>.Success(Disabled(commands.RouteId, command));
        }

        return command.Kind switch
        {
            AppCommandKind.Navigate => Navigate(commands.RouteId, command),
            AppCommandKind.OpenLocation => OpenLocation(commands.RouteId, command, paths),
            AppCommandKind.Refresh => Ready(
                commands.RouteId,
                command,
                $"Refresh {commands.RouteTitle}.",
                navigationTarget: null,
                locationOpenRequest: null,
                workflowId: null,
                requiresRefresh: true),
            AppCommandKind.Workflow => Ready(
                commands.RouteId,
                command,
                $"Start workflow command {command.Id}.",
                navigationTarget: null,
                locationOpenRequest: null,
                workflowId: command.Id,
                requiresRefresh: false),
            _ => OperationResult<AppCommandRequestSnapshot>.Success(Disabled(
                commands.RouteId,
                command,
                new IconReplacerError(
                    ErrorCode.InvalidArgument,
                    "The app command cannot be executed automatically.",
                    command.Id)))
        };
    }

    private static OperationResult<AppCommandRequestSnapshot> Navigate(
        string routeId,
        AppCommandDescriptor command)
    {
        if (string.IsNullOrWhiteSpace(command.TargetRouteId))
        {
            return OperationResult<AppCommandRequestSnapshot>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "The navigation command does not define a target route.",
                command.Id));
        }

        return Ready(
            routeId,
            command,
            $"Navigate to {command.TargetRouteId}.",
            command.TargetRouteId,
            locationOpenRequest: null,
            workflowId: null,
            requiresRefresh: false);
    }

    private OperationResult<AppCommandRequestSnapshot> OpenLocation(
        string routeId,
        AppCommandDescriptor command,
        IconLibraryPaths paths)
    {
        if (command.LocationKind is null)
        {
            return OperationResult<AppCommandRequestSnapshot>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "The open-location command does not define a location.",
                command.Id));
        }

        var openRequest = _locationService.CreateOpenRequest(command.LocationKind.Value, paths);
        if (!openRequest.Succeeded || openRequest.Value is null)
        {
            return OperationResult<AppCommandRequestSnapshot>.Failure(openRequest.Error);
        }

        if (!openRequest.Value.CanOpen)
        {
            return OperationResult<AppCommandRequestSnapshot>.Success(Disabled(
                routeId,
                command,
                openRequest.Value.Error,
                openRequest.Value));
        }

        return Ready(
            routeId,
            command,
            $"Open {openRequest.Value.TargetPath}.",
            navigationTarget: null,
            openRequest.Value,
            workflowId: null,
            requiresRefresh: false);
    }

    private static AppCommandRequestSnapshot Disabled(
        string routeId,
        AppCommandDescriptor command,
        IconReplacerError? error = null,
        AppLocationOpenRequest? locationOpenRequest = null)
    {
        var effectiveError = error ?? command.Error;
        return new AppCommandRequestSnapshot(
            routeId,
            command.Id,
            command,
            CanExecute: false,
            $"Command {command.Id} is disabled: {effectiveError.Message}",
            NavigationTarget: null,
            locationOpenRequest,
            WorkflowId: null,
            RequiresRefresh: false,
            effectiveError,
            DateTimeOffset.UtcNow);
    }

    private static OperationResult<AppCommandRequestSnapshot> Ready(
        string routeId,
        AppCommandDescriptor command,
        string summary,
        string? navigationTarget,
        AppLocationOpenRequest? locationOpenRequest,
        string? workflowId,
        bool requiresRefresh)
    {
        return OperationResult<AppCommandRequestSnapshot>.Success(new AppCommandRequestSnapshot(
            routeId,
            command.Id,
            command,
            CanExecute: true,
            summary,
            navigationTarget,
            locationOpenRequest,
            workflowId,
            requiresRefresh,
            IconReplacerError.None,
            DateTimeOffset.UtcNow));
    }
}
