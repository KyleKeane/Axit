namespace Axit.Core;

/// <summary>
/// Where the bundle keeps its files (docs/axit/SPEC.md AX-5.1): one folder for the bundle, one file per app. Settings
/// in <c>%APPDATA%\Axit\&lt;app&gt;.json</c>, logs in <c>%LOCALAPPDATA%\Axit\logs</c>, downloaded updates in
/// <c>%LOCALAPPDATA%\Axit\updates</c>. The files are named after the apps in lower case: <c>axclaude.json</c>, <c>axdown.log</c>.
/// </summary>
public static class AppPaths
{
    public static string SettingsFile(string app) =>
        Path.Combine(Home(Environment.SpecialFolder.ApplicationData), "Axit", app + ".json");

    /// <summary>The bundle's one list of recently closed files and folders, for the Axit menu (AX-9.4).</summary>
    public static string RecentlyClosedFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.ApplicationData), "Axit", "recently-closed.json");

    public static string LogsFolder { get; } =
        Path.Combine(Home(Environment.SpecialFolder.LocalApplicationData), "Axit", "logs");

    public static string UpdatesFolder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Axit", "updates");

    /// <summary>Where install.ps1 puts the bundle (AX-2.1), and the program file there, which an update starts again.</summary>
    public static string InstallFolder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Axit");

    public static string InstalledExe => Path.Combine(InstallFolder, "Axit.exe");

    /// <summary>
    /// The folder the settings, the recently closed list and the logs live under: the user's, or <c>AXIT_HOME</c> when
    /// it is set, which a test run sets to a folder of its own so that its windows neither read nor write the user's
    /// files (docs/axit/automation.md).
    /// </summary>
    private static string Home(Environment.SpecialFolder folder) =>
        Environment.GetEnvironmentVariable("AXIT_HOME") is { Length: > 0 } home ? home : Environment.GetFolderPath(folder);
}
