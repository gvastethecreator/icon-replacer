using System.Xml.Linq;
using IconReplacer.AppModel;

namespace IconReplacer.Core.Tests;

public sealed class ShellManifestContractServiceTests
{
    [Fact]
    public void GetContractDefinesModernShellManifestParts()
    {
        var contract = new ShellManifestContractService().GetContract();

        Assert.Equal("IconReplacer", contract.PackageName);
        Assert.Equal("IconReplacer.App", contract.ApplicationId);
        Assert.Equal("windows.comServer", contract.ComServerCategory);
        Assert.Equal("windows.fileExplorerContextMenus", contract.FileExplorerContextMenusCategory);
        Assert.Equal("IconReplacer.ShellExtension.dll", contract.ShellExtensionDllPath);
        Assert.Equal("STA", contract.ThreadingModel);
        Assert.True(contract.RequiresPackageIdentity);
        Assert.True(contract.RequiresExplorerRestartAfterInstall);
        Assert.Contains("IExplorerCommand", contract.RequiredInterfaces);
        Assert.Contains("IEnumExplorerCommand", contract.RequiredInterfaces);
        Assert.Contains("IExplorerCommandState", contract.RequiredInterfaces);
        Assert.Contains("IContextMenu", contract.RequiredInterfaces);
        Assert.Contains("IContextMenu2", contract.RequiredInterfaces);
        Assert.Contains("IContextMenu3", contract.RequiredInterfaces);
        Assert.Contains("IShellExtInit", contract.RequiredInterfaces);
        Assert.Matches("^[0-9A-F-]{36}$", contract.ExplorerCommandClsid);
    }

    [Fact]
    public void GetContractRegistersDirectAndCollectionCommandsForEachTarget()
    {
        var contract = new ShellManifestContractService().GetContract();

        Assert.Equal(4, contract.Targets.Count);
        Assert.Contains(contract.Targets, target =>
            target.ItemType == "Directory" &&
            target.VerbId == "IconReplacerChangeIconDirectory" &&
            target.Clsid == contract.ExplorerCommandClsid);
        Assert.Contains(contract.Targets, target =>
            target.ItemType == ".lnk" &&
            target.VerbId == "IconReplacerChangeIconShortcut" &&
            target.Clsid == contract.ExplorerCommandClsid);
        Assert.Contains(contract.Targets, target =>
            target.ItemType == "Directory" &&
            target.VerbId == "IconReplacerCollectionsDirectory" &&
            target.Clsid == ShellManifestContractService.CollectionsCommandClsid);
        Assert.Contains(contract.Targets, target =>
            target.ItemType == ".lnk" &&
            target.VerbId == "IconReplacerCollectionsShortcut" &&
            target.Clsid == ShellManifestContractService.CollectionsCommandClsid);
    }

    [Fact]
    public void GetContractBuildsParseableManifestFragment()
    {
        var contract = new ShellManifestContractService().GetContract();

        var fragment = XElement.Parse(contract.ManifestFragment);

        Assert.Equal("Extensions", fragment.Name.LocalName);
        Assert.Contains("Category=\"windows.comServer\"", contract.ManifestFragment);
        Assert.Contains("Category=\"windows.fileExplorerContextMenus\"", contract.ManifestFragment);
        Assert.Contains($"AppId=\"{contract.ExplorerCommandClsid}\"", contract.ManifestFragment);
        Assert.Contains($"Id=\"{contract.ExplorerCommandClsid}\"", contract.ManifestFragment);
        Assert.Contains($"Id=\"{ShellManifestContractService.CollectionsCommandClsid}\"", contract.ManifestFragment);
        Assert.Contains($"Id=\"{ShellManifestContractService.ClassicContextMenuClsid}\"", contract.ManifestFragment);
        Assert.Contains("Path=\"IconReplacer.ShellExtension.dll\"", contract.ManifestFragment);
        Assert.Contains("ThreadingModel=\"STA\"", contract.ManifestFragment);
        Assert.Contains("Type=\"Directory\"", contract.ManifestFragment);
        Assert.Contains("Type=\".lnk\"", contract.ManifestFragment);
        Assert.Contains("Category=\"windows.fileExplorerClassicContextMenuHandler\"", contract.ManifestFragment);
        Assert.Contains("xmlns:desktop9=\"http://schemas.microsoft.com/appx/manifest/desktop/windows10/9\"", contract.ManifestFragment);
    }
}
