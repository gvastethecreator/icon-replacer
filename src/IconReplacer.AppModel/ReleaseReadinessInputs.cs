namespace IconReplacer.AppModel;

public sealed record ReleaseReadinessInputs(
    PackagingPlanInputs PackagingInputs,
    bool BuildPassed,
    bool TestsPassed,
    bool CliProofCaptured,
    bool ManualExplorerProofCaptured,
    bool AccessibilityProofCaptured,
    bool ReleaseEvidenceCaptured)
{
    public static ReleaseReadinessInputs NotChecked { get; } = new(
        PackagingPlanInputs.NotChecked,
        BuildPassed: false,
        TestsPassed: false,
        CliProofCaptured: false,
        ManualExplorerProofCaptured: false,
        AccessibilityProofCaptured: false,
        ReleaseEvidenceCaptured: false);

    public static ReleaseReadinessInputs FromTooling(WinUiToolingSnapshot tooling)
    {
        return NotChecked with { PackagingInputs = PackagingPlanInputs.FromTooling(tooling) };
    }
}
