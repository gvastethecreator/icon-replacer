namespace IconReplacer.AppModel;

public sealed record IconChangeResult(
    ShellSelectionEvaluation Selection,
    IconApplyResult ApplyResult);
