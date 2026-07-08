using System.Text.Json;
using System.Text.Json.Serialization;

namespace IconReplacer.Core;

public sealed class RestoreRecordStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _stateFilePath;

    public RestoreRecordStore(string stateFilePath)
    {
        _stateFilePath = stateFilePath;
    }

    public OperationResult<IReadOnlyList<RestoreRecord>> List()
    {
        var records = LoadRecords();
        if (!records.Succeeded || records.Value is null)
        {
            return OperationResult<IReadOnlyList<RestoreRecord>>.Failure(records.Error);
        }

        return OperationResult<IReadOnlyList<RestoreRecord>>.Success(records.Value);
    }

    public OperationResult<RestoreRecord?> Get(Guid id)
    {
        var records = LoadRecords();
        if (!records.Succeeded || records.Value is null)
        {
            return OperationResult<RestoreRecord?>.Failure(records.Error);
        }

        return OperationResult<RestoreRecord?>.Success(records.Value.FirstOrDefault(record => record.Id == id));
    }

    public OperationResult Upsert(RestoreRecord record)
    {
        var records = LoadRecords();
        if (!records.Succeeded || records.Value is null)
        {
            return OperationResult.Failure(records.Error);
        }

        var nextRecords = records.Value.Where(existing => existing.Id != record.Id).Append(record)
            .OrderByDescending(existing => existing.CreatedAt)
            .ToArray();

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_stateFilePath)!);
            var payload = new RestoreRecordStoreDto(nextRecords.Select(ToDto).ToArray());
            File.WriteAllText(_stateFilePath, JsonSerializer.Serialize(payload, JsonOptions));
            return OperationResult.Success();
        }
        catch (UnauthorizedAccessException ex)
        {
            return OperationResult.Failure(new IconReplacerError(
                ErrorCode.PermissionDenied,
                "Restore state could not be saved.",
                ex.Message));
        }
        catch (IOException ex)
        {
            return OperationResult.Failure(new IconReplacerError(
                ErrorCode.Unknown,
                "Restore state could not be saved.",
                ex.Message));
        }
    }

    private OperationResult<IReadOnlyList<RestoreRecord>> LoadRecords()
    {
        if (!File.Exists(_stateFilePath))
        {
            return OperationResult<IReadOnlyList<RestoreRecord>>.Success([]);
        }

        try
        {
            var json = File.ReadAllText(_stateFilePath);
            var payload = JsonSerializer.Deserialize<RestoreRecordStoreDto>(json, JsonOptions);
            return OperationResult<IReadOnlyList<RestoreRecord>>.Success(
                payload?.Records.Select(ToRecord).ToArray() ?? []);
        }
        catch (JsonException ex)
        {
            return OperationResult<IReadOnlyList<RestoreRecord>>.Failure(new IconReplacerError(
                ErrorCode.Unknown,
                "Restore state is not valid JSON.",
                ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return OperationResult<IReadOnlyList<RestoreRecord>>.Failure(new IconReplacerError(
                ErrorCode.PermissionDenied,
                "Restore state could not be read.",
                ex.Message));
        }
        catch (IOException ex)
        {
            return OperationResult<IReadOnlyList<RestoreRecord>>.Failure(new IconReplacerError(
                ErrorCode.Unknown,
                "Restore state could not be read.",
                ex.Message));
        }
    }

    private static RestoreRecordDto ToDto(RestoreRecord record)
    {
        return record.PreviousState switch
        {
            FolderRestoreSnapshot folder => new RestoreRecordDto(
                record.Id,
                record.Target.Kind,
                record.Target.FullPath,
                record.CreatedAt,
                record.AppliedIconPath,
                record.Status,
                record.RecoveryDetail,
                new FolderRestoreSnapshotDto(
                    folder.DesktopIniExisted,
                    folder.PreviousShellClassInfoValues,
                    ToInt(folder.PreviousFolderAttributes),
                    ToInt(folder.PreviousDesktopIniAttributes)),
                Shortcut: null),
            ShortcutRestoreSnapshot shortcut => new RestoreRecordDto(
                record.Id,
                record.Target.Kind,
                record.Target.FullPath,
                record.CreatedAt,
                record.AppliedIconPath,
                record.Status,
                record.RecoveryDetail,
                Folder: null,
                new ShortcutRestoreSnapshotDto(shortcut.PreviousIconPath, shortcut.PreviousIconIndex)),
            _ => throw new InvalidOperationException("Unsupported restore snapshot type.")
        };
    }

    private static RestoreRecord ToRecord(RestoreRecordDto dto)
    {
        RestoreSnapshot snapshot = dto.TargetKind switch
        {
            TargetKind.Folder when dto.Folder is not null => new FolderRestoreSnapshot(
                dto.Folder.DesktopIniExisted,
                dto.Folder.PreviousShellClassInfoValues,
                ToAttributes(dto.Folder.PreviousFolderAttributes),
                ToAttributes(dto.Folder.PreviousDesktopIniAttributes)),
            TargetKind.Shortcut when dto.Shortcut is not null => new ShortcutRestoreSnapshot(
                dto.Shortcut.PreviousIconPath,
                dto.Shortcut.PreviousIconIndex),
            _ => throw new JsonException("Restore record snapshot does not match target kind.")
        };

        return new RestoreRecord(
            dto.Id,
            new TargetItem(dto.TargetKind, dto.TargetPath),
            dto.CreatedAt,
            dto.AppliedIconPath,
            snapshot,
            dto.Status,
            dto.RecoveryDetail);
    }

    private static int? ToInt(FileAttributes? attributes)
    {
        return attributes is null ? null : (int)attributes.Value;
    }

    private static FileAttributes? ToAttributes(int? attributes)
    {
        return attributes is null ? null : (FileAttributes)attributes.Value;
    }

    private sealed record RestoreRecordStoreDto(IReadOnlyList<RestoreRecordDto> Records);

    private sealed record RestoreRecordDto(
        Guid Id,
        TargetKind TargetKind,
        string TargetPath,
        DateTimeOffset CreatedAt,
        string AppliedIconPath,
        RestoreRecordStatus Status,
        string? RecoveryDetail,
        FolderRestoreSnapshotDto? Folder,
        ShortcutRestoreSnapshotDto? Shortcut);

    private sealed record FolderRestoreSnapshotDto(
        bool DesktopIniExisted,
        IReadOnlyDictionary<string, string?> PreviousShellClassInfoValues,
        int? PreviousFolderAttributes,
        int? PreviousDesktopIniAttributes);

    private sealed record ShortcutRestoreSnapshotDto(
        string? PreviousIconPath,
        int PreviousIconIndex);
}

