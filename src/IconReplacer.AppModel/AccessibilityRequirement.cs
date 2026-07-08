namespace IconReplacer.AppModel;

public sealed record AccessibilityRequirement(
    string Id,
    AccessibilityRequirementCategory Category,
    string Title,
    string Detail,
    bool RequiredForV1,
    bool ManualProofRequired);
