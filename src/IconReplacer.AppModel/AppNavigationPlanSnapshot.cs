namespace IconReplacer.AppModel;

public sealed record AppNavigationPlanSnapshot(
    IReadOnlyList<AppNavigationRoute> Routes,
    IReadOnlyDictionary<AppActionKind, string> ActionTargets,
    string DefaultRouteId,
    string FallbackRouteId,
    DateTimeOffset RefreshedAt)
{
    public int RouteCount => Routes.Count;

    public int TopLevelRouteCount => Routes.Count(route => route.IsTopLevel);

    public AppNavigationRoute? FindRoute(string routeId)
    {
        return Routes.FirstOrDefault(route =>
            string.Equals(route.RouteId, routeId, StringComparison.OrdinalIgnoreCase));
    }
}
