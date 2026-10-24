using System.Diagnostics;

namespace Axit.Core;

/// <summary>
/// The two variables a test run gives the windows it starts (docs/axit/automation.md; security audit S-11):
/// <c>AXIT_AUTOMATION</c>, the run's secret, which opens the window's automation channel to the runner alone, and
/// <c>AXIT_HOME</c>, the run's folder for the settings and logs. They are read once, as the process starts, and taken
/// out of its environment at once, so that no program the window starts inherits them (a shell, Claude, git, the user's
/// own programs): only a new Axit window of the same run gets them, from <see cref="PassTo"/>. A secret shorter than 32
/// characters opens nothing, and the folder counts only with a secret.
/// </summary>
public static class AutomationEnvironment
{
    public const string SecretVariable = "AXIT_AUTOMATION";
    public const string HomeVariable = "AXIT_HOME";

    /// <summary>The shortest secret the channel accepts: 128 bits as hexadecimal.</summary>
    public const int MinimumSecretLength = 32;

    private static bool _captured;
    private static string? _secret;
    private static string? _home;

    /// <summary>The run's secret; null outside a test run.</summary>
    public static string? Secret
    {
        get
        {
            Capture();
            return _secret;
        }
    }

    /// <summary>The run's folder for settings and logs; null outside a test run.</summary>
    public static string? Home
    {
        get
        {
            Capture();
            return _home;
        }
    }

    /// <summary>Whether this process is a window of a test run.</summary>
    public static bool Requested => Secret is not null;

    /// <summary>Reads the two variables and takes them out of the process's environment; the first call does it, the program's <c>Main</c> makes that call before anything else.</summary>
    public static void Capture()
    {
        if (_captured)
        {
            return;
        }

        _captured = true;
        var secret = Environment.GetEnvironmentVariable(SecretVariable);
        var home = Environment.GetEnvironmentVariable(HomeVariable);
        Environment.SetEnvironmentVariable(SecretVariable, null);
        Environment.SetEnvironmentVariable(HomeVariable, null);
        _secret = secret is { Length: >= MinimumSecretLength } ? secret : null;
        _home = _secret is not null && home is { Length: > 0 } ? home : null;
    }

    /// <summary>A new Axit window of the same run gets the run's variables; outside a run, nothing.</summary>
    public static void PassTo(ProcessStartInfo info)
    {
        if (Secret is { } secret)
        {
            info.Environment[SecretVariable] = secret;
            if (Home is { } home)
            {
                info.Environment[HomeVariable] = home;
            }
        }
    }
}
