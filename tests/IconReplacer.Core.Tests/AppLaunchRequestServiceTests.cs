using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class AppLaunchRequestServiceTests
{
    [Fact]
    public void CreateChangeIconRequestBuildsStableArgumentsForFolder()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("Project Folder");
        Directory.CreateDirectory(folder);

        var request = new AppLaunchRequestService().CreateChangeIconRequest(folder, paths);

        Assert.True(request.Succeeded, request.Error.Message);
        Assert.NotNull(request.Value);
        Assert.True(request.Value.CanLaunch);
        Assert.Equal(AppLaunchVerb.ChangeIcon, request.Value.Verb);
        Assert.Equal(AppLaunchRequestService.ChangeIconVerbName, request.Value.VerbName);
        Assert.Equal(TargetKind.Folder, request.Value.Selection.Target!.Kind);
        Assert.Equal(
            [
                "change-icon",
                "--target",
                folder,
                "--target-kind",
                "folder"
            ],
            request.Value.AppArguments);
        Assert.Contains($"\"{folder}\"", request.Value.DisplayArguments);
        Assert.True(request.Value.PickerRequest.CanOpenPicker);
    }

    [Fact]
    public void ParseArgumentsSupportsExplicitShortcutKind()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var shortcut = temp.PathFor("Project.lnk");
        File.WriteAllText(shortcut, "placeholder shortcut");

        var request = new AppLaunchRequestService().ParseArguments(
            [
                "change-icon",
                "--target",
                shortcut,
                "--target-kind",
                "shortcut"
            ],
            paths);

        Assert.True(request.Succeeded, request.Error.Message);
        Assert.NotNull(request.Value);
        Assert.True(request.Value.CanLaunch);
        Assert.Equal(TargetKind.Shortcut, request.Value.Selection.Target!.Kind);
        Assert.Contains("shortcut", request.Value.AppArguments);
    }

    [Fact]
    public void CreateChangeIconRequestRejectsUnsupportedTargetWithoutArguments()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var file = temp.PathFor("notes.txt");
        File.WriteAllText(file, "not a shortcut");

        var request = new AppLaunchRequestService().CreateChangeIconRequest(file, paths);

        Assert.True(request.Succeeded, request.Error.Message);
        Assert.NotNull(request.Value);
        Assert.False(request.Value.CanLaunch);
        Assert.Empty(request.Value.AppArguments);
        Assert.Equal(string.Empty, request.Value.DisplayArguments);
        Assert.Equal(ErrorCode.UnsupportedTarget, request.Value.Error.Code);
        Assert.False(request.Value.PickerRequest.CanOpenPicker);
    }

    [Fact]
    public void ParseArgumentsRejectsMissingTarget()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var request = new AppLaunchRequestService().ParseArguments(
            ["change-icon", "--target-kind", "folder"],
            paths);

        Assert.False(request.Succeeded);
        Assert.Equal(ErrorCode.InvalidArgument, request.Error.Code);
    }
}
