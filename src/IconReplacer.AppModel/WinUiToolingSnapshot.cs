namespace IconReplacer.AppModel;

public sealed record WinUiToolingSnapshot(
    bool IsChecked,
    bool WinUiTemplatesAvailable,
    bool WinAppAvailable,
    string WinUiTemplatesDetail,
    string WinAppDetail)
{
    public static WinUiToolingSnapshot NotChecked { get; } = new(
        IsChecked: false,
        WinUiTemplatesAvailable: false,
        WinAppAvailable: false,
        "WinUI templates were not checked.",
        "winapp CLI was not checked.");
}
