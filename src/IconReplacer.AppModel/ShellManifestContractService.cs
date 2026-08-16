using System.Xml.Linq;

namespace IconReplacer.AppModel;

public sealed class ShellManifestContractService
{
    public const string PackageName = "IconReplacer";
    public const string ApplicationId = "IconReplacer.App";
    public const string ComServerCategory = "windows.comServer";
    public const string FileExplorerContextMenusCategory = "windows.fileExplorerContextMenus";
    public const string ClassicContextMenuCategory = "windows.fileExplorerClassicContextMenuHandler";
    public const string ComNamespace = "http://schemas.microsoft.com/appx/manifest/com/windows10";
    public const string Desktop4Namespace = "http://schemas.microsoft.com/appx/manifest/desktop/windows10/4";
    public const string Desktop5Namespace = "http://schemas.microsoft.com/appx/manifest/desktop/windows10/5";
    public const string Desktop9Namespace = "http://schemas.microsoft.com/appx/manifest/desktop/windows10/9";
    public const string ExplorerCommandClsid = "B8F1A86D-4C52-4C53-BF72-30B59F7F0F7D";
    public const string CollectionsCommandClsid = "B8F1A86D-4C52-4C53-BF72-30B59F7F0F7E";
    public const string ClassicContextMenuClsid = "B8F1A86D-4C52-4C53-BF72-30B59F7F0F7F";
    public const string SurrogateServerDisplayName = "Icon Replacer Explorer Command";
    public const string ShellExtensionDllPath = "IconReplacer.ShellExtension.dll";
    public const string ThreadingModel = "STA";

    public ShellManifestContractSnapshot GetContract()
    {
        var targets = new[]
        {
            new ShellManifestContextMenuTarget(
                "Directory",
                "IconReplacerChangeIconDirectory",
                ExplorerCommandClsid),
            new ShellManifestContextMenuTarget(
                "Directory",
                "IconReplacerCollectionsDirectory",
                CollectionsCommandClsid),
            new ShellManifestContextMenuTarget(
                ".lnk",
                "IconReplacerChangeIconShortcut",
                ExplorerCommandClsid),
            new ShellManifestContextMenuTarget(
                ".lnk",
                "IconReplacerCollectionsShortcut",
                CollectionsCommandClsid)
        };

        return new ShellManifestContractSnapshot(
            PackageName,
            ApplicationId,
            ComServerCategory,
            FileExplorerContextMenusCategory,
            ComNamespace,
            Desktop4Namespace,
            Desktop5Namespace,
            ExplorerCommandClsid,
            SurrogateServerDisplayName,
            ShellExtensionDllPath,
            ThreadingModel,
            RequiresPackageIdentity: true,
            RequiresExplorerRestartAfterInstall: true,
            new[]
            {
                "IExplorerCommand",
                "IEnumExplorerCommand",
                "IExplorerCommandState",
                "IContextMenu",
                "IContextMenu2",
                "IContextMenu3",
                "IShellExtInit"
            },
            targets,
            BuildManifestFragment(targets),
            DateTimeOffset.UtcNow);
    }

    private static string BuildManifestFragment(IReadOnlyList<ShellManifestContextMenuTarget> targets)
    {
        XNamespace com = ComNamespace;
        XNamespace desktop4 = Desktop4Namespace;
        XNamespace desktop5 = Desktop5Namespace;
        XNamespace desktop9 = Desktop9Namespace;

        var extensions = new XElement("Extensions",
            new XAttribute(XNamespace.Xmlns + "com", ComNamespace),
            new XAttribute(XNamespace.Xmlns + "desktop4", Desktop4Namespace),
            new XAttribute(XNamespace.Xmlns + "desktop5", Desktop5Namespace),
            new XAttribute(XNamespace.Xmlns + "desktop9", Desktop9Namespace),
            new XElement(com + "Extension",
                new XAttribute("Category", ComServerCategory),
                new XElement(com + "ComServer",
                    new XElement(com + "SurrogateServer",
                        new XAttribute("AppId", ExplorerCommandClsid),
                        new XAttribute("DisplayName", SurrogateServerDisplayName),
                        new XElement(com + "Class",
                            new XAttribute("Id", ExplorerCommandClsid),
                            new XAttribute("Path", ShellExtensionDllPath),
                            new XAttribute("ThreadingModel", ThreadingModel)),
                        new XElement(com + "Class",
                            new XAttribute("Id", CollectionsCommandClsid),
                            new XAttribute("Path", ShellExtensionDllPath),
                            new XAttribute("ThreadingModel", ThreadingModel)),
                        new XElement(com + "Class",
                            new XAttribute("Id", ClassicContextMenuClsid),
                            new XAttribute("Path", ShellExtensionDllPath),
                            new XAttribute("ThreadingModel", ThreadingModel))))),
            new XElement(desktop4 + "Extension",
                new XAttribute("Category", FileExplorerContextMenusCategory),
                new XElement(desktop4 + "FileExplorerContextMenus",
                    targets.GroupBy(target => target.ItemType, StringComparer.OrdinalIgnoreCase).Select(group =>
                        new XElement(desktop5 + "ItemType",
                            new XAttribute("Type", group.Key),
                            group.Select(target =>
                                new XElement(desktop5 + "Verb",
                                    new XAttribute("Id", target.VerbId),
                                    new XAttribute("Clsid", target.Clsid))))))),
            new XElement(desktop9 + "Extension",
                new XAttribute("Category", ClassicContextMenuCategory),
                new XElement(desktop9 + "FileExplorerClassicContextMenuHandler",
                    new XElement(desktop9 + "ExtensionHandler",
                        new XAttribute("Type", "Directory"),
                        new XAttribute("Clsid", ClassicContextMenuClsid)),
                    new XElement(desktop9 + "ExtensionHandler",
                        new XAttribute("Type", ".lnk"),
                        new XAttribute("Clsid", ClassicContextMenuClsid)))));

        return extensions.ToString(SaveOptions.None);
    }
}
