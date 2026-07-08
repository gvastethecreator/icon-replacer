using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class RestoreRecordTests
{
    [Fact]
    public void CreatePendingNormalizesAppliedIconPathAndStartsPending()
    {
        var target = new TargetItem(TargetKind.Folder, Path.GetFullPath("SampleFolder"));
        var snapshot = new FolderRestoreSnapshot(
            DesktopIniExisted: false,
            new Dictionary<string, string?> { ["IconResource"] = null },
            FileAttributes.Directory,
            null);

        var record = RestoreRecord.CreatePending(target, "icons/test.ico", snapshot);

        Assert.NotEqual(Guid.Empty, record.Id);
        Assert.Equal(RestoreRecordStatus.Pending, record.Status);
        Assert.Equal(Path.GetFullPath("icons/test.ico"), record.AppliedIconPath);
        Assert.Equal(target, record.Target);
    }
}
