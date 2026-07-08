using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class ShellSelectionService
{
    public OperationResult<ShellSelectionEvaluation> EvaluatePath(string path)
    {
        return Evaluate([new ShellSelectionItem(path)]);
    }

    public OperationResult<ShellSelectionEvaluation> Evaluate(IReadOnlyList<ShellSelectionItem> selection)
    {
        if (selection is null)
        {
            return OperationResult<ShellSelectionEvaluation>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "A shell selection is required."));
        }

        if (selection.Count == 0)
        {
            return OperationResult<ShellSelectionEvaluation>.Success(ShellSelectionEvaluation.Disabled(
                ShellSelectionStatus.NoSelection,
                new IconReplacerError(
                    ErrorCode.InvalidArgument,
                    "Select one folder or .lnk shortcut to change its icon.")));
        }

        if (selection.Count > 1)
        {
            return OperationResult<ShellSelectionEvaluation>.Success(ShellSelectionEvaluation.Disabled(
                ShellSelectionStatus.MultipleSelectionUnsupported,
                new IconReplacerError(
                    ErrorCode.UnsupportedTarget,
                    "Multiple-selection icon changes are not supported in V1.",
                    $"{selection.Count} items selected.")));
        }

        return EvaluateSingle(selection[0]);
    }

    private static OperationResult<ShellSelectionEvaluation> EvaluateSingle(ShellSelectionItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Path))
        {
            return OperationResult<ShellSelectionEvaluation>.Success(ShellSelectionEvaluation.Disabled(
                ShellSelectionStatus.InvalidPath,
                new IconReplacerError(
                    ErrorCode.InvalidArgument,
                    "A target path is required.")));
        }

        if (FileSystemPathPolicy.IsRemoteOrUnsupported(item.Path))
        {
            return OperationResult<ShellSelectionEvaluation>.Success(ShellSelectionEvaluation.Disabled(
                ShellSelectionStatus.RemotePathUnsupported,
                new IconReplacerError(
                    ErrorCode.RemotePathUnsupported,
                    "Remote or web-backed targets are not supported in V1.",
                    item.Path),
                item.Path));
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(item.Path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return OperationResult<ShellSelectionEvaluation>.Success(ShellSelectionEvaluation.Disabled(
                ShellSelectionStatus.InvalidPath,
                new IconReplacerError(
                    ErrorCode.InvalidArgument,
                    "The target path is not valid.",
                    ex.Message),
                item.Path));
        }

        if (item.IsDirectory is true)
        {
            if (!Directory.Exists(fullPath))
            {
                return MissingTarget(fullPath);
            }

            return EvaluateTarget(fullPath, isDirectory: true);
        }

        if (item.IsDirectory is false)
        {
            if (!File.Exists(fullPath))
            {
                return MissingTarget(fullPath);
            }

            return EvaluateTarget(fullPath, isDirectory: false);
        }

        if (Directory.Exists(fullPath))
        {
            return EvaluateTarget(fullPath, isDirectory: true);
        }

        if (File.Exists(fullPath))
        {
            return EvaluateTarget(fullPath, isDirectory: false);
        }

        return MissingTarget(fullPath);
    }

    private static OperationResult<ShellSelectionEvaluation> EvaluateTarget(string fullPath, bool isDirectory)
    {
        var target = TargetItem.FromShellSelection(fullPath, isDirectory);
        if (target.Succeeded && target.Value is not null)
        {
            return OperationResult<ShellSelectionEvaluation>.Success(
                ShellSelectionEvaluation.Enabled(target.Value));
        }

        return OperationResult<ShellSelectionEvaluation>.Success(ShellSelectionEvaluation.Disabled(
            ShellSelectionStatus.UnsupportedTarget,
            target.Error,
            fullPath));
    }

    private static OperationResult<ShellSelectionEvaluation> MissingTarget(string fullPath)
    {
        return OperationResult<ShellSelectionEvaluation>.Success(ShellSelectionEvaluation.Disabled(
            ShellSelectionStatus.MissingTarget,
            new IconReplacerError(
                ErrorCode.PathNotFound,
                "The selected target does not exist.",
                fullPath),
            fullPath));
    }
}
