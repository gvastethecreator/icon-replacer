namespace IconReplacer.AppModel;

public sealed record AppCommandStateSnapshot(
    string RouteId,
    string RouteTitle,
    IReadOnlyList<AppCommandDescriptor> Commands,
    DateTimeOffset RefreshedAt)
{
    public int CommandCount => Commands.Count;

    public int EnabledCount => Commands.Count(command => command.IsEnabled);

    public int DisabledCount => Commands.Count(command => !command.IsEnabled);
}
