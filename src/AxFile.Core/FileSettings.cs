using Axit.Core;

namespace AxFile.Core;

/// <summary>
/// Everything axFile remembers between runs (SPEC.md AF-8.1), in <c>%APPDATA%\Axit\axfile.json</c> through the bundle's
/// <see cref="SettingsFile"/>: the font, the update check and the window's place from <see cref="WindowSettings"/>,
/// plus the last folder, the recent folders, the favorites, whether hidden entries show, the sort order and the user's
/// own programs. A missing or unreadable file falls back to the defaults; the player's keys of an older file (speed,
/// seekSeconds, sayEachFile) are axMedia's now and are ignored here.
/// </summary>
public sealed class FileSettings : WindowSettings
{

    /// <summary>The folder the window had when it closed, for the start without a path.</summary>
    public string? LastFolder { get; set; }

    /// <summary>The folders opened by command (the start, Open folder, Go to folder, a recent item, the close), newest first, for Folder → Open recent folder.</summary>
    public List<string> RecentFolders { get; set; } = [];

    /// <summary>Hidden and system entries in the list (AF-2.2); off, as in File Explorer.</summary>
    public bool ShowHidden { get; set; }

    /// <summary>Entries whose name starts with a period (.git, .env) in the list (AF-2.2); off.</summary>
    public bool ShowDotFiles { get; set; }

    /// <summary>The list's order (AF-3.8), kept for the next folder and the next window.</summary>
    public SortBy SortBy { get; set; } = SortBy.Name;

    /// <summary>The folders the user pinned with Ctrl+D (AF-3.9), in the order added, for Folder → Favorites.</summary>
    public List<string> Favorites { get; set; } = [];

    /// <summary>The user's own programs and scripts of Open with a custom program ▸ (AF-4.11), in the order added.</summary>
    public List<UserProgram> Programs { get; set; } = [];

    public static string DefaultPath => AppPaths.SettingsFile("axfile");

    public static FileSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<FileSettings>(path, out error);
        settings.Sanitize();
        return settings;
    }

    public override void Save(string path) => SettingsFile.Save(path, this);

    /// <summary>Puts the folder at the front of <see cref="RecentFolders"/>, once, keeps the list to <see cref="RecentPaths.Max"/>, and makes it the last folder.</summary>
    public void RememberFolder(string folder)
    {
        LastFolder = folder;
        RecentPaths.Remember(RecentFolders, folder);
    }

    private void Sanitize()
    {
        SanitizeWindow();
        RecentFolders = RecentPaths.Sanitize(RecentFolders);
        if (string.IsNullOrWhiteSpace(LastFolder))
        {
            LastFolder = null;
        }

        Programs ??= [];
        Programs.RemoveAll(program => string.IsNullOrWhiteSpace(program?.Name) || string.IsNullOrWhiteSpace(program?.Path));
        if (!Enum.IsDefined(SortBy))
        {
            SortBy = SortBy.Name;
        }

        Favorites ??= [];
        Favorites.RemoveAll(string.IsNullOrWhiteSpace);
        Favorites = Favorites.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Whether the folder is a favorite (AF-3.9).</summary>
    public bool IsFavorite(string folder) => Favorites.Any(f => string.Equals(f, folder, StringComparison.OrdinalIgnoreCase));

    /// <summary>Adds the folder to the favorites, or removes it when it is one; true when it was added.</summary>
    public bool ToggleFavorite(string folder)
    {
        if (IsFavorite(folder))
        {
            Favorites.RemoveAll(f => string.Equals(f, folder, StringComparison.OrdinalIgnoreCase));
            return false;
        }

        Favorites.Add(folder);
        return true;
    }
}
