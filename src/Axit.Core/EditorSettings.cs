namespace Axit.Core;

/// <summary>
/// Everything an editor window remembers between runs (docs/axdown/SPEC.md AD-8), in <c>%APPDATA%\Axit\axdown.json</c>
/// (or the app's own file) through the bundle's <see cref="SettingsFile"/>: the font, the update check and the
/// window's place from <see cref="WindowSettings"/>, plus word wrap and the recent files. A missing or unreadable
/// file falls back to the defaults.
/// </summary>
public sealed class EditorSettings : WindowSettings
{
    public bool WordWrap { get; set; } = true;

    /// <summary>The last files opened or saved under a name, newest first, for File → Open recent file (AD-4.1); at most <see cref="RecentPaths.Max"/>.</summary>
    public List<string> RecentFiles { get; set; } = [];


    /// <summary>Loads the file, or returns defaults. <paramref name="error"/> describes an unreadable file.</summary>
    public static EditorSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<EditorSettings>(path, out error);
        settings.Sanitize();
        return settings;
    }

    public override void Save(string path) => SettingsFile.Save(path, this);

    /// <summary>Puts the file at the front of <see cref="RecentFiles"/>, once, and keeps the list to <see cref="RecentPaths.Max"/>.</summary>
    public void RememberFile(string path) => RecentPaths.Remember(RecentFiles, path);

    private void Sanitize()
    {
        SanitizeWindow();
        RecentFiles = RecentPaths.Sanitize(RecentFiles);
    }
}
