namespace IconReplacer.Core.Tests;

public sealed class WindowsOnlyFactAttribute : FactAttribute
{
    public WindowsOnlyFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "Windows Shell link COM APIs are only available on Windows.";
        }
    }
}

