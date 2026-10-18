using System.Text;

namespace AxTerminal.Core;

/// <summary>The shells AxTerminal starts (SPEC.md AT-1.2).</summary>
public enum ShellKind
{
    PowerShell7,
    WindowsPowerShell,
    CommandPrompt,
    Custom,
}

/// <summary>
/// Finds the shells and builds the command line and the environment the window starts one with (SPEC.md AT-1.2,
/// AT-1.3, AT-D2, AT-D7): PowerShell gets a snippet after its profile that wraps the user's prompt in the integration
/// marks (OSC 133 A, B and D with the exit code, OSC 9;9 with the directory, the Windows Terminal convention) and
/// silences the progress bar; Command Prompt gets the same marks through its PROMPT variable; a custom command line
/// gets nothing. The environment keeps the user's and adds TERM and the two pager settings.
/// </summary>
public static class ShellLauncher
{
    /// <summary>
    /// Runs after PowerShell's profile (<c>-NoExit -EncodedCommand</c>, so that no quoting touches it): keeps the
    /// prompt function the profile left and wraps its text in the marks. The exit code follows Windows Terminal's
    /// rule: a new history entry since the last prompt means a command ran; <c>$?</c> true is 0, an error of
    /// PowerShell's own (the newest error belongs to that history entry) is -1, otherwise the last native exit code;
    /// no new entry (a bare Enter, Ctrl+C) gives a D without a code. Written with [char]27 and [char]7, since the
    /// backtick-e escape is PowerShell 7 only.
    /// </summary>
    public const string PromptSnippet = """
        $global:__AxitPrompt = $function:prompt
        $global:__AxitHistoryId = -1
        function global:prompt {
            $ok = $?
            $e = [char]27
            $b = [char]7
            $last = Get-History -Count 1
            $lastId = if ($null -ne $last) { $last.Id } else { 0 }
            $out = ''
            if ($global:__AxitHistoryId -ne -1) {
                $code = $null
                if ($lastId -ne $global:__AxitHistoryId) {
                    if ($ok) { $code = 0 }
                    else {
                        $code = -1
                        try {
                            $own = ($Error.Count -gt 0 -and $null -ne $Error[0].InvocationInfo -and $Error[0].InvocationInfo.HistoryId -eq $lastId)
                            if (-not $own -and $null -ne $global:LASTEXITCODE) { $code = $global:LASTEXITCODE }
                        } catch { $code = -1 }
                    }
                }
                if ($null -ne $code) { $out += "$e]133;D;$code$b" } else { $out += "$e]133;D$b" }
            }
            $global:__AxitHistoryId = $lastId
            $loc = $executionContext.SessionState.Path.CurrentLocation.Path
            $out += "$e]133;A$b$e]9;9;`"$loc`"$b"
            $text = if ($global:__AxitPrompt) { & $global:__AxitPrompt } else { "PS $loc> " }
            $out += "$text$e]133;B$b"
            $out
        }
        $global:ProgressPreference = 'SilentlyContinue'
        """;

    /// <summary>What the menus and the system lines call each shell.</summary>
    public static string Describe(ShellKind kind) => kind switch
    {
        ShellKind.PowerShell7 => "PowerShell 7",
        ShellKind.WindowsPowerShell => "Windows PowerShell",
        ShellKind.CommandPrompt => "Command Prompt",
        _ => "Custom shell",
    };

    /// <summary>The settings file's word for a shell (SPEC.md AT-8.1) and back; an unknown word is the default.</summary>
    public static string Key(ShellKind kind) => kind switch
    {
        ShellKind.PowerShell7 => "pwsh",
        ShellKind.WindowsPowerShell => "powershell",
        ShellKind.CommandPrompt => "cmd",
        _ => "custom",
    };

    public static ShellKind Parse(string? key) => key switch
    {
        "pwsh" => ShellKind.PowerShell7,
        "powershell" => ShellKind.WindowsPowerShell,
        "cmd" => ShellKind.CommandPrompt,
        "custom" => ShellKind.Custom,
        _ => DefaultKind(),
    };

