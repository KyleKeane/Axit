using Axit.Core;

namespace AxPass.Core;

/// <summary>
/// Everything axPass remembers between runs (SPEC.md PW-4), in <c>%APPDATA%\Axit\axpass.json</c> through the bundle's
/// <see cref="SettingsFile"/>: the font, the update check and the window's place from <see cref="WindowSettings"/>, the
/// database open last, where KeePassXC's tool is when it is not where it installs, and the minutes before the window
/// locks itself. Never the master password nor anything from the database.
/// </summary>
public sealed class PassSettings : WindowSettings
{
    public const int DefaultLockMinutes = 10;

    public string? LastDatabase { get; set; }

    /// <summary>The tool's path when the user set one; null to look for it (KeePassCli.Find).</summary>
    public string? CliPath { get; set; }

    /// <summary>Minutes without a key or a mouse move before the window locks (PW-4.3); 0 never.</summary>
    public int LockMinutes { get; set; } = DefaultLockMinutes;

    public static string DefaultPath => AppPaths.SettingsFile("axpass");

    public static PassSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<PassSettings>(path, out error);
        settings.Sanitize();
        return settings;
    }

    public override void Save(string path) => SettingsFile.Save(path, this);

    private void Sanitize()
    {
        SanitizeWindow();
        if (string.IsNullOrWhiteSpace(LastDatabase))
        {
            LastDatabase = null;
        }

        if (string.IsNullOrWhiteSpace(CliPath))
        {
            CliPath = null;
        }

        if (LockMinutes is < 0 or > 24 * 60)
        {
            LockMinutes = DefaultLockMinutes;
        }
    }
}
