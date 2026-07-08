namespace IconReplacer.Core.Tests;

internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        FullPath = Path.Combine(Path.GetTempPath(), "IconReplacerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(FullPath);
    }

    public string FullPath { get; }

    public string PathFor(params string[] parts)
    {
        return Path.Combine([FullPath, .. parts]);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(FullPath))
            {
                Directory.Delete(FullPath, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

