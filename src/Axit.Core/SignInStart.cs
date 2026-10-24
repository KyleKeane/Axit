using Microsoft.Win32;

namespace Axit.Core;

/// <summary>
/// Start with Windows for a background app (docs/axstart/SPEC.md AS-1.3; from axStart on its second use, axClip,
/// 2026-10-23): a value under the user's Run key, "Axit axStart" with <c>"…\Axit.exe" start --hidden</c>, made or
/// removed when the app's setting flips and checked at each start. Only the installed program writes it, naming
/// itself: a development build never puts itself, or an older installed copy, into the sign-in.
/// </summary>
public static class SignInStart
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>The value's name for an app: "Axit axStart".</summary>
    public static string ValueName(string appName) => "Axit " + appName;

    /// <summary>The value as the setting says: written when on and Axit is installed, removed when off.</summary>
    public static void Apply(string appName, string verb, bool on)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (on && IsInstalledProgram())
            {
                key.SetValue(ValueName(appName), $"\"{AppPaths.InstalledExe}\" {verb} --hidden");
            }
            else if (!on)
            {
                key.DeleteValue(ValueName(appName), throwOnMissingValue: false);
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            Log.Error("The sign-in start could not be changed", ex);
        }
    }

    /// <summary>
    /// Whether this process is the installed program: only then is the value written, since an installed copy older
    /// than the app would take its verb for a path and greet every sign-in with an error.
    /// </summary>
    private static bool IsInstalledProgram() =>
        Environment.ProcessPath is { } path && string.Equals(Path.GetFullPath(path), Path.GetFullPath(AppPaths.InstalledExe), StringComparison.OrdinalIgnoreCase);
}
