namespace IconReplacer.Core;

public sealed record IconReplacerError(
    ErrorCode Code,
    string Message,
    string? Detail = null)
{
    public static IconReplacerError None { get; } = new(ErrorCode.None, string.Empty);
}

