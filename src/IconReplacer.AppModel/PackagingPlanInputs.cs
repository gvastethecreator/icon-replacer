namespace IconReplacer.AppModel;

public sealed record PackagingPlanInputs(
    WinUiToolingSnapshot WinUiTooling,
    NativeToolingSnapshot NativeTooling,
    bool PackageIdentityBuilt,
    bool NativeShellExtensionBuilt,
    bool DevSigningAvailable,
    bool InstallerBuilt,
    bool InstallProofCaptured,
    bool UninstallProofCaptured)
{
    public static PackagingPlanInputs NotChecked { get; } = new(
        WinUiToolingSnapshot.NotChecked,
        NativeToolingSnapshot.NotChecked,
        PackageIdentityBuilt: false,
        NativeShellExtensionBuilt: false,
        DevSigningAvailable: false,
        InstallerBuilt: false,
        InstallProofCaptured: false,
        UninstallProofCaptured: false);

    public static PackagingPlanInputs FromTooling(WinUiToolingSnapshot tooling)
    {
        return NotChecked with { WinUiTooling = tooling };
    }

    public static PackagingPlanInputs FromTooling(
        WinUiToolingSnapshot winUiTooling,
        NativeToolingSnapshot nativeTooling)
    {
        return NotChecked with
        {
            WinUiTooling = winUiTooling,
            NativeTooling = nativeTooling
        };
    }
}
