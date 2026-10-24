using System.Diagnostics;

namespace Axit.Core;

/// <summary>
/// A program an app runs and Windows does not have (docs/axit/SPEC.md AX-2.9, the user's ask of 2026-10-24): Git for
/// axGIT, KeePassXC for axPass, Claude Code for axClaude. Its name, how it installs (winget's package id, or its own
/// installer's PowerShell command), and its download page for when neither can run. The install is only ever started
/// by the user, from the notice that says the program is missing (bundle B-15: no network call of a window's own).
/// </summary>
public sealed record ProgramInstall(string Name, string? WingetId, string? PowerShellCommand, string Page, bool AsksForAdministrator)
{
    public static readonly ProgramInstall Git = new("Git", "Git.Git", null, "https://git-scm.com/downloads/win", AsksForAdministrator: true);

    /// <summary>PowerShell 7, for axTerminal (Windows has Windows PowerShell 5.1 only); Microsoft's own package.</summary>
    public static readonly ProgramInstall PowerShell7 = new("PowerShell 7", "Microsoft.PowerShell", null, "https://learn.microsoft.com/powershell/scripting/install/installing-powershell-on-windows", AsksForAdministrator: true);

    public static readonly ProgramInstall KeePassXC = new("KeePassXC", "KeePassXCTeam.KeePassXC", null, "https://keepassxc.org/download/#windows", AsksForAdministrator: true);

    /// <summary>Claude Code's own installer, for this user only, as its setup page gives it.</summary>
    public static readonly ProgramInstall ClaudeCode = new("Claude Code", null, "irm https://claude.ai/install.ps1 | iex", "https://code.claude.com/docs/en/setup", AsksForAdministrator: false);

    /// <summary>
    /// winget, Windows' package manager (the App Installer), where Windows puts it for the user; null on a Windows
    /// without it. Never a winget.exe found on the PATH, which any folder there could hold (security audit S-10).
    /// </summary>
    public static string? Winget()
    {
        var apps = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps", "winget.exe");
        return File.Exists(apps) ? apps : null;
    }

    /// <summary>Whether this program can be installed from here: its own installer, or winget being there.</summary>
    public bool CanInstall => PowerShellCommand is not null || (WingetId is not null && Winget() is not null);

    /// <summary>The process that installs it, without a window; null when it cannot be installed from here.</summary>
    public ProcessStartInfo? StartInfo()
    {
        ProcessStartInfo info;
        if (PowerShellCommand is { } command)
        {
            info = new ProcessStartInfo(SystemPrograms.PowerShell);
            foreach (var argument in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", command })
            {
                info.ArgumentList.Add(argument);
            }
        }
        else if (WingetId is { } id && Winget() is { } winget)
        {
            info = new ProcessStartInfo(winget);
            foreach (var argument in WingetArguments(id))
            {
                info.ArgumentList.Add(argument);
            }
        }
        else
        {
            return null;
        }

        info.UseShellExecute = false;
        info.CreateNoWindow = true;
        info.RedirectStandardOutput = true;
        info.RedirectStandardError = true;
        return info;
    }

    /// <summary>winget's arguments: the package by its exact id from winget's own source (S-10: never another source the user added), every question answered, the installer silent.</summary>
    public static IReadOnlyList<string> WingetArguments(string id) =>
        ["install", "--id", id, "--exact", "--source", "winget", "--silent", "--accept-package-agreements", "--accept-source-agreements", "--disable-interactivity"];

    /// <summary>
    /// The process's PATH made again from Windows' own (the machine's, then the user's), so that a program installed
    /// while the window runs is found without starting it again; the other variables stay as they are.
    /// </summary>
    public static void RefreshPath() =>
        Environment.SetEnvironmentVariable("PATH", CombinePath(
            Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine),
            Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User),
            Environment.GetEnvironmentVariable("PATH")));

    /// <summary>The machine's PATH, the user's, then any of the process's own entries neither holds, each once.</summary>
    public static string CombinePath(string? machine, string? user, string? current)
    {
        var entries = new List<string>();
        foreach (var part in $"{machine};{user};{current}".Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var expanded = Environment.ExpandEnvironmentVariables(part);
            if (!entries.Contains(expanded, StringComparer.OrdinalIgnoreCase))
            {
                entries.Add(expanded);
            }
        }

        return string.Join(';', entries);
    }
}
