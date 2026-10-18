using Axit.Core;

namespace AxGit.Core;

/// <summary>
/// Everything AxGit remembers between runs (SPEC.md AG-8.1), in <c>%APPDATA%\Axit\axgit.json</c> through the bundle's
/// <see cref="SettingsFile"/>. A missing or unreadable file falls back to the defaults.
/// </summary>
public sealed class GitSettings : IWindowSettings
{
    public const int MaxRecentRepositories = 10;

    /// <summary>Null means the Windows system font.</summary>
    public string? FontFamily { get; set; }

    /// <summary>Point size; 0 means the system font size.</summary>
    public float FontSize { get; set; }

    public bool FontBold { get; set; }

    /// <summary>Ask GitHub once at startup whether a newer release exists (docs/axit/SPEC.md AX-3).</summary>
    public bool CheckForUpdates { get; set; } = true;

    /// <summary>The repositories opened, newest first, for Repository → Open recent repository and the start without a path.</summary>
    public List<string> RecentRepositories { get; set; } = [];

    public WindowPlacement? Window { get; set; }

    public static string DefaultPath => AppPaths.SettingsFile("axgit");

    public static GitSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<GitSettings>(path, out error);
        settings.Sanitize();
        return settings;
    }

    public void Save(string path) => SettingsFile.Save(path, this);

    /// <summary>Puts the repository at the front of <see cref="RecentRepositories"/>, once, and keeps the list to <see cref="MaxRecentRepositories"/>.</summary>
    public void RememberRepository(string root)
    {
        RecentRepositories.RemoveAll(r => string.Equals(r, root, StringComparison.OrdinalIgnoreCase));
        RecentRepositories.Insert(0, root);
        Trim();
    }

    private void Trim()
    {
        if (RecentRepositories.Count > MaxRecentRepositories)
        {
            RecentRepositories.RemoveRange(MaxRecentRepositories, RecentRepositories.Count - MaxRecentRepositories);
        }
    }

    private void Sanitize()
    {
        RecentRepositories ??= [];
        RecentRepositories.RemoveAll(string.IsNullOrWhiteSpace);
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
