namespace IconReplacer.Core;

public interface IExplorerChangeNotifier
{
    void NotifyUpdated(string path);
}

public sealed class NoOpExplorerChangeNotifier : IExplorerChangeNotifier
{
    public void NotifyUpdated(string path)
    {
    }
}

