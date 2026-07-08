using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class ShellLinkClientTests
{
    [WindowsOnlyFact]
    public void CreateOrUpdateAndReadRoundTripsShortcutMetadata()
    {
        using var temp = new TempDirectory();
        var target = temp.PathFor("target.exe");
        File.WriteAllBytes(target, [0]);
        var workingDirectory = temp.PathFor("work");
        Directory.CreateDirectory(workingDirectory);
        var icon = temp.PathFor("icons", "old.ico");
        TestIconFactory.WriteValidIcon(icon);
        var shortcut = temp.PathFor("sample.lnk");
        var client = new ShellLinkClient();

        var create = client.CreateOrUpdate(new ShellLinkInfo(
            shortcut,
            target,
            "--sample",
            workingDirectory,
            "Sample shortcut",
            65,
            icon,
            0));

        Assert.True(create.Succeeded, create.Error.Message);

        var read = client.Read(shortcut);

        Assert.True(read.Succeeded, read.Error.Message);
        Assert.NotNull(read.Value);
        Assert.Equal(shortcut, read.Value.FullPath);
        Assert.Equal(target, read.Value.TargetPath);
        Assert.Equal("--sample", read.Value.Arguments);
        Assert.Equal(workingDirectory, read.Value.WorkingDirectory);
        Assert.Equal("Sample shortcut", read.Value.Description);
        Assert.Equal(65, read.Value.Hotkey);
        Assert.Equal(icon, read.Value.IconPath);
        Assert.Equal(0, read.Value.IconIndex);
    }

    [WindowsOnlyFact]
    public void SetIconLocationChangesOnlyIconLocation()
    {
        using var temp = new TempDirectory();
        var target = temp.PathFor("target.exe");
        File.WriteAllBytes(target, [0]);
        var workingDirectory = temp.PathFor("work");
        Directory.CreateDirectory(workingDirectory);
        var oldIcon = temp.PathFor("icons", "old.ico");
        var newIcon = temp.PathFor("icons", "new.ico");
        TestIconFactory.WriteValidIcon(oldIcon);
        TestIconFactory.WriteValidIcon(newIcon);
        var shortcut = temp.PathFor("sample.lnk");
        var client = new ShellLinkClient();
        var create = client.CreateOrUpdate(new ShellLinkInfo(
            shortcut,
            target,
            "--sample",
            workingDirectory,
            "Sample shortcut",
            65,
            oldIcon,
            0));
        Assert.True(create.Succeeded, create.Error.Message);

        var updated = client.SetIconLocation(shortcut, newIcon, 0);

        Assert.True(updated.Succeeded, updated.Error.Message);
        Assert.NotNull(updated.Value);
        Assert.Equal(target, updated.Value.TargetPath);
        Assert.Equal("--sample", updated.Value.Arguments);
        Assert.Equal(workingDirectory, updated.Value.WorkingDirectory);
        Assert.Equal("Sample shortcut", updated.Value.Description);
        Assert.Equal(65, updated.Value.Hotkey);
        Assert.Equal(newIcon, updated.Value.IconPath);
        Assert.Equal(0, updated.Value.IconIndex);
    }
}

