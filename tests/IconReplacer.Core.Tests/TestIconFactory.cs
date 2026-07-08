namespace IconReplacer.Core.Tests;

internal static class TestIconFactory
{
    public static void WriteValidIcon(string path, byte width = 32, byte height = 32)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);

        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)1);

        writer.Write(width);
        writer.Write(height);
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((ushort)1);
        writer.Write((ushort)32);
        writer.Write((uint)4);
        writer.Write((uint)22);

        writer.Write([0, 0, 0, 0]);
    }

    public static void WritePngHeader(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]);
    }
}

