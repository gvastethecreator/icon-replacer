namespace IconReplacer.AppModel;

public sealed record ShellManifestContextMenuTarget(
    string ItemType,
    string VerbId,
    string Clsid);
