using System.Text.Json.Serialization;
using Axit.Core;

namespace AxStart.Core;

/// <summary>
/// Everything AxStart remembers between runs (SPEC.md AS-5.1), in <c>%APPDATA%\Axit\axstart.json</c> through the
/// bundle's <see cref="SettingsFile"/>: the font and the update check from <see cref="WindowSettings"/> (the window's
/// place is not used: the window is centred), plus the hotkey, Start with Windows, Say the first match while typing,
/// and how often AxStart started each program, for the order of the matches. A missing or unreadable file falls back
/// to the defaults.
/// </summary>
public sealed class StartSettings : WindowSettings
{
    /// <summary>The hotkey as text, "Ctrl+Shift+Space" (AS-4.1), the file's <c>hotkey</c>; <see cref="Key"/> reads it. (Named after the record until 2026-10-23, which hid the record's name inside this class.)</summary>
    [JsonPropertyName("hotkey")]
    public string HotkeyText { get; set; } = Hotkey.Default.ToString();

    /// <summary>AxStart starts hidden at sign-in (AS-1.3).</summary>
    public bool StartWithWindows { get; set; } = true;

    /// <summary>The first match and the count said after the typing pauses (AS-3.2).</summary>
    public bool SayFirstMatch { get; set; } = true;

    /// <summary>Each program's display name and how often AxStart started it (AS-2.2).</summary>
    public Dictionary<string, int> Starts { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static string DefaultPath => AppPaths.SettingsFile("axstart");

    /// <summary>The hotkey of the settings, or the default when the text is not one.</summary>
    public Hotkey Key => Hotkey.Parse(HotkeyText) is { } key && key.Refusal() is null ? key : Hotkey.Default;

    public static StartSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<StartSettings>(path, out error);
        settings.Sanitize();
        return settings;
    }

    public override void Save(string path) => SettingsFile.Save(path, this);

    /// <summary>One more start of the program (AS-2.3).</summary>
    public void Started(string name) => Starts[name] = (Starts.TryGetValue(name, out var count) ? count : 0) + 1;

    private void Sanitize()
    {
        SanitizeWindow();
        HotkeyText = Key.ToString();
        // Read back with the serializer's own comparer: names differing in case only are added together.
        var starts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, count) in Starts ?? [])
        {
            if (!string.IsNullOrWhiteSpace(name) && count > 0)
            {
                starts[name] = (starts.TryGetValue(name, out var before) ? before : 0) + count;
            }
        }

        Starts = starts;
    }
}
