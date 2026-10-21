using Axit.Core;

namespace AxFile.Core;

/// <summary>
/// Everything AxFile remembers between runs (SPEC.md AF-8.1), in <c>%APPDATA%\Axit\axfile.json</c> through the bundle's
/// <see cref="SettingsFile"/>: the font, the update check and the window's place from <see cref="WindowSettings"/>,
/// plus the last folder, the recent folders, whether hidden entries show, the player's speed and seek step, whether
/// each file is said as it starts, and the user's own programs. A missing or unreadable file falls back to the defaults.
/// </summary>
public sealed class FileSettings : WindowSettings
{
    public const int MaxRecentFolders = 10;

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

    /// <summary>The folders the user pinned with Ctrl+D (AF-3.9), in the order added, for Folder → Favourites.</summary>
    public List<string> Favourites { get; set; } = [];

    /// <summary>The one unprompted announcement: the name of the next file when the player moves on (AF-5.4, G5).</summary>
    public bool SayEachFile { get; set; } = true;

    /// <summary>The playback speed, kept for the next file and the next window (AF-5.4).</summary>
    public double Speed { get; set; } = 1;

    /// <summary>What Ctrl+Left and Ctrl+Right move by.</summary>
    public int SeekSeconds { get; set; } = 15;

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

    /// <summary>Puts the folder at the front of <see cref="RecentFolders"/>, once, keeps the list to <see cref="MaxRecentFolders"/>, and makes it the last folder.</summary>
    public void RememberFolder(string folder)
    {
        LastFolder = folder;
        RecentFolders.RemoveAll(r => string.Equals(r, folder, StringComparison.OrdinalIgnoreCase));
        RecentFolders.Insert(0, folder);
        Trim();
    }

    private void Trim()
    {
        if (RecentFolders.Count > MaxRecentFolders)
        {
            RecentFolders.RemoveRange(MaxRecentFolders, RecentFolders.Count - MaxRecentFolders);
        }
    }

    private void Sanitize()
    {
        SanitizeWindow();
        RecentFolders ??= [];
        RecentFolders.RemoveAll(string.IsNullOrWhiteSpace);
        Trim();
        if (string.IsNullOrWhiteSpace(LastFolder))
        {
            LastFolder = null;
        }

        Speed = MediaFiles.SanitizeSpeed(Speed);
        if (SeekSeconds is < 1 or > 600)
        {
            SeekSeconds = 15;
        }

        Programs ??= [];
        Programs.RemoveAll(program => string.IsNullOrWhiteSpace(program?.Name) || string.IsNullOrWhiteSpace(program?.Path));
        if (!Enum.IsDefined(SortBy))
        {
            SortBy = SortBy.Name;
        }

        Favourites ??= [];
        Favourites.RemoveAll(string.IsNullOrWhiteSpace);
        Favourites = Favourites.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Whether the folder is a favourite (AF-3.9).</summary>
    public bool IsFavourite(string folder) => Favourites.Any(f => string.Equals(f, folder, StringComparison.OrdinalIgnoreCase));

    /// <summary>Adds the folder to the favourites, or removes it when it is one; true when it was added.</summary>
    public bool ToggleFavourite(string folder)
    {
        if (IsFavourite(folder))
        {
            Favourites.RemoveAll(f => string.Equals(f, folder, StringComparison.OrdinalIgnoreCase));
            return false;
        }

        Favourites.Add(folder);
        return true;
    }
}
