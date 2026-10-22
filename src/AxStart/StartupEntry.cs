using Axit.Core;
using Microsoft.Win32;

namespace AxStart;

/// <summary>
/// Start with Windows (SPEC.md AS-1.3): the value "Axit AxStart" under the user's Run key, <c>"…\Axit.exe" start
/// --hidden</c>, made or removed when the setting flips and checked at each start. Only the installed program writes
/// it, naming itself: a development build never puts itself, or an older installed copy, into the sign-in.
/// </summary>
public static class StartupEntry
{
    public const string ValueName = "Axit AxStart";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>The value as the setting says: written when on and Axit is installed, removed when off.</summary>
    public static void Apply(bool on)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (on && IsInstalledProgram())
            {
                key.SetValue(ValueName, $"\"{AppPaths.InstalledExe}\" start --hidden");
            }
            else if (!on)
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            Log.Error("The sign-in start could not be changed", ex);
        }
    }

    /// <summary>
    /// Whether this process is the installed program: only then is the value written, since an installed copy older
    /// than AxStart would take "start" for a path and greet every sign-in with an error.
    /// </summary>
    private static bool IsInstalledProgram() =>
        Environment.ProcessPath is { } path && string.Equals(Path.GetFullPath(path), Path.GetFullPath(AppPaths.InstalledExe), StringComparison.OrdinalIgnoreCase);
}
