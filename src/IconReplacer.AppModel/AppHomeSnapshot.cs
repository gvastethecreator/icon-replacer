namespace IconReplacer.AppModel;

public sealed record AppHomeSnapshot(
    SetupReadinessSnapshot Setup,
    DashboardSnapshot Dashboard,
    IconMenuSnapshot Menu,
    RestoreHistorySnapshot History,
    IReadOnlyList<AppLocationTarget> Locations,
    DateTimeOffset RefreshedAt)
{
    public bool CanUseCoreFeatures => Setup.CanUseCoreFeatures;

    public bool NeedsAttention => Setup.NeedsAttention;
}
