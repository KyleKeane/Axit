namespace Axit.Core;

/// <summary>
/// Where the bundle keeps its files (docs/axit/SPEC.md AX-5.1): one folder for the bundle, one file per app. Settings
/// in <c>%APPDATA%\Axit\&lt;app&gt;.json</c>, logs in <c>%LOCALAPPDATA%\Axit\logs</c>, downloaded updates in
/// <c>%LOCALAPPDATA%\Axit\updates</c>. The app names are the verbs: <c>axclaude</c>, <c>axdown</c>.
/// </summary>
public static class AppPaths
{
    public static string SettingsFile(string app) =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Axit", app + ".json");

    public static string LogsFolder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Axit", "logs");

    public static string UpdatesFolder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Axit", "updates");

    /// <summary>Where install.ps1 puts the bundle (AX-2.1), and the program file there, which an update starts again.</summary>
    public static string InstallFolder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Axit");

    public static string InstalledExe => Path.Combine(InstallFolder, "Axit.exe");
}
