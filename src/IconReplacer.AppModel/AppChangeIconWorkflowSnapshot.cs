using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record AppChangeIconWorkflowSnapshot(
    AppActivationSnapshot Activation,
    AppLaunchRequestSnapshot? LaunchRequest,
    ActivatedIconChangePreviewSnapshot? Preview,
    string? SelectedIconPath,
    AppChangeIconWorkflowStep Step,
    bool CanOpenPicker,
    bool CanPreview,
    bool CanApply,
    IconReplacerError Error,
    DateTimeOffset RefreshedAt);
