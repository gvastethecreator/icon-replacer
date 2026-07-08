using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record ActivatedIconChangePreviewSnapshot(
    AppActivationSnapshot Activation,
    IconChangePreviewSnapshot Preview,
    bool CanApply,
    IconReplacerError Error,
    DateTimeOffset RefreshedAt);
