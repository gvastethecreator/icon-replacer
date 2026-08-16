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
            requirement.Id == "semantics-persistent-status" &&
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
            requirement.Id == "proof-keyboard-library" &&
            requirement.ManualProofRequired);
    }

    [Fact]
    public void GetPlanListsCoreWinUiSurfacesAndCommands()
    {
        var plan = new AccessibilityPlanService().GetPlan();

        Assert.Equal(3, plan.Surfaces.Count);
        Assert.Contains(plan.Surfaces, surface =>
            surface.RouteId == "library" &&
            surface.PrimaryCommands.Contains("Import icons"));
        Assert.Contains(plan.Surfaces, surface =>
            surface.RouteId == "recent" &&
            surface.PrimaryCommands.Contains("Restore original icon"));
        Assert.Contains(plan.Surfaces, surface =>
            surface.RouteId == "settings" &&
            surface.RequiresKeyboardReachability &&
            surface.PrimaryCommands.Contains("Use Dark theme"));
        Assert.DoesNotContain(plan.Surfaces, surface =>
            surface.RouteId is "package-plan" or "shell-bridge" or "diagnostics");
    }
}
