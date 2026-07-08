using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record AppRouteViewSnapshot(
    AppWindowSnapshot Window,
    AppNavigationRoute Route,
    AppCommandStateSnapshot Commands,
    AppRouteContentKind ContentKind,
    bool IsContentReady,
    string Summary,
    AppHomeSnapshot? Home,
    IconBrowserSnapshot? Browser,
    IconDetailsSnapshot? IconDetails,
    IconImportPickerRequestSnapshot? ImportPickerRequest,
    IReadOnlyList<IconCollectionSummary>? Collections,
    AppChangeIconWorkflowSnapshot? ChangeIconWorkflow,
    RestoreHistorySnapshot? History,
    AppRestoreWorkflowSnapshot? RestoreWorkflow,
    AppDiagnosticsSnapshot? Diagnostics,
    ShellIntegrationPlanSnapshot? ShellPlan,
    ShellExtensionBridgeSnapshot? ShellBridge,
    PackagingPlanSnapshot? PackagePlan,
    AccessibilityPlanSnapshot? AccessibilityPlan,
    IconReplacerError Error,
    DateTimeOffset RefreshedAt);
