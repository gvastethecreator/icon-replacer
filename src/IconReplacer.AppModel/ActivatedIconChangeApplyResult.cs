namespace IconReplacer.AppModel;

public sealed record ActivatedIconChangeApplyResult(
    AppActivationSnapshot Activation,
    IconChangeResult ChangeResult,
    DateTimeOffset RefreshedAt);
