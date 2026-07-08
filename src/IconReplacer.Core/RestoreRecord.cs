namespace IconReplacer.Core;

public sealed record RestoreRecord(
    Guid Id,
    TargetItem Target,
    DateTimeOffset CreatedAt,
    string AppliedIconPath,
    RestoreSnapshot PreviousState,
    RestoreRecordStatus Status,
    string? RecoveryDetail = null)
{
    public static RestoreRecord CreatePending(TargetItem target, string appliedIconPath, RestoreSnapshot previousState)
    {
        return new RestoreRecord(
            Guid.NewGuid(),
            target,
            DateTimeOffset.UtcNow,
            Path.GetFullPath(appliedIconPath),
            previousState,
            RestoreRecordStatus.Pending);
    }
}

public abstract record RestoreSnapshot;

public sealed record FolderRestoreSnapshot(
    bool DesktopIniExisted,
    IReadOnlyDictionary<string, string?> PreviousShellClassInfoValues,
    FileAttributes? PreviousFolderAttributes,
    FileAttributes? PreviousDesktopIniAttributes)
    : RestoreSnapshot;

public sealed record ShortcutRestoreSnapshot(
    string? PreviousIconPath,
    int PreviousIconIndex)
    : RestoreSnapshot;
