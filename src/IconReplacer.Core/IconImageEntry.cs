namespace IconReplacer.Core;

public sealed record IconImageEntry(
    int Width,
    int Height,
    byte ColorCount,
    ushort Planes,
    ushort BitCount,
    uint BytesInResource,
    uint ImageOffset)
{
    public bool HasUsefulSize => Width is 16 or 32 or 48 or 256 || Height is 16 or 32 or 48 or 256;
}

