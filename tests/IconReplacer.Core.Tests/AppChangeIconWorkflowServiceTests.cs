using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class AppChangeIconWorkflowServiceTests
{
    [Fact]
    public void GetWorkflowForValidActivationWaitsForIconSelection()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("Project");
        Directory.CreateDirectory(folder);

        var workflow = new AppChangeIconWorkflowService().GetWorkflow(ChangeIconArguments(folder), paths);

        Assert.True(workflow.Succeeded, workflow.Error.Message);
        Assert.NotNull(workflow.Value);
        Assert.Equal(AppChangeIconWorkflowStep.NeedIcon, workflow.Value.Step);
        Assert.True(workflow.Value.CanOpenPicker);
        Assert.False(workflow.Value.CanPreview);
        Assert.False(workflow.Value.CanApply);
        Assert.NotNull(workflow.Value.LaunchRequest);
        Assert.True(workflow.Value.LaunchRequest.PickerRequest.CanOpenPicker);
    }

    [Fact]
    public void GetWorkflowWithSelectedIconIsReadyToApplyWithoutMutating()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("Project");
        var icon = temp.PathFor("icons", "blue.ico");
        Directory.CreateDirectory(folder);
        TestIconFactory.WriteValidIcon(icon);

        var workflow = new AppChangeIconWorkflowService().GetWorkflow(ChangeIconArguments(folder), paths, icon);

        Assert.True(workflow.Succeeded, workflow.Error.Message);
        Assert.NotNull(workflow.Value);
        Assert.Equal(AppChangeIconWorkflowStep.ReadyToApply, workflow.Value.Step);
        Assert.True(workflow.Value.CanPreview);
        Assert.True(workflow.Value.CanApply);
        Assert.NotNull(workflow.Value.Preview);
        Assert.Equal("blue", workflow.Value.Preview.Preview.IconDetails!.DisplayName);
        Assert.False(File.Exists(Path.Combine(folder, "desktop.ini")));
        Assert.False(File.Exists(paths.RestoreStateFile));
    }

    [Fact]
    public void GetWorkflowBlocksUnsupportedTargetBeforePicker()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var file = temp.PathFor("notes.txt");
        File.WriteAllText(file, "not a shortcut");

        var workflow = new AppChangeIconWorkflowService().GetWorkflow(
            [
                "change-icon",
                "--target",
                file,
                "--target-kind",
                "shortcut"
            ],
            paths);

        Assert.True(workflow.Succeeded, workflow.Error.Message);
        Assert.NotNull(workflow.Value);
        Assert.Equal(AppChangeIconWorkflowStep.Blocked, workflow.Value.Step);
        Assert.False(workflow.Value.CanOpenPicker);
        Assert.False(workflow.Value.CanApply);
        Assert.Equal(ErrorCode.UnsupportedTarget, workflow.Value.Error.Code);
    }

    [Fact]
    public void GetWorkflowWithInvalidIconKeepsTargetUnchanged()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("Project");
        var badIcon = temp.PathFor("icons", "bad.ico");
        Directory.CreateDirectory(folder);
        TestIconFactory.WritePngHeader(badIcon);

        var workflow = new AppChangeIconWorkflowService().GetWorkflow(ChangeIconArguments(folder), paths, badIcon);

        Assert.True(workflow.Succeeded, workflow.Error.Message);
        Assert.NotNull(workflow.Value);
        Assert.Equal(AppChangeIconWorkflowStep.Blocked, workflow.Value.Step);
        Assert.True(workflow.Value.CanPreview);
        Assert.False(workflow.Value.CanApply);
        Assert.Equal(ErrorCode.InvalidIcon, workflow.Value.Error.Code);
        Assert.False(File.Exists(Path.Combine(folder, "desktop.ini")));
        Assert.False(File.Exists(paths.RestoreStateFile));
    }

    [Fact]
    public void GetWorkflowRejectsHomeActivation()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var workflow = new AppChangeIconWorkflowService().GetWorkflow([], paths);

        Assert.False(workflow.Succeeded);
        Assert.Equal(ErrorCode.InvalidArgument, workflow.Error.Code);
    }

    private static string[] ChangeIconArguments(string folder)
    {
        return
        [
            "change-icon",
            "--target",
            folder,
            "--target-kind",
            "folder"
        ];
    }
}
