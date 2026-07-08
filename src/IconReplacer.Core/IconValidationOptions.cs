namespace IconReplacer.Core;

public sealed record IconValidationOptions(long MaxLengthBytes = 20 * 1024 * 1024, int MaxImageCount = 64)
{
    public static IconValidationOptions Default { get; } = new();
}

