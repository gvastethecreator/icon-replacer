namespace IconReplacer.AppModel;

public sealed record AccessibilityPlanSnapshot(
    IReadOnlyList<AccessibilityRequirement> Requirements,
    IReadOnlyList<AccessibilitySurface> Surfaces,
    DateTimeOffset RefreshedAt)
{
    public int RequiredCount => Requirements.Count(requirement => requirement.RequiredForV1);

    public int ManualProofCount => Requirements.Count(requirement => requirement.ManualProofRequired);

    public bool RequiresManualProof => ManualProofCount > 0;
}
