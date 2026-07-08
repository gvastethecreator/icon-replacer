namespace IconReplacer.Core;

public interface IExplorerChangeNotifier
{
    void NotifyUpdated(string path);
}

public sealed class WindowsExplorerChangeNotifier : IExplorerChangeNotifier
{
    private const uint UpdateItem = 0x00002000;
    private const uint UpdateDirectory = 0x00001000;
    private const uint PathWide = 0x0005;
    private const uint FlushNoWait = 0x2000;

    public void NotifyUpdated(string path)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (ArgumentException)
        {
            return;
        }
        catch (NotSupportedException)
        {
            return;
        }

        Notify(UpdateItem, fullPath);

        if (Directory.Exists(fullPath))
        {
            Notify(UpdateDirectory, fullPath);
        }

        var parent = Directory.Exists(fullPath)
            ? Directory.GetParent(fullPath)?.FullName
            : Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            Notify(UpdateDirectory, parent);
        }
    }

    private static void Notify(uint eventId, string path)
    {
        SHChangeNotify(eventId, PathWide | FlushNoWait, path, null);
    }

    [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern void SHChangeNotify(
        uint wEventId,
        uint uFlags,
        string dwItem1,
        string? dwItem2);
}

public sealed class NoOpExplorerChangeNotifier : IExplorerChangeNotifier
{
    public void NotifyUpdated(string path)
    {
    }
}
