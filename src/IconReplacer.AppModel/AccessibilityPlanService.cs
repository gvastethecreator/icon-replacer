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
                "The import picker returns focus",
                "Closing or cancelling the native import picker should restore focus to Import icons."),
            Requirement(
                "keyboard-splitter-reachable",
                AccessibilityRequirementCategory.Keyboard,
                "The Collections splitter is keyboard operable",
                "The splitter must expose a useful role and support Left and Right arrow resizing."),
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
                "names-restore-actions",
                AccessibilityRequirementCategory.NamesAndSemantics,
                "Restore actions identify their target",
                "Repeated Restore buttons must include the changed target in their automation name."),
            Requirement(
                "semantics-persistent-status",
                AccessibilityRequirementCategory.NamesAndSemantics,
                "Status and errors are persistent and announced",
                "InfoBar status must remain visible, expose title and message, and reopen when content changes."),
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
                "proof-library-normal",
                "Library at normal scaling",
                "Capture the Library surface with real collections and previews at normal scaling."),
            ManualProof(
                "proof-library-200",
                "Library at 200 percent scaling",
                "Capture Library, toolbar, Collections, and preview labels at 200 percent scaling."),
            ManualProof(
                "proof-high-contrast",
                "High contrast proof",
                "Capture app state with high contrast enabled."),
            ManualProof(
                "proof-keyboard-library",
                "Keyboard-only Library proof",
                "Capture navigation, search, toolbar, preview-size, collection, and splitter keyboard notes."),
            ManualProof(
                "proof-keyboard-recent-status",
                "Keyboard restore and status proof",
                "Capture a target-specific Restore action and an announced persistent status or error.")
        };
    }

    private static IReadOnlyList<AccessibilitySurface> GetSurfaces()
    {
        return new[]
        {
            Surface(
                "library",
                "Library",
                "Searchable icon previews, collection filters, preview sizing, import, refresh, and library access.",
                "Search icons",
                "Filter by collection",
                "Resize Collections panel",
                "Change icon preview size",
                "Refresh library",
                "Import icons",
                "Open Icon Library"),
            Surface(
                "recent",
                "Recent Changes",
                "Changed targets with target-specific restore actions and status details.",
                "Review changed targets",
                "Restore original icon"),
            Surface(
                "settings",
                "Settings",
                "Theme preference and the full Icon Library path.",
                "Use system theme",
                "Use Light theme",
                "Use Dark theme",
                "Open Icon Library")
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
