using System.Text;

namespace IconReplacer.Core;

public sealed class DesktopIniDocument
{
    public const string ShellClassInfoSection = ".ShellClassInfo";
    private readonly Dictionary<string, Dictionary<string, string>> _sections;

    private DesktopIniDocument(Dictionary<string, Dictionary<string, string>> sections)
    {
        _sections = sections;
    }

    public static DesktopIniDocument Empty()
    {
        return new DesktopIniDocument(new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase));
    }

    public static DesktopIniDocument Load(string path)
    {
        if (!File.Exists(path))
        {
            return Empty();
        }

        var document = Empty();
        string? currentSection = null;

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith(';'))
            {
                continue;
            }

            if (line.StartsWith('[') && line.EndsWith(']') && line.Length > 2)
            {
                currentSection = line[1..^1];
                document.EnsureSection(currentSection);
                continue;
            }

            if (currentSection is null)
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            document.SetValue(currentSection, key, value);
        }

        return document;
    }

    public string? GetValue(string section, string key)
    {
        return _sections.TryGetValue(section, out var values) &&
            values.TryGetValue(key, out var value)
                ? value
                : null;
    }

    public IReadOnlyDictionary<string, string?> CaptureValues(string section, params string[] keys)
    {
        return keys.ToDictionary(
            key => key,
            key => GetValue(section, key),
            StringComparer.OrdinalIgnoreCase);
    }

    public void SetValue(string section, string key, string value)
    {
        EnsureSection(section)[key] = value;
    }

    public void RemoveValue(string section, string key)
    {
        if (!_sections.TryGetValue(section, out var values))
        {
            return;
        }

        values.Remove(key);
        if (values.Count == 0)
        {
            _sections.Remove(section);
        }
    }

    public bool HasAnyValues => _sections.Values.Any(section => section.Count > 0);

    public void Save(string path)
    {
        var builder = new StringBuilder();
        foreach (var section in _sections.OrderBy(section => section.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (section.Value.Count == 0)
            {
                continue;
            }

            builder.Append('[').Append(section.Key).AppendLine("]");
            foreach (var pair in section.Value.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                builder.Append(pair.Key).Append('=').AppendLine(pair.Value);
            }

            builder.AppendLine();
        }

        File.WriteAllText(path, builder.ToString(), Encoding.Unicode);
    }

    private Dictionary<string, string> EnsureSection(string section)
    {
        if (!_sections.TryGetValue(section, out var values))
        {
            values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _sections[section] = values;
        }

        return values;
    }
}

