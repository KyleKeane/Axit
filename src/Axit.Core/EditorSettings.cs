using Axit.Core;

namespace Axit.Core;

/// <summary>
/// Everything AxDown remembers between runs (docs/axdown/SPEC.md AD-8), in <c>%APPDATA%\Axit\axdown.json</c> through the
/// bundle's <see cref="SettingsFile"/>. A missing or unreadable file falls back to the defaults.
/// </summary>
public sealed class EditorSettings : IWindowSettings
{
    /// <summary>Null means the Windows system font (which follows the text size setting under Accessibility).</summary>
    public string? FontFamily { get; set; }

    /// <summary>Point size; 0 means the system font size.</summary>
    public float FontSize { get; set; }

    public bool FontBold { get; set; }
    public bool WordWrap { get; set; } = true;

    /// <summary>Ask GitHub once at startup whether a newer release exists (docs/axit/SPEC.md AX-3).</summary>
    public bool CheckForUpdates { get; set; } = true;

    /// <summary>The last files opened or saved under a name, newest first, for File → Open recent file (AD-4.1); at most <see cref="MaxRecentFiles"/>.</summary>
    public List<string> RecentFiles { get; set; } = [];

    public const int MaxRecentFiles = 10;

    public WindowPlacement? Window { get; set; }

    /// <summary>Loads the file, or returns defaults. <paramref name="error"/> describes an unreadable file.</summary>
    public static EditorSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<EditorSettings>(path, out error);
        settings.Sanitize();
        return settings;
    }

    public void Save(string path) => SettingsFile.Save(path, this);

    /// <summary>Puts the file at the front of <see cref="RecentFiles"/>, once, and keeps the list to <see cref="MaxRecentFiles"/>.</summary>
    public void RememberFile(string path)
    {
        RecentFiles.RemoveAll(f => string.Equals(f, path, StringComparison.OrdinalIgnoreCase));
        RecentFiles.Insert(0, path);
        Trim();
    }

    private void Trim()
    {
        if (RecentFiles.Count > MaxRecentFiles)
        {
            RecentFiles.RemoveRange(MaxRecentFiles, RecentFiles.Count - MaxRecentFiles);
        }
    }

    private void Sanitize()
    {
        RecentFiles ??= [];
        RecentFiles.RemoveAll(string.IsNullOrWhiteSpace);
        Trim();

        if (FontSize is < 0 or > 96)
        {
            FontSize = 0;
        }

        if (string.IsNullOrWhiteSpace(FontFamily))
        {
            FontFamily = null;
        }

        if (Window is { } w && (w.Width < 200 || w.Height < 150))
        {
            Window = null;
        }
    }
}
