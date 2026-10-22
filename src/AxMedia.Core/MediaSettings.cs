using Axit.Core;

namespace AxMedia.Core;

/// <summary>
/// Everything AxMedia remembers between runs (SPEC.md AM-7.1), in <c>%APPDATA%\Axit\axmedia.json</c> through the
/// bundle's <see cref="SettingsFile"/>: the font, the update check and the window's place from
/// <see cref="WindowSettings"/>, plus the speed, the seek step, whether each file is said as it starts, and the
/// files and folders added by command. A missing or unreadable file falls back to the defaults.
/// </summary>
public sealed class MediaSettings : WindowSettings
{
    public const int MaxRecent = 10;

    /// <summary>The one unprompted announcement: the name of the next file when the player moves on (AM-4.3, G5).</summary>
    public bool SayEachFile { get; set; } = true;

    /// <summary>The playback speed, kept for the next file and the next window (AM-4.3).</summary>
    public double Speed { get; set; } = 1;

    /// <summary>What Ctrl+Left and Ctrl+Right move by.</summary>
    public int SeekSeconds { get; set; } = 15;

    /// <summary>The files and folders added by command (Open files, Open folder), newest first, for Queue → Open recent (AM-3.2).</summary>
    public List<string> Recent { get; set; } = [];

    public static string DefaultPath => AppPaths.SettingsFile("axmedia");

    public static MediaSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<MediaSettings>(path, out error);
        settings.Sanitize();
        return settings;
    }

    public override void Save(string path) => SettingsFile.Save(path, this);

    /// <summary>Puts the path at the front of <see cref="Recent"/>, once, and keeps the list to <see cref="MaxRecent"/>.</summary>
    public void Remember(string path)
    {
        Recent.RemoveAll(r => string.Equals(r, path, StringComparison.OrdinalIgnoreCase));
        Recent.Insert(0, path);
        Trim();
    }

    private void Trim()
    {
        if (Recent.Count > MaxRecent)
        {
            Recent.RemoveRange(MaxRecent, Recent.Count - MaxRecent);
        }
    }

    private void Sanitize()
    {
        SanitizeWindow();
        Speed = MediaFiles.SanitizeSpeed(Speed);
        if (SeekSeconds is < 1 or > 600)
        {
            SeekSeconds = 15;
        }

        Recent ??= [];
        Recent.RemoveAll(string.IsNullOrWhiteSpace);
        Trim();
    }
}
