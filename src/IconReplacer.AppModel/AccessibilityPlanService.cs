namespace IconReplacer.AppModel;

public sealed class AccessibilityPlanService
{
    public AccessibilityPlanSnapshot GetPlan()
    {
        return new AccessibilityPlanSnapshot(
            GetRequirements(),
            GetSurfaces(),
            DateTimeOffset.UtcNow);
    }

    private static IReadOnlyList<AccessibilityRequirement> GetRequirements()
    {
        return new[]
        {
            Requirement(
                "keyboard-every-command",
                AccessibilityRequirementCategory.Keyboard,
                "Every command is keyboard reachable",
                "Primary and secondary commands must be reachable without a mouse."),
            Requirement(
                "keyboard-focus-order",
                AccessibilityRequirementCategory.Keyboard,
                "Focus order follows visual order",
                "Tab order should match the visible reading and workflow order."),
            Requirement(
                "keyboard-dialog-return",
                AccessibilityRequirementCategory.Keyboard,
                "Dialogs return focus",
                "Pickers, confirmations, and error dialogs should restore focus to the invoking control."),
            Requirement(
                "keyboard-details-reachable",
                AccessibilityRequirementCategory.Keyboard,
                "Restore and error details are reachable",
                "Record details, disabled reasons, and warning details must be reachable by keyboard."),
            Requirement(
                "names-icon-only-controls",
                AccessibilityRequirementCategory.NamesAndSemantics,
                "Icon-only controls have accessible names",
                "Buttons that use icons must expose clear automation names."),
            Requirement(
                "names-icon-tiles",
                AccessibilityRequirementCategory.NamesAndSemantics,
                "Icon tiles expose name and category",
                "Catalog tiles must not rely on thumbnails alone."),
            Requirement(
                "semantics-persistent-errors",
                AccessibilityRequirementCategory.NamesAndSemantics,
                "Errors are persistent UI",
                "Errors must remain visible in the app and not be toast-only."),
            Requirement(
                "visual-high-contrast",
                AccessibilityRequirementCategory.VisualAdaptation,
                "High contrast keeps status readable",
                "Controls, warnings, disabled reasons, and status counts must remain readable."),
            Requirement(
                "visual-200-scaling",
                AccessibilityRequirementCategory.VisualAdaptation,
                "200 percent scaling does not clip",
                "Primary commands, counters, and paths should not clip at 200 percent scaling."),
            Requirement(
                "visual-long-paths",
                AccessibilityRequirementCategory.VisualAdaptation,
                "Long paths wrap or elide",
                "Long paths should wrap or elide with a reachable detail path."),
            ManualProof(
                "proof-first-run-normal",
                "First-run normal scaling proof",
                "Capture first-run app state at normal scaling."),
            ManualProof(
                "proof-first-run-200",
                "First-run 200 percent proof",
                "Capture first-run app state at 200 percent scaling."),
            ManualProof(
                "proof-high-contrast",
                "High contrast proof",
                "Capture app state with high contrast enabled."),
            ManualProof(
                "proof-keyboard-import-restore",
                "Keyboard import and restore proof",
                "Capture notes or screenshots for keyboard-only import and restore flows."),
            ManualProof(
                "proof-error-state",
                "Error state proof",
                "Capture a persistent error state with reachable details.")
        };
    }

    private static IReadOnlyList<AccessibilitySurface> GetSurfaces()
    {
        return new[]
        {
            Surface(
                "home",
                "Home",
                "First screen with readiness, setup actions, locations, menu counts, and history counts.",
                "Import icons",
                "Open Icon Library",
                "Show diagnostics",
                "Configure shell integration"),
            Surface(
                "icon-browser",
                "Icon Browser",
                "Search, category filters, warning review, and icon selection.",
                "Search icons",
                "Filter by category",
                "Open icon details",
                "Import icons"),
            Surface(
                "icon-details",
                "Icon Details",
                "Selected icon metadata, image entries, and recommended image.",
                "Use selected icon",
                "Copy icon path",
                "Open containing folder"),
            Surface(
                "history",
                "Recent Changes",
                "Filtered restore history with enabled, warning, and disabled restore actions.",
                "Filter history",
                "Preview restore",
                "Restore selected change"),
            Surface(
                "restore-preview",
                "Restore Preview",
                "Confirmation state before restore mutation.",
                "Confirm restore",
                "Cancel restore",
                "Review disabled reason"),
            Surface(
                "diagnostics",
                "Diagnostics",
                "Readiness, blockers, app locations, shell status, and tooling checks.",
                "Refresh diagnostics",
                "Open app data",
                "Open package plan"),
            Surface(
                "shell-plan",
                "Shell Integration Plan",
                "Modern shell path, prerequisites, manifest contract, and fallback status.",
                "Review shell bridge",
                "Review manifest contract",
                "Review package plan"),
            Surface(
                "shell-bridge",
                "Shell Bridge",
                "Native Explorer command bridge preview, resolved arguments, menu caps, and safety rules.",
                "Review resolved commands",
                "Review safety rules",
                "Open diagnostics"),
            Surface(
                "package-plan",
                "Package Plan",
                "Install, uninstall, signing, proof, and data-preservation gates.",
                "Review install blockers",
                "Review uninstall policy")
        };
    }

    private static AccessibilityRequirement Requirement(
        string id,
        AccessibilityRequirementCategory category,
        string title,
        string detail)
    {
        return new AccessibilityRequirement(
            id,
            category,
            title,
            detail,
            RequiredForV1: true,
            ManualProofRequired: false);
    }

    private static AccessibilityRequirement ManualProof(
        string id,
        string title,
        string detail)
    {
        return new AccessibilityRequirement(
            id,
            AccessibilityRequirementCategory.ManualProof,
            title,
            detail,
            RequiredForV1: true,
            ManualProofRequired: true);
    }

    private static AccessibilitySurface Surface(
        string routeId,
        string title,
        string purpose,
        params string[] primaryCommands)
    {
        return new AccessibilitySurface(
            routeId,
            title,
            purpose,
            primaryCommands,
            RequiresKeyboardReachability: true,
            RequiresPersistentErrors: true);
    }
}
