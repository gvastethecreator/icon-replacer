using System.Security.Cryptography;
using System.Text;
using IconReplacer.Core;

namespace IconReplacer.AppModel;

internal sealed class TargetMutationCoordinator
{
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    public OperationResult<T> Run<T>(
        string targetPath,
        Func<OperationResult<T>> operation)
    {
        string lockName;
        try
        {
            lockName = CreateLockName(targetPath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return OperationResult<T>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "The target path is not valid.",
                ex.Message));
        }

        Mutex? targetLock = null;
        var lockTaken = false;
        try
        {
            targetLock = new Mutex(initiallyOwned: false, lockName);
            try
            {
                lockTaken = targetLock.WaitOne(LockTimeout);
            }
            catch (AbandonedMutexException)
            {
                lockTaken = true;
            }

            if (!lockTaken)
            {
                return OperationResult<T>.Failure(new IconReplacerError(
                    ErrorCode.Unknown,
                    "Another icon operation is still using this target. Try again."));
            }

            return operation();
        }
        catch (UnauthorizedAccessException ex)
        {
            return LockFailure<T>(ErrorCode.PermissionDenied, ex);
        }
        catch (IOException ex)
        {
            return LockFailure<T>(ErrorCode.Unknown, ex);
        }
        catch (WaitHandleCannotBeOpenedException ex)
        {
            return LockFailure<T>(ErrorCode.Unknown, ex);
        }
        finally
        {
            if (lockTaken)
            {
                targetLock!.ReleaseMutex();
            }

            targetLock?.Dispose();
        }
    }

    private static OperationResult<T> LockFailure<T>(ErrorCode code, Exception exception) =>
        OperationResult<T>.Failure(new IconReplacerError(
            code,
            "The target could not be reserved for an icon operation.",
            exception.Message));

    private static string CreateLockName(string targetPath)
    {
        var canonicalPath = Path.GetFullPath(targetPath).ToUpperInvariant();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalPath)));
        return $"Local\\IconReplacer.TargetMutation.{hash}";
    }
}
