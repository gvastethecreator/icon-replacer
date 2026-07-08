using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record RestoreRecordSummary(
    Guid Id,
    TargetKind TargetKind,
    RestoreRecordStatus Status,
    DateTimeOffset CreatedAt,
    string TargetPath,
    string AppliedIconPath,
    bool TargetExists,
    bool AppliedIconExists,
    bool CanRestore)
{
    public static RestoreRecordSummary FromRecord(RestoreRecord record)
    {
        var targetExists = record.Target.Kind switch
        {
            TargetKind.Folder => Directory.Exists(record.Target.FullPath),
            TargetKind.Shortcut => File.Exists(record.Target.FullPath),
            _ => false
        };
        var appliedIconExists = File.Exists(record.AppliedIconPath);

        return new RestoreRecordSummary(
            record.Id,
            record.Target.Kind,
            record.Status,
            record.CreatedAt,
            record.Target.FullPath,
            record.AppliedIconPath,
            targetExists,
            appliedIconExists,
            record.Status == RestoreRecordStatus.Applied && targetExists);
    }
}
