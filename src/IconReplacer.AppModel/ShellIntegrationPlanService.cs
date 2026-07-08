namespace IconReplacer.AppModel;

public sealed class ShellIntegrationPlanService
{
    public const string ModernModeName = "Modern MSIX + IExplorerCommand";
    public const string ClassicModeName = "Classic HKCU context-menu verb";

    public ShellIntegrationPlanSnapshot GetPlan(
        ShellIntegrationReadiness readiness = ShellIntegrationReadiness.NotConfigured,
        WinUiToolingSnapshot? winUiTooling = null)
    {
        var tooling = winUiTooling ?? WinUiToolingSnapshot.NotChecked;
        var items = new List<ShellIntegrationPlanItem>
        {
            Pass(
                "decision",
                "Modern shell integration selected",
                "V1 targets the Windows 11 context-menu path with package identity and a native IExplorerCommand extension.",
                requiredForV1: true),
            Pass(
                "manifest-contract",
                "Modern shell manifest contract ready",
                "The shell-manifest contract defines windows.comServer and windows.fileExplorerContextMenus entries for Directory and .lnk targets.",
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
            ExplorerRegistrationItem(readiness),
            Info(
                "classic-fallback",
                "Classic integration remains fallback only",
                "HKCU registry verbs are retained as a prototype or recovery path, not the V1 product target.",
                requiredForV1: false)
        };

        return new ShellIntegrationPlanSnapshot(
            ShellIntegrationMode.ModernMsixIExplorerCommand,
            ShellIntegrationMode.ClassicHkcuVerb,
            readiness,
            DecisionFinal: true,
            ModernModeName,
            ClassicModeName,
            "Modern integration best matches the requested right-click experience and one-level dynamic Icon Library menu.",
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
                "Explorer decision pending",
                "Modern integration is recommended, but V1 registration should wait for decision confirmation.",
                requiredForV1: true),
            _ => Warning(
                "explorer-registration",
                "Explorer registration not configured",
                "Build package identity and the native IExplorerCommand extension before registering Explorer.",
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
