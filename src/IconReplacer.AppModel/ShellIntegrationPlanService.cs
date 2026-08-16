namespace IconReplacer.AppModel;

public sealed class ShellIntegrationPlanService
{
    public const string PackagedDualModeName = "Packaged Windows 11 + classic Explorer menus";
    public const string ModernModeName = "Modern MSIX + IExplorerCommand";
    public const string LegacyClassicModeName = "Legacy HKCU context-menu verb (retired)";
    public const string NoFallbackModeName = "No fallback; packaged pair required";

    public ShellIntegrationPlanSnapshot GetPlan(
        ShellIntegrationReadiness readiness = ShellIntegrationReadiness.NotConfigured,
        WinUiToolingSnapshot? winUiTooling = null)
    {
        var tooling = winUiTooling ?? WinUiToolingSnapshot.NotChecked;
        var items = new List<ShellIntegrationPlanItem>
        {
            Pass(
                "decision",
                "Packaged dual Explorer integration selected",
                "V1 uses native IExplorerCommand entries for the Windows 11 menu and a packaged classic handler for Show more options.",
                requiredForV1: true),
            Pass(
                "manifest-contract",
                "Dual shell manifest contract ready",
                "The manifest defines COM, modern File Explorer commands, and the packaged classic handler for Directory and .lnk targets.",
                requiredForV1: true),
            ToolingItem(
                "winui-templates",
                "WinUI templates",
                tooling.IsChecked,
                tooling.WinUiTemplatesAvailable,
                tooling.WinUiTemplatesDetail,
                "Run /winui-setup before scaffolding the packaged WinUI app.",
                requiredForV1: true),
            ToolingItem(
                "winapp",
                "winapp CLI",
                tooling.IsChecked,
                tooling.WinAppAvailable,
                tooling.WinAppDetail,
                "Run /winui-setup before scaffolding or running the packaged WinUI app.",
                requiredForV1: true),
            Info(
                "package-identity",
                "Package identity required",
                "MSIX package identity is required before Explorer can register the modern command.",
                requiredForV1: true),
            Info(
                "native-extension",
                "Native shell extension required",
                "The Explorer-facing component should stay thin and call shared AppModel operations.",
                requiredForV1: true),
            Pass(
                "classic-handler",
                "Packaged classic handler selected",
                "Show more options uses the bounded packaged handler; raw HKCU verbs are retired.",
                requiredForV1: true),
            ExplorerRegistrationItem(readiness),
            Info(
                "legacy-hkcu-retired",
                "Legacy HKCU verbs retired",
                "The product does not install raw per-user context-menu verbs.",
                requiredForV1: false)
        };

        return new ShellIntegrationPlanSnapshot(
            ShellIntegrationMode.PackagedDualExplorerCommands,
            ShellIntegrationMode.None,
            readiness,
            DecisionFinal: true,
            PackagedDualModeName,
            NoFallbackModeName,
            "The packaged dual path provides the native Windows 11 menu and the requested preview-capable classic menu without raw registry verbs.",
            items,
            DateTimeOffset.UtcNow);
    }

    private static ShellIntegrationPlanItem ExplorerRegistrationItem(ShellIntegrationReadiness readiness)
    {
        return readiness switch
        {
            ShellIntegrationReadiness.Configured => Pass(
                "explorer-registration",
                "Explorer registration configured",
                "Explorer integration is installed for the selected modern path.",
                requiredForV1: true),
            ShellIntegrationReadiness.Unavailable => Blocking(
                "explorer-registration",
                "Explorer registration unavailable",
                "The current environment cannot support the selected Explorer integration.",
                requiredForV1: true),
            ShellIntegrationReadiness.DecisionPending => Warning(
                "explorer-registration",
                "Explorer readiness state is stale",
                "The V1 integration decision is final; refresh setup state before installing the signed package.",
                requiredForV1: true),
            _ => Warning(
                "explorer-registration",
                "Explorer registration not configured",
                "Install the signed package only when the visual Explorer proof is approved.",
                requiredForV1: true)
        };
    }

    private static ShellIntegrationPlanItem ToolingItem(
        string id,
        string title,
        bool isChecked,
        bool isAvailable,
        string availableDetail,
        string missingDetail,
        bool requiredForV1)
    {
        if (!isChecked)
        {
            return Info(id, $"{title} not checked", availableDetail, requiredForV1);
        }

        return isAvailable
            ? Pass(id, $"{title} available", availableDetail, requiredForV1)
            : Blocking(id, $"{title} missing", missingDetail, requiredForV1);
    }

    private static ShellIntegrationPlanItem Pass(
        string id,
        string title,
        string detail,
        bool requiredForV1)
    {
        return new ShellIntegrationPlanItem(id, AppDiagnosticStatus.Pass, title, detail, requiredForV1);
    }

    private static ShellIntegrationPlanItem Info(
        string id,
        string title,
        string detail,
        bool requiredForV1)
    {
        return new ShellIntegrationPlanItem(id, AppDiagnosticStatus.Info, title, detail, requiredForV1);
    }

    private static ShellIntegrationPlanItem Warning(
        string id,
        string title,
        string detail,
        bool requiredForV1)
    {
        return new ShellIntegrationPlanItem(id, AppDiagnosticStatus.Warning, title, detail, requiredForV1);
    }

    private static ShellIntegrationPlanItem Blocking(
        string id,
        string title,
        string detail,
        bool requiredForV1)
    {
        return new ShellIntegrationPlanItem(id, AppDiagnosticStatus.Blocking, title, detail, requiredForV1);
    }
}
