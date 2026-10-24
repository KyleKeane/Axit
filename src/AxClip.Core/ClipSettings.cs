using System.Text.Json;
using System.Text.Json.Serialization;
using Axit.Core;

namespace AxClip.Core;

/// <summary>
/// Everything axClip remembers between runs (SPEC.md CP-6.1), in <c>%APPDATA%\Axit\axclip.json</c> through the
/// bundle's <see cref="SettingsFile"/>: the font and the update check from <see cref="WindowSettings"/>, the hotkey,
/// Start with Windows, Say the first match while typing, Keep history between runs, and the history only while that
/// is on (CP-D2).
/// </summary>
public sealed class ClipSettings : WindowSettings
{
    /// <summary>Windows+Alt+V (CP-D1).</summary>
    public static readonly Hotkey DefaultHotkey = new(Hotkey.Windows | Hotkey.Alt, 'V');

    /// <summary>The hotkey as text, the file's <c>hotkey</c>; <see cref="Key"/> reads it.</summary>
    [JsonPropertyName("hotkey")]
    public string HotkeyText { get; set; } = DefaultHotkey.ToString();

    public bool StartWithWindows { get; set; } = true;

    public bool SayFirstMatch { get; set; } = true;

    public bool KeepHistory { get; set; }

    /// <summary>The items, newest first, kept only while <see cref="KeepHistory"/> is on; on disk only as <see cref="ProtectedHistory"/>.</summary>
    [JsonIgnore]
    public List<ClipEntry> History { get; set; } = [];

    /// <summary>
    /// The history as the file holds it (security audit S-21): encrypted for this Windows account (<see cref="UserSecret"/>),
    /// so that the file in a backup, a roaming profile or another user's hands shows nothing of what was copied.
    /// </summary>
    [JsonPropertyName("protectedHistory")]
    public string? ProtectedHistory { get; set; }

    public static string DefaultPath => AppPaths.SettingsFile("axclip");

    /// <summary>The hotkey of the settings, or the default when the text is not a usable one.</summary>
    public Hotkey Key => Hotkey.Parse(HotkeyText) is { } key && key.Refusal() is null ? key : DefaultHotkey;

    public static ClipSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<ClipSettings>(path, out error);
        settings.Sanitize();
        return settings;
    }

    public override void Save(string path)
    {
        ProtectedHistory = KeepHistory && History.Count > 0 ? UserSecret.Protect(JsonSerializer.Serialize(History)) : null;
        SettingsFile.Save(path, this);
    }

    /// <summary>The window's history into the settings, or nothing of it with Keep history off.</summary>
    public void Keep(IEnumerable<ClipEntry> history) =>
        History = KeepHistory ? history.Take(ClipHistory.MaxItems).ToList() : [];

    private void Sanitize()
    {
        SanitizeWindow();
        HotkeyText = Key.ToString();
        History = KeepHistory && ProtectedHistory is { } kept && UserSecret.Unprotect(kept) is { } json ? Read(json) : [];
        History = History.Where(entry => entry is { Text.Length: > 0 and <= ClipPolicy.MaxLength }).Take(ClipHistory.MaxItems).ToList();
    }

    private static List<ClipEntry> Read(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<ClipEntry>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
