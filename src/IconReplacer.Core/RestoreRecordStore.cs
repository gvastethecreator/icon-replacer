using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IconReplacer.Core;

public sealed class RestoreRecordStore
{
    private static readonly TimeSpan StateLockTimeout = TimeSpan.FromSeconds(10);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _stateFilePath;
    private readonly string _stateLockName;

    public RestoreRecordStore(string stateFilePath)
    {
        _stateFilePath = stateFilePath;
        _stateLockName = CreateStateLockName(stateFilePath);
    }

    public OperationResult<IReadOnlyList<RestoreRecord>> List()
    {
        return WithStateLock(
            () =>
            {
                var records = LoadRecords();
                if (!records.Succeeded || records.Value is null)
                {
                    return OperationResult<IReadOnlyList<RestoreRecord>>.Failure(records.Error);
                }

                return OperationResult<IReadOnlyList<RestoreRecord>>.Success(records.Value);
            },
            OperationResult<IReadOnlyList<RestoreRecord>>.Failure);
    }

    public OperationResult<RestoreRecord?> Get(Guid id)
    {
        return WithStateLock(
            () =>
            {
                var records = LoadRecords();
                if (!records.Succeeded || records.Value is null)
                {
                    return OperationResult<RestoreRecord?>.Failure(records.Error);
                }

                return OperationResult<RestoreRecord?>.Success(
                    records.Value.FirstOrDefault(record => record.Id == id));
            },
            OperationResult<RestoreRecord?>.Failure);
    }

    public OperationResult Upsert(RestoreRecord record)
    {
        return WithStateLock(
            () =>
            {
                var records = LoadRecords();
                if (!records.Succeeded || records.Value is null)
                {
                    return OperationResult.Failure(records.Error);
                }

                var nextRecords = records.Value
                    .Where(existing => existing.Id != record.Id)
                    .Append(record)
                    .OrderByDescending(existing => existing.CreatedAt)
                    .ToArray();

                var payload = new RestoreRecordStoreDto(nextRecords.Select(ToDto).ToArray());
                return SaveRecords(payload);
            },
            OperationResult.Failure);
    }

    private OperationResult SaveRecords(RestoreRecordStoreDto payload)
    {
        string? temporaryPath = null;
        try
        {
            var directory = Path.GetDirectoryName(_stateFilePath)!;
            Directory.CreateDirectory(directory);
            temporaryPath = Path.Combine(
                directory,
                $"{Path.GetFileName(_stateFilePath)}.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp");

            using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.WriteThrough))
            {
                JsonSerializer.Serialize(stream, payload, JsonOptions);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, _stateFilePath, overwrite: true);
            temporaryPath = null;
            return OperationResult.Success();
        }
        catch (UnauthorizedAccessException ex)
        {
            return SaveFailure(ErrorCode.PermissionDenied, ex);
        }
        catch (IOException ex)
        {
            return SaveFailure(ErrorCode.Unknown, ex);
        }
        finally
        {
            if (temporaryPath is not null)
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
    }

    private static OperationResult SaveFailure(ErrorCode code, Exception exception) =>
        OperationResult.Failure(new IconReplacerError(
            code,
            "Restore state could not be saved.",
            exception.Message));

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

    private T WithStateLock<T>(Func<T> operation, Func<IconReplacerError, T> failure)
        where T : OperationResult
    {
        using var stateLock = new Mutex(initiallyOwned: false, _stateLockName);
        var lockTaken = false;
        try
        {
            try
            {
                lockTaken = stateLock.WaitOne(StateLockTimeout);
            }
            catch (AbandonedMutexException)
            {
                lockTaken = true;
            }

            if (!lockTaken)
            {
                return failure(new IconReplacerError(
                    ErrorCode.Unknown,
                    "Restore state is busy. Try the operation again."));
            }

            return operation();
        }
        catch (UnauthorizedAccessException ex)
        {
            return failure(CreateLockError(ex));
        }
        catch (IOException ex)
        {
            return failure(CreateLockError(ex));
        }
        finally
        {
            if (lockTaken)
            {
                stateLock.ReleaseMutex();
            }
        }
    }

    private static IconReplacerError CreateLockError(Exception exception) =>
        new(
            exception is UnauthorizedAccessException ? ErrorCode.PermissionDenied : ErrorCode.Unknown,
            "Restore state could not be locked.",
            exception.Message);

    private static string CreateStateLockName(string stateFilePath)
    {
        var canonicalPath = Path.GetFullPath(stateFilePath).ToUpperInvariant();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalPath)));
        return $"Local\\IconReplacer.RestoreState.{hash}";
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
