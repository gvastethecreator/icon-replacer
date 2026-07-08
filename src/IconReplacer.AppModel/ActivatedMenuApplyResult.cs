namespace IconReplacer.AppModel;

public sealed record ActivatedMenuApplyResult(
    AppActivationSnapshot Activation,
    IconMenuApplyResult MenuApplyResult,
    DateTimeOffset AppliedAt);
