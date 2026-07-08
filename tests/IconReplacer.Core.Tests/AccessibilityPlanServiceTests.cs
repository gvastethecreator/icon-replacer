using IconReplacer.AppModel;

namespace IconReplacer.Core.Tests;

public sealed class AccessibilityPlanServiceTests
{
    [Fact]
    public void GetPlanIncludesKeyboardSemanticAndVisualRequirements()
    {
        var plan = new AccessibilityPlanService().GetPlan();

        Assert.Contains(plan.Requirements, requirement =>
            requirement.Id == "keyboard-every-command" &&
            requirement.Category == AccessibilityRequirementCategory.Keyboard &&
            requirement.RequiredForV1);
        Assert.Contains(plan.Requirements, requirement =>
            requirement.Id == "names-icon-tiles" &&
            requirement.Category == AccessibilityRequirementCategory.NamesAndSemantics);
        Assert.Contains(plan.Requirements, requirement =>
            requirement.Id == "visual-200-scaling" &&
            requirement.Category == AccessibilityRequirementCategory.VisualAdaptation);
    }

    [Fact]
    public void GetPlanRequiresManualProofForRelease()
    {
        var plan = new AccessibilityPlanService().GetPlan();

        Assert.True(plan.RequiresManualProof);
        Assert.Equal(5, plan.ManualProofCount);
        Assert.Contains(plan.Requirements, requirement =>
            requirement.Id == "proof-keyboard-import-restore" &&
            requirement.ManualProofRequired);
    }

    [Fact]
    public void GetPlanListsCoreWinUiSurfacesAndCommands()
    {
        var plan = new AccessibilityPlanService().GetPlan();

        Assert.Contains(plan.Surfaces, surface =>
            surface.RouteId == "home" &&
            surface.PrimaryCommands.Contains("Import icons"));
        Assert.Contains(plan.Surfaces, surface =>
            surface.RouteId == "history" &&
            surface.PrimaryCommands.Contains("Restore selected change"));
        Assert.Contains(plan.Surfaces, surface =>
            surface.RouteId == "package-plan" &&
            surface.RequiresKeyboardReachability &&
            surface.RequiresPersistentErrors);
        Assert.Contains(plan.Surfaces, surface =>
            surface.RouteId == "shell-bridge" &&
            surface.PrimaryCommands.Contains("Review resolved commands"));
    }
}
