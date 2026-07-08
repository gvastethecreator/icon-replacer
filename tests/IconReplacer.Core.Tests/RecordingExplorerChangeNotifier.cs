using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

internal sealed class RecordingExplorerChangeNotifier : IExplorerChangeNotifier
{
    public List<string> UpdatedPaths { get; } = [];

    public void NotifyUpdated(string path)
    {
        UpdatedPaths.Add(path);
    }
}

