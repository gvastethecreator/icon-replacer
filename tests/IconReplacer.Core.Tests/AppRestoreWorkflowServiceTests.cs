using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class AppRestoreWorkflowServiceTests
{
    [Fact]
    public void GetWorkflowWithoutSelectedRecordWaitsForSelection()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var record = CreateRecord(temp, TargetKind.Folder, "folder", "icon.ico", RestoreRecordStatus.Applied);
        Assert.True(new RestoreRecordStore(paths.RestoreStateFile).Upsert(record).Succeeded);

        var workflow = new AppRestoreWorkflowService().GetWorkflow(paths);

        Assert.True(workflow.Succeeded, workflow.Error.Message);
        Assert.NotNull(workflow.Value);
        Assert.Equal(AppRestoreWorkflowStep.NeedRecord, workflow.Value.Step);
        Assert.Null(workflow.Value.SelectedRecordId);
        Assert.Null(workflow.Value.Preview);
        Assert.False(workflow.Value.CanPreview);
        Assert.False(workflow.Value.CanRestore);
        Assert.Equal(ErrorCode.None, workflow.Value.Error.Code);
        Assert.Single(workflow.Value.History.Records);
    }

    [Fact]
    public void GetWorkflowWithAppliedRecordIsReadyToRestore()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var record = CreateRecord(temp, TargetKind.Folder, "folder", "icon.ico", RestoreRecordStatus.Applied);
        var store = new RestoreRecordStore(paths.RestoreStateFile);
        Assert.True(store.Upsert(record).Succeeded);

        var workflow = new AppRestoreWorkflowService().GetWorkflow(paths, record.Id);

        Assert.True(workflow.Succeeded, workflow.Error.Message);
        Assert.NotNull(workflow.Value);
        Assert.Equal(AppRestoreWorkflowStep.ReadyToRestore, workflow.Value.Step);
        Assert.Equal(record.Id, workflow.Value.SelectedRecordId);
        Assert.NotNull(workflow.Value.Preview);
        Assert.True(workflow.Value.CanPreview);
        Assert.True(workflow.Value.CanRestore);
        Assert.Equal(ErrorCode.None, workflow.Value.Error.Code);
        Assert.Equal(record.Id, workflow.Value.Preview!.Record.Id);

        var stored = store.Get(record.Id);
        Assert.True(stored.Succeeded, stored.Error.Message);
        Assert.Equal(RestoreRecordStatus.Applied, stored.Value!.Status);
    }

    [Fact]
    public void GetWorkflowBlocksAlreadyRestoredRecord()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var record = CreateRecord(temp, TargetKind.Folder, "folder", "icon.ico", RestoreRecordStatus.Restored);
        Assert.True(new RestoreRecordStore(paths.RestoreStateFile).Upsert(record).Succeeded);

        var workflow = new AppRestoreWorkflowService().GetWorkflow(paths, record.Id);

        Assert.True(workflow.Succeeded, workflow.Error.Message);
        Assert.NotNull(workflow.Value);
        Assert.Equal(AppRestoreWorkflowStep.Blocked, workflow.Value.Step);
        Assert.Equal(record.Id, workflow.Value.SelectedRecordId);
        Assert.NotNull(workflow.Value.Preview);
        Assert.True(workflow.Value.CanPreview);
        Assert.False(workflow.Value.CanRestore);
        Assert.Equal(ErrorCode.InvalidArgument, workflow.Value.Error.Code);
    }

    [Fact]
    public void GetWorkflowBlocksMissingRecordAfterLoadingHistory()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var record = CreateRecord(temp, TargetKind.Folder, "folder", "icon.ico", RestoreRecordStatus.Applied);
        Assert.True(new RestoreRecordStore(paths.RestoreStateFile).Upsert(record).Succeeded);
        var missingId = Guid.NewGuid();

        var workflow = new AppRestoreWorkflowService().GetWorkflow(paths, missingId);

        Assert.True(workflow.Succeeded, workflow.Error.Message);
        Assert.NotNull(workflow.Value);
        Assert.Equal(AppRestoreWorkflowStep.Blocked, workflow.Value.Step);
        Assert.Equal(missingId, workflow.Value.SelectedRecordId);
        Assert.Null(workflow.Value.Preview);
        Assert.False(workflow.Value.CanPreview);
        Assert.False(workflow.Value.CanRestore);
        Assert.Equal(ErrorCode.PathNotFound, workflow.Value.Error.Code);
        Assert.Single(workflow.Value.History.Records);
    }

    [Fact]
    public void GetWorkflowKeepsMissingAppliedIconRestorableWithWarning()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var record = CreateRecord(
            temp,
            TargetKind.Folder,
            "folder",
            "missing.ico",
            RestoreRecordStatus.Applied,
            createIcon: false);
        Assert.True(new RestoreRecordStore(paths.RestoreStateFile).Upsert(record).Succeeded);

        var workflow = new AppRestoreWorkflowService().GetWorkflow(paths, record.Id);

        Assert.True(workflow.Succeeded, workflow.Error.Message);
        Assert.NotNull(workflow.Value);
        Assert.Equal(AppRestoreWorkflowStep.ReadyToRestore, workflow.Value.Step);
        Assert.True(workflow.Value.CanPreview);
        Assert.True(workflow.Value.CanRestore);
        Assert.NotNull(workflow.Value.Preview);
        Assert.False(workflow.Value.Preview!.Record.AppliedIconExists);
        Assert.Contains("applied icon file is missing", workflow.Value.Preview.WarningText);
    }

    private static RestoreRecord CreateRecord(
        TempDirectory temp,
        TargetKind targetKind,
        string targetName,
        string iconName,
        RestoreRecordStatus status,
        bool createTarget = true,
        bool createIcon = true)
    {
        var targetPath = temp.PathFor("targets", targetName);
        if (createTarget)
        {
            if (targetKind == TargetKind.Folder)
            {
                Directory.CreateDirectory(targetPath);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                File.WriteAllText(targetPath, "shortcut placeholder");
            }
        }

        var iconPath = temp.PathFor("icons", iconName);
        if (createIcon)
        {
            TestIconFactory.WriteValidIcon(iconPath);
        }

        return RestoreRecord.CreatePending(
            new TargetItem(targetKind, targetPath),
            iconPath,
            targetKind == TargetKind.Folder
                ? new FolderRestoreSnapshot(
                    DesktopIniExisted: false,
                    new Dictionary<string, string?>(),
                    FileAttributes.Directory,
                    null)
                : new ShortcutRestoreSnapshot(null, 0)) with { Status = status };
    }
}
