using System.Text;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class DesktopIniDocumentTests
{
    [Fact]
    public void LoadReadsSectionsAndKeys()
    {
        using var temp = new TempDirectory();
        var path = temp.PathFor("desktop.ini");
        File.WriteAllText(
            path,
            """
            [.ShellClassInfo]
            InfoTip=Keep me
            IconFile=old.ico

            [Other]
            Value=yes
            """,
            Encoding.Unicode);

        var document = DesktopIniDocument.Load(path);

        Assert.Equal("Keep me", document.GetValue(".ShellClassInfo", "InfoTip"));
        Assert.Equal("old.ico", document.GetValue(".ShellClassInfo", "IconFile"));
        Assert.Equal("yes", document.GetValue("Other", "Value"));
    }

    [Fact]
    public void SavePreservesUnrelatedKeysWhenIconKeysChange()
    {
        using var temp = new TempDirectory();
        var path = temp.PathFor("desktop.ini");
        File.WriteAllText(
            path,
            """
            [.ShellClassInfo]
            InfoTip=Keep me
            IconFile=old.ico
            """,
            Encoding.Unicode);

        var document = DesktopIniDocument.Load(path);
        document.SetValue(".ShellClassInfo", "IconResource", "new.ico,0");
        document.RemoveValue(".ShellClassInfo", "IconFile");
        document.Save(path);

        var saved = DesktopIniDocument.Load(path);

        Assert.Equal("Keep me", saved.GetValue(".ShellClassInfo", "InfoTip"));
        Assert.Equal("new.ico,0", saved.GetValue(".ShellClassInfo", "IconResource"));
        Assert.Null(saved.GetValue(".ShellClassInfo", "IconFile"));
    }
}

