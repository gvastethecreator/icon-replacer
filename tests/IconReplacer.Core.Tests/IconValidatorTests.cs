using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class IconValidatorTests
{
    [Fact]
    public void ValidateAcceptsStructurallyValidIco()
    {
        using var temp = new TempDirectory();
        var iconPath = temp.PathFor("valid.ico");
        TestIconFactory.WriteValidIcon(iconPath);

        var result = IconValidator.Validate(iconPath);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal(Path.GetFullPath(iconPath), result.Value.FullPath);
        Assert.Single(result.Value.Images);
        Assert.Equal(32, result.Value.Images[0].Width);
        Assert.Equal(32, result.Value.Images[0].Height);
    }

    [Fact]
    public void ValidateRejectsRenamedPng()
    {
        using var temp = new TempDirectory();
        var iconPath = temp.PathFor("renamed.ico");
        TestIconFactory.WritePngHeader(iconPath);

        var result = IconValidator.Validate(iconPath);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.InvalidIcon, result.Error.Code);
    }

    [Fact]
    public void ValidateRejectsTruncatedDirectory()
    {
        using var temp = new TempDirectory();
        var iconPath = temp.PathFor("truncated.ico");
        File.WriteAllBytes(iconPath, [0, 0, 1, 0, 1, 0]);

        var result = IconValidator.Validate(iconPath);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.InvalidIcon, result.Error.Code);
    }

    [Fact]
    public void ValidateRejectsImageOutsideFile()
    {
        using var temp = new TempDirectory();
        var iconPath = temp.PathFor("outside.ico");

        using (var stream = File.Create(iconPath))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)1);
            writer.Write((byte)32);
            writer.Write((byte)32);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write((uint)100);
            writer.Write((uint)22);
            writer.Write([0, 0, 0, 0]);
        }

        var result = IconValidator.Validate(iconPath);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.InvalidIcon, result.Error.Code);
    }

    [Fact]
    public void ValidateRejectsIconsWithoutCommonWindowsSize()
    {
        using var temp = new TempDirectory();
        var iconPath = temp.PathFor("odd-size.ico");
        TestIconFactory.WriteValidIcon(iconPath, width: 17, height: 17);

        var result = IconValidator.Validate(iconPath);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.InvalidIcon, result.Error.Code);
    }

    [Fact]
    public void ValidateRejectsOversizedIcon()
    {
        using var temp = new TempDirectory();
        var iconPath = temp.PathFor("valid.ico");
        TestIconFactory.WriteValidIcon(iconPath);

        var result = IconValidator.Validate(iconPath, new IconValidationOptions(MaxLengthBytes: 3));

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.InvalidIcon, result.Error.Code);
    }

    [Fact]
    public void ValidateRejectsUnsupportedExtension()
    {
        using var temp = new TempDirectory();
        var iconPath = temp.PathFor("valid.png");
        TestIconFactory.WritePngHeader(iconPath);

        var result = IconValidator.Validate(iconPath);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.InvalidIcon, result.Error.Code);
    }

    [Fact]
    public void ValidateRejectsRemotePaths()
    {
        var result = IconValidator.Validate(@"\\server\share\icon.ico");

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.RemotePathUnsupported, result.Error.Code);
    }
}

