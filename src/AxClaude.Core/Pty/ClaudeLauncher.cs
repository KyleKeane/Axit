using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Axit.Core;

namespace AxClaude.Core.Pty;

/// <summary>Finds the Claude Code executable and builds the command line and environment for it.</summary>
public static class ClaudeLauncher
{
    /// <summary>The native Windows installer, as documented at <see cref="InstallUrl"/>; one source with the Install Claude Code now button (<c>ProgramInstall.ClaudeCode</c>).</summary>
    public static string InstallCommand => ProgramInstall.ClaudeCode.PowerShellCommand!;

    public static string InstallUrl => ProgramInstall.ClaudeCode.Page;

    /// <summary>
    /// The places searched for Claude Code, in order (FR-1.2): the explicit path when one is given, otherwise
    /// <c>claude.exe</c> or <c>claude.cmd</c> in every PATH folder, the native installer's folder and the npm folder.
    /// </summary>
    public static IReadOnlyList<string> Candidates(string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            return [explicitPath];
        }

        var candidates = new List<string>();
        // Only the PATH's full folders: a relative entry would be the current folder (S-1).
        foreach (var directory in SystemPrograms.PathFolders())
        {
            foreach (var name in new[] { "claude.exe", "claude.cmd" })
            {
                candidates.Add(Path.Combine(directory, name));
            }
        }

        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        candidates.Add(Path.Combine(profile, ".local", "bin", "claude.exe"));
        candidates.Add(Path.Combine(appData, "npm", "claude.cmd"));
        return candidates;
    }

    public static string? Find(string? explicitPath)
    {
        foreach (var candidate in Candidates(explicitPath))
        {
            try
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch (ArgumentException)
            {
                // A PATH entry with characters that are not allowed in a path.
            }
        }

        return null;
    }

    public static string BuildCommandLine(string claudePath, IEnumerable<string> arguments)
    {
        var args = string.Join(' ', arguments.Select(Quote));
        return claudePath.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase)
            ? $"\"{SystemPrograms.Cmd}\" /d /s /c \"\"{claudePath}\" {args}\""
            : $"\"{claudePath}\" {args}";
    }

    /// <summary>
    /// What <c>claude --version</c> prints, its first line (<c>2.0.30 (Claude Code)</c>), or null when Claude cannot
    /// be started or has not answered within <paramref name="timeout"/>, in which case it is ended with whatever it
    /// started. Asked once per run of the app, logged and named by Copy diagnostics (FR-13.2, FR-13.3), so that a
    /// transcript bug can be matched to the Claude Code that drew the screens (docs/axit/longevity.md 3.10). A
    /// <c>.cmd</c> runs through cmd.exe, as the session does (<see cref="BuildCommandLine"/>).
    /// </summary>
    public static async Task<string?> VersionAsync(string claudePath, TimeSpan timeout)
    {
        var script = claudePath.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase);
        var start = new ProcessStartInfo
        {
            FileName = script ? SystemPrograms.Cmd : claudePath,
            Arguments = script ? $"/d /s /c \"\"{claudePath}\" --version\"" : "--version",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        try
        {
            using var process = Process.Start(start);
            if (process is null)
            {
                return null;
            }

            process.StandardInput.Close();
            // Both pipes are read to their end, so that a full one cannot hold Claude; a program Claude started could
            // keep them open after it, which the timeout covers as it covers a hang.
            var both = Task.WhenAll(process.StandardOutput.ReadToEndAsync(), process.StandardError.ReadToEndAsync());
            if (await Task.WhenAny(both, Task.Delay(timeout)).ConfigureAwait(false) != both)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
                {
                    // Gone already.
                }

                return null;
            }

            var output = await both.ConfigureAwait(false);
            var first = output[0].Split('\n')[0].Trim();
            return first.Length > 0 ? first : null;
        }
        catch (Exception ex) when (ex is Win32Exception or IOException or InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>
    /// The current environment without any CLAUDE* variable (so a nested session is not detected), plus the
    /// variables that make screen reader mode immediate.
    /// </summary>
    public static Dictionary<string, string> BuildEnvironment()
    {
        var env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            var key = (string)entry.Key;
            if (key.StartsWith("CLAUDE", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            env[key] = (string?)entry.Value ?? string.Empty;
        }

        env["TERM"] = "xterm-256color";
        env["CLAUDE_AX_STARTUP_QUIET_MS"] = "0";
        env["CLAUDE_AX_PREPARK_MS"] = "0";
        env["CLAUDE_CODE_DISABLE_ALTERNATE_SCREEN"] = "1";
        return env;
    }

    /// <summary>
    /// The arguments in a line the user typed (the New session notice, FR-8.6), split the way Windows splits a command
    /// line (CommandLineToArgvW): at whitespace; a double-quoted part keeps its spaces; a backslash is literal except in
    /// front of a quote, where each pair is one backslash and an odd one left over makes the quote literal. So
    /// <c>"C:\Dir With Space\\"</c> is that folder with its trailing backslash. The inverse of <see cref="JoinArguments"/>.
    /// </summary>
    public static IReadOnlyList<string> SplitArguments(string text)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        var hasToken = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\\')
            {
                var run = 1;
                while (i + run < text.Length && text[i + run] == '\\')
                {
                    run++;
                }

                if (i + run < text.Length && text[i + run] == '"')
                {
                    current.Append('\\', run / 2);
                    if (run % 2 == 1)
                    {
                        current.Append('"');
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    i += run;
                }
                else
                {
                    current.Append('\\', run);
                    i += run - 1;
                }

                hasToken = true;
            }
            else if (c == '"')
            {
                // "" is an empty argument; a quoted part joins what is around it: a"b c"d is one argument.
                inQuotes = !inQuotes;
                hasToken = true;
            }
            else if (!inQuotes && char.IsWhiteSpace(c))
            {
                if (hasToken)
                {
                    result.Add(current.ToString());
                    current.Clear();
                    hasToken = false;
                }
            }
            else
            {
                current.Append(c);
                hasToken = true;
            }
        }

        if (hasToken)
        {
            result.Add(current.ToString());
        }

        return result;
    }

    /// <summary>The arguments as one line, quoted where needed, so that <see cref="SplitArguments"/> and Windows give them back.</summary>
    public static string JoinArguments(IEnumerable<string> arguments) => string.Join(' ', arguments.Select(Quote));

    /// <summary>
    /// The argument as the child's CommandLineToArgvW reads it back: quoted when it has a space, a tab or a quote;
    /// inside, backslashes are literal except in front of a quote, the closing one included, where the run is
    /// doubled, and a quote gets one more backslash. An argument ending in a backslash used to swallow the closing
    /// quote and every argument after it.
    /// </summary>
    private static string Quote(string argument)
    {
        if (argument.Length > 0 && argument.IndexOfAny([' ', '\t', '"']) < 0)
        {
            return argument;
        }

        var quoted = new StringBuilder("\"");
        var backslashes = 0;
        foreach (var c in argument)
        {
            if (c == '\\')
            {
                backslashes++;
                continue;
            }

            quoted.Append('\\', c == '"' ? backslashes * 2 + 1 : backslashes).Append(c);
            backslashes = 0;
        }

        return quoted.Append('\\', backslashes * 2).Append('"').ToString();
    }
}
