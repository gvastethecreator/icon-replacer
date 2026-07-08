using Microsoft.UI.Xaml;
using System.Text;

namespace IconReplacer.App;

public partial class App : Application
{
    public static Window Window { get; private set; } = null!;

    public static Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; private set; } = null!;

    public static IReadOnlyList<string> InitialActivationArguments { get; private set; } = [];

    public static Exception? StartupActivationException { get; private set; }

    public static nint WindowHandle =>
        WinRT.Interop.WindowNative.GetWindowHandle(Window);

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        IReadOnlyList<string> activationArguments = [];
        try
        {
            activationArguments = ParseActivationArguments(args.Arguments);
            if (activationArguments.Count == 0)
            {
                activationArguments = ReadPendingActivationArguments();
            }
        }
        catch (Exception ex)
        {
            StartupActivationException = ex;
        }

        InitialActivationArguments = activationArguments;
        Window = new MainWindow();
        DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        Window.Activate();
    }

    private static IReadOnlyList<string> ParseActivationArguments(string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return [];
        }

        var parsed = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        foreach (var character in arguments)
        {
            if (character == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(character) && !inQuotes)
            {
                FlushCurrent();
                continue;
            }

            current.Append(character);
        }

        FlushCurrent();
        return parsed;

        void FlushCurrent()
        {
            if (current.Length == 0)
            {
                return;
            }

            parsed.Add(current.ToString());
            current.Clear();
        }
    }

    private static IReadOnlyList<string> ReadPendingActivationArguments()
    {
        var pendingFile = GetPendingActivationFile();
        if (!File.Exists(pendingFile))
        {
            return [];
        }

        try
        {
            var arguments = File.ReadAllText(pendingFile);
            File.Delete(pendingFile);
            return ParseActivationArguments(arguments);
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static string GetPendingActivationFile()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "Icon Replacer", "pending-activation.args");
    }
}
