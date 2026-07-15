using IconReplacer.App.ViewModels;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IconReplacer.App;

internal sealed class ThemePreferenceStore
{
    private readonly string _settingsFile;

    public ThemePreferenceStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _settingsFile = Path.Combine(appData, "Icon Replacer", "preferences.json");
    }

    public AppThemePreference Load()
    {
        try
        {
            if (!File.Exists(_settingsFile))
            {
                return AppThemePreference.System;
            }

            var settings = JsonSerializer.Deserialize(
                File.ReadAllText(_settingsFile),
                ThemeSettingsJsonContext.Default.ThemeSettings);
            return Enum.TryParse<AppThemePreference>(settings?.Theme, ignoreCase: true, out var preference)
                ? preference
                : AppThemePreference.System;
        }
        catch (JsonException)
        {
            return AppThemePreference.System;
        }
        catch (IOException)
        {
            return AppThemePreference.System;
        }
        catch (UnauthorizedAccessException)
        {
            return AppThemePreference.System;
        }
    }

    public void Save(AppThemePreference preference)
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsFile)!;
            Directory.CreateDirectory(directory);
            var json = JsonSerializer.Serialize(
                new ThemeSettings(preference.ToString()),
                ThemeSettingsJsonContext.Default.ThemeSettings);
            File.WriteAllText(_settingsFile, json);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

internal sealed record ThemeSettings(string Theme);

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ThemeSettings))]
internal partial class ThemeSettingsJsonContext : JsonSerializerContext;
