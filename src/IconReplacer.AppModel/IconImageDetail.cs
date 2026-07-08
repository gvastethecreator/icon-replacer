namespace IconReplacer.AppModel;

public sealed record IconImageDetail(
    int Width,
    int Height,
    ushort BitCount,
    uint BytesInResource,
    uint ImageOffset,
    bool HasUsefulSize);
