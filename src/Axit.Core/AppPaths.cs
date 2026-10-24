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

    /// <summary>The AI tools' access (docs/axit/mcp.md MC-6.1): on or off, and the clients the user paired.</summary>
    public static string McpFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.ApplicationData), "Axit", "mcp.json");

    /// <summary>The key the broker proves itself with to the windows (MC-6.3), kept with DPAPI; never roamed, so under the local folder.</summary>
    public static string McpKeyFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.LocalApplicationData), "Axit", "mcp.key");

    /// <summary>Every call of an AI tool, one line each (MC-6.6).</summary>
    public static string McpAuditFile { get; } =
        Path.Combine(Home(Environment.SpecialFolder.LocalApplicationData), "Axit", "mcp-audit.log");

    public static string LogsFolder { get; } =
        Path.Combine(Home(Environment.SpecialFolder.LocalApplicationData), "Axit", "logs");

    public static string UpdatesFolder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Axit", "updates");

    /// <summary>Where install.ps1 puts the bundle (AX-2.1), and the program file there, which an update starts again.</summary>
    public static string InstallFolder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Axit");

    public static string InstalledExe => Path.Combine(InstallFolder, "Axit.exe");

    /// <summary>
    /// The folder the settings, the recently closed list and the logs live under: the user's, or a test run's own
    /// (<see cref="AutomationEnvironment.Home"/>), so that its windows neither read nor write the user's files
    /// (docs/axit/automation.md); a run's folder counts only with the run's secret (security audit S-11).
    /// </summary>
    private static string Home(Environment.SpecialFolder folder) =>
        AutomationEnvironment.Home ?? Environment.GetFolderPath(folder);
}
