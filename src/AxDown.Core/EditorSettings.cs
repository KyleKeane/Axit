using Axit.Core;

namespace AxDown.Core;

/// <summary>
/// Everything AxDown remembers between runs (SPEC.md AD-8), in <c>%APPDATA%\Axit\axdown.json</c> through the
/// bundle's <see cref="SettingsFile"/>. A missing or unreadable file falls back to the defaults.
/// </summary>
public sealed class EditorSettings
{
    /// <summary>Null means the Windows system font (which follows the text size setting under Accessibility).</summary>
    public string? FontFamily { get; set; }

    /// <summary>Point size; 0 means the system font size.</summary>
    public float FontSize { get; set; }

    public bool FontBold { get; set; }
    public bool WordWrap { get; set; } = true;

    /// <summary>Ask GitHub once at startup whether a newer release exists (docs/axit/SPEC.md AX-3).</summary>
    public bool CheckForUpdates { get; set; } = true;

    public WindowPlacement? Window { get; set; }

    public static string DefaultPath => AppPaths.SettingsFile("axdown");

    /// <summary>Loads the file, or returns defaults. <paramref name="error"/> describes an unreadable file.</summary>
    public static EditorSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<EditorSettings>(path, out error);
        settings.Sanitize();
        return settings;
    }

    public void Save(string path) => SettingsFile.Save(path, this);

    private void Sanitize()
    {
        if (FontSize is < 0 or > 96) FontSize = 0;
        if (string.IsNullOrWhiteSpace(FontFamily)) FontFamily = null;
        if (Window is { } w && (w.Width < 200 || w.Height < 150)) Window = null;
    }
}
