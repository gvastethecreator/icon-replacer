namespace IconReplacer.AppModel;

public sealed record IconBrowserItem(
    string DisplayName,
    string FullPath,
    string CategoryName,
    long LengthBytes);
