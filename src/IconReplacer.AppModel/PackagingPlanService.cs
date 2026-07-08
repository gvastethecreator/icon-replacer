namespace IconReplacer.AppModel;

public sealed class PackagingPlanService
{
    public const string InstallMode = "Per-user MSIX package";

    private readonly ShellManifestContractService _manifestContractService;

    public PackagingPlanService(ShellManifestContractService? manifestContractService = null)
    {
        _manifestContractService = manifestContractService ?? new ShellManifestContractService();
    }

    public PackagingPlanSnapshot GetPlan(PackagingPlanInputs? inputs = null)
    {
        var currentInputs = inputs ?? PackagingPlanInputs.NotChecked;
        var manifest = _manifestContractService.GetContract();
        var items = new List<ShellIntegrationPlanItem>
        {
            Pass(
                "install-mode",
                "Per-user MSIX install selected",
                "Normal use should not require admin or all-users installation.",
                requiredForV1: true),
            ToolingItem(
                "winui-templates",
                "WinUI templates",
                currentInputs.WinUiTooling.IsChecked,
                currentInputs.WinUiTooling.WinUiTemplatesAvailable,
                currentInputs.WinUiTooling.WinUiTemplatesDetail,
                "Run /winui-setup before scaffolding or packaging the WinUI app.",
                requiredForV1: true),
            ToolingItem(
                "winapp",
                "winapp CLI",
                currentInputs.WinUiTooling.IsChecked,
                currentInputs.WinUiTooling.WinAppAvailable,
                currentInputs.WinUiTooling.WinAppDetail,
                "Run /winui-setup before building, installing, or verifying the MSIX package.",
                requiredForV1: true),
            NativeToolingItem(currentInputs.NativeTooling),
            Pass(
                "manifest-contract",
                "Shell manifest contract ready",
                "The package manifest contract has COM and File Explorer context-menu entries for Directory and .lnk.",
                requiredForV1: true),
            BooleanGate(
                "package-identity",
                "Package identity built",
                currentInputs.PackageIdentityBuilt,
                "Package identity is ready for Explorer registration.",
                "Build the MSIX package identity before registering Explorer integration."),
            BooleanGate(
                "native-shell-extension",
                "Native shell extension built",
                currentInputs.NativeShellExtensionBuilt,
                "The native IExplorerCommand extension binary is available.",
                "Build IconReplacer.ShellExtension.dll before packaging the modern shell path."),
            BooleanGate(
                "dev-signing",
                "Development signing available",
                currentInputs.DevSigningAvailable,
                "A trusted local signing path is available for development installs.",
                "Create or trust a development signing path before local install proof."),
            BooleanGate(
                "installer-built",
                "Install package built",
                currentInputs.InstallerBuilt,
                "The installable package is built.",
                "Build the package before fresh install or upgrade proof."),
            Pass(
                "uninstall-policy",
                "Uninstall policy preserves user data",
                "Uninstall should remove shell integration while preserving .icons and restore history by default.",
                requiredForV1: true),
            ProofGate(
                "install-proof",
                "Fresh install proof captured",
                currentInputs.InstallProofCaptured,
                "Fresh install proof confirms shell integration registration and app launch.",
                "Capture fresh install proof after the package is built."),
            ProofGate(
                "uninstall-proof",
                "Uninstall proof captured",
                currentInputs.UninstallProofCaptured,
                "Uninstall proof confirms shell integration removal and .icons preservation.",
                "Capture uninstall proof before release.")
        };

        return new PackagingPlanSnapshot(
            InstallMode,
            manifest.PackageName,
            RequiresPackageIdentity: true,
            RequiresDevSigning: true,
            RemovesShellIntegrationOnUninstall: true,
            PreservesIconLibraryOnUninstall: true,
            PreservesRestoreHistoryByDefault: true,
            manifest,
            items,
            DateTimeOffset.UtcNow);
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

    private static ShellIntegrationPlanItem NativeToolingItem(NativeToolingSnapshot tooling)
    {
        if (!tooling.IsChecked)
        {
            return Info(
                "native-build-tools",
                "Native build tools not checked",
                tooling.Summary,
                requiredForV1: true);
        }

        return tooling.CanBuildNativeExtension
            ? Pass(
                "native-build-tools",
                "Native build tools available",
                tooling.Summary,
                requiredForV1: true)
            : Blocking(
                "native-build-tools",
                "Native build tools missing",
                "Install Visual Studio C++ Build Tools or use a Developer Command Prompt before building IconReplacer.ShellExtension.dll. " + tooling.Summary,
                requiredForV1: true);
    }

    private static ShellIntegrationPlanItem BooleanGate(
        string id,
        string title,
        bool isReady,
        string readyDetail,
        string missingDetail)
    {
        return isReady
            ? Pass(id, title, readyDetail, requiredForV1: true)
            : Blocking(id, title, missingDetail, requiredForV1: true);
    }

    private static ShellIntegrationPlanItem ProofGate(
        string id,
        string title,
        bool isCaptured,
        string capturedDetail,
        string missingDetail)
    {
        return isCaptured
            ? Pass(id, title, capturedDetail, requiredForV1: true)
            : Warning(id, title, missingDetail, requiredForV1: true);
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