    /// <summary>PowerShell 7 when it is installed, otherwise Windows PowerShell (SPEC.md AT-1.2).</summary>
    public static ShellKind DefaultKind() => Find(ShellKind.PowerShell7) is not null ? ShellKind.PowerShell7 : ShellKind.WindowsPowerShell;

    /// <summary>The program of a shell, or null when it is not installed (Custom has none).</summary>
    public static string? Find(ShellKind kind)
    {
        switch (kind)
        {
            case ShellKind.PowerShell7:
                return FindOnPath("pwsh.exe") ?? Existing(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell", "7", "pwsh.exe"));
            case ShellKind.WindowsPowerShell:
                return Existing(Path.Combine(SystemRoot(), "System32", "WindowsPowerShell", "v1.0", "powershell.exe")) ?? FindOnPath("powershell.exe");
            case ShellKind.CommandPrompt:
                return Existing(Environment.GetEnvironmentVariable("ComSpec")) ?? Existing(Path.Combine(SystemRoot(), "System32", "cmd.exe"));
            default:
                return null;
        }
    }

    /// <summary>
    /// The command line the pseudo console starts: the shell's program with the snippet for PowerShell, the program
    /// alone for Command Prompt (its marks come through the environment), the custom line as typed. Null when the
    /// shell is not installed or the custom line is empty.
    /// </summary>
    public static string? BuildCommandLine(ShellKind kind, string? customCommandLine = null)
    {
        if (kind == ShellKind.Custom)
        {
            return string.IsNullOrWhiteSpace(customCommandLine) ? null : customCommandLine.Trim();
        }

        var program = Find(kind);
        if (program is null)
        {
            return null;
        }

        return kind == ShellKind.CommandPrompt
            ? Quote(program)
            : $"{Quote(program)} -NoLogo -NoExit -EncodedCommand {EncodedSnippet()}";
    }

    /// <summary>The snippet as <c>-EncodedCommand</c> takes it: base64 of its UTF-16 text.</summary>
    public static string EncodedSnippet() => Convert.ToBase64String(Encoding.Unicode.GetBytes(PromptSnippet.ReplaceLineEndings("\n")));

    /// <summary>
    /// The user's environment, plus TERM for the programs that look, GIT_PAGER and GH_PAGER set to cat, which git and
    /// the GitHub CLI document as "no pager" (AT-D7), and for Command Prompt the PROMPT with the marks around the
    /// prompt it had.
    /// </summary>
    public static Dictionary<string, string> BuildEnvironment(ShellKind kind)
    {
        var env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            env[(string)entry.Key] = (string?)entry.Value ?? string.Empty;
        }

        env["TERM"] = "xterm-256color";
        env["GIT_PAGER"] = "cat";
        env["GH_PAGER"] = "cat";
        if (kind == ShellKind.CommandPrompt)
        {
            env.TryGetValue("PROMPT", out var current);
            env["PROMPT"] = CommandPromptWithMarks(current);
        }

        return env;
    }

    /// <summary>
    /// Command Prompt's PROMPT with the marks around the prompt text (<c>$e</c> is ESC, <c>$e\</c> the string
    /// terminator, <c>$P</c> the directory): the D without an exit code, since cmd has none to give, A, the directory,
    /// the prompt as it was (<c>$P$G</c> when none was set), B.
    /// </summary>
    public static string CommandPromptWithMarks(string? current)
    {
        var text = string.IsNullOrWhiteSpace(current) ? "$P$G" : current;
        return "$e]133;D$e\\$e]133;A$e\\$e]9;9;$P$e\\" + text + "$e]133;B$e\\";
    }

    private static string Quote(string path) => path.Contains(' ') ? $"\"{path}\"" : path;

    private static string SystemRoot() => Environment.GetEnvironmentVariable("SystemRoot") ?? @"C:\Windows";

    private static string? Existing(string? path) => path is not null && File.Exists(path) ? path : null;

    private static string? FindOnPath(string file)
    {
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(dir.Trim(), file);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch (ArgumentException)
            {
                // A PATH entry with characters a path cannot hold: skipped.
            }
        }

        return null;
    }
}
