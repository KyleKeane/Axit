using Microsoft.Win32;

namespace AxProcess.Core;

/// <summary>Where a startup entry is set (SPEC.md PR-5.1).</summary>
public enum StartupPlace
{
    UserRunKey,
    MachineRunKey,
    MachineRunKey32,
    UserFolder,
    CommonFolder,
}

/// <summary>One item of the Startup view (PR-5.1): its name, the command or the file it starts, where it is set, and whether Task Manager has turned it off.</summary>
public sealed record StartupEntry(string Name, string Command, StartupPlace Place, bool Enabled)
{
    /// <summary>"OneDrive, enabled, current user's Run key".</summary>
    public string Label => $"{Name}, {(Enabled ? "enabled" : "disabled")}, {PlaceText(Place)}";

    /// <summary>The key a view keeps the selection by: the name and the place.</summary>
    public string Key => $"{Place}:{Name}";

    /// <summary>The lines of the details notice (PR-5.3).</summary>
    public IReadOnlyList<string> Details =>
        CommandLine.ProgramOf(Command) is { } program
            ? [$"Command {Command}", $"Set in {PlaceText(Place)}", Enabled ? "Enabled" : "Disabled", $"Program {program}"]
            : [$"Command {Command}", $"Set in {PlaceText(Place)}", Enabled ? "Enabled" : "Disabled"];

    public static string PlaceText(StartupPlace place) => place switch
    {
        StartupPlace.UserRunKey => "current user's Run key",
        StartupPlace.MachineRunKey => "computer's Run key",
        StartupPlace.MachineRunKey32 => "computer's Run key (32-bit)",
        StartupPlace.UserFolder => "your Startup folder",
        _ => "all users' Startup folder",
    };

    public static string ListName(int count) => count == 0 ? "Startup, none" : $"Startup, {count}";
}

/// <summary>The program file a command starts (PR-5.3): the quoted path, or the text up to and with ".exe", or the whole command when it names a file.</summary>
public static class CommandLine
{
    public static string? ProgramOf(string command)
    {
        var text = Environment.ExpandEnvironmentVariables(command.Trim());
        if (text.Length == 0)
        {
            return null;
        }

        if (text[0] == '"')
        {
            var close = text.IndexOf('"', 1);
            return close > 1 ? text[1..close] : null;
        }

        var exe = text.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        if (exe > 0)
        {
            return text[..(exe + 4)];
        }

        // A Startup folder entry, a shortcut or a file, is the path itself; anything else names no file.
        return Path.IsPathRooted(text) ? text : null;
    }
}

/// <summary>
/// The Startup view (SPEC.md PR-5): the Run keys of the user and of the computer (and its 32-bit twin) and the two
/// Startup folders, each entry enabled unless its StartupApproved value says Task Manager turned it off (PR-5.2).
/// Read-only (PR-D4). <see cref="Read"/> reads the registry and the folders; <see cref="IsEnabled"/> is pure.
/// </summary>
public static class StartupList
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunKey32 = @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run";
    private const string ApprovedRun = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private const string ApprovedRun32 = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run32";
    private const string ApprovedFolder = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder";

    /// <summary>
    /// Whether an entry is on: Task Manager writes 12 bytes under StartupApproved, the first even (02 or 06) when the
    /// entry runs and odd (03 or 01) when it was turned off. No value, or a short one, is on.
    /// </summary>
    public static bool IsEnabled(byte[]? approved) => approved is not { Length: > 0 } || approved[0] % 2 == 0;

    public static IReadOnlyList<StartupEntry> Read()
    {
        var entries = new List<StartupEntry>();
        ReadKey(entries, Registry.CurrentUser, RunKey, ApprovedRun, StartupPlace.UserRunKey);
        ReadKey(entries, Registry.LocalMachine, RunKey, ApprovedRun, StartupPlace.MachineRunKey);
        ReadKey(entries, Registry.LocalMachine, RunKey32, ApprovedRun32, StartupPlace.MachineRunKey32);
        ReadFolder(entries, Environment.GetFolderPath(Environment.SpecialFolder.Startup), Registry.CurrentUser, StartupPlace.UserFolder);
        ReadFolder(entries, Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), Registry.LocalMachine, StartupPlace.CommonFolder);
        return Sorted(entries);
    }

    public static IReadOnlyList<StartupEntry> Sorted(IEnumerable<StartupEntry> entries) =>
        entries.OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase).ThenBy(entry => entry.Place).ToList();

    private static void ReadKey(List<StartupEntry> entries, RegistryKey hive, string path, string approvedPath, StartupPlace place)
    {
        try
        {
            using var key = hive.OpenSubKey(path);
            if (key is null)
            {
                return;
            }

            using var approved = hive.OpenSubKey(approvedPath);
            foreach (var name in key.GetValueNames())
            {
                if (name.Length == 0 || key.GetValue(name) is not string command)
                {
                    continue;
                }

                entries.Add(new StartupEntry(name, command, place, IsEnabled(approved?.GetValue(name) as byte[])));
            }
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            // A key the user may not read is left out.
        }
    }

    private static void ReadFolder(List<StartupEntry> entries, string folder, RegistryKey hive, StartupPlace place)
    {
        if (folder.Length == 0 || !Directory.Exists(folder))
        {
            return;
        }

        try
        {
            using var approved = hive.OpenSubKey(ApprovedFolder);
            foreach (var file in Directory.GetFiles(folder))
            {
                var fileName = Path.GetFileName(file);
                if (fileName.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                entries.Add(new StartupEntry(Path.GetFileNameWithoutExtension(file), file, place, IsEnabled(approved?.GetValue(fileName) as byte[])));
            }
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
        }
    }
}
