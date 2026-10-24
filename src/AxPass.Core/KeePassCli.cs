using System.Diagnostics;
using System.Text;

namespace AxPass.Core;

/// <summary>What one run of the tool gave: its exit code and what it printed.</summary>
public sealed record CliResult(int ExitCode, string Output, string Error)
{
    public bool Succeeded => ExitCode == 0;

    /// <summary>The tool's own words for a failure ("Could not find entry with path …"), or a plain line when it said nothing (a wrong password, with -q).</summary>
    public string Why => Error.Trim() is { Length: > 0 } error ? error.ReplaceLineEndings(" ") : $"KeePassXC's tool ended with code {ExitCode}";
}

/// <summary>
/// KeePassXC's command-line tool (docs/axpass/SPEC.md PW-5, PW-D1; what it does was tried and is in
/// docs/axpass/kdbx-notes.md section 8): one process per command, no window, the database's and the entry's paths as
/// arguments, the master password (and a new entry password, twice) as lines of standard input, never on the command
/// line, in a file or in the environment. Blocking: the window runs it off the UI thread.
/// </summary>
public static class KeePassCli
{
    public const string ExeName = "keepassxc-cli.exe";

    /// <summary>How long one command may take before it is ended: Argon2 with a large memory setting takes seconds.</summary>
    public const int TimeoutMs = 60_000;

    /// <summary>The tool: the path the user set, KeePassXC's install folder, or the PATH; null when it is nowhere.</summary>
    public static string? Find(string? setPath)
    {
        if (!string.IsNullOrWhiteSpace(setPath) && File.Exists(setPath))
        {
            return setPath;
        }

        foreach (var folder in new[] { Environment.SpecialFolder.ProgramFiles, Environment.SpecialFolder.ProgramFilesX86 })
        {
            var path = Path.Combine(Environment.GetFolderPath(folder), "KeePassXC", ExeName);
            if (File.Exists(path))
            {
                return path;
            }
        }

        foreach (var folder in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var path = Path.Combine(folder.Trim('"'), ExeName);
                if (File.Exists(path))
                {
                    return path;
                }
            }
            catch (ArgumentException)
            {
                // A PATH entry that is no path.
            }
        }

        return null;
    }

    /// <summary>
    /// Runs one command: <paramref name="arguments"/> as they are, <paramref name="input"/> the lines of standard input
    /// (the master password first), each ended by a line feed. A run past <see cref="TimeoutMs"/> is ended and reported.
    /// </summary>
    public static CliResult Run(string cli, IReadOnlyList<string> arguments, IReadOnlyList<string> input)
    {
        var info = new ProcessStartInfo(cli)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            StandardInputEncoding = new UTF8Encoding(false),
        };
        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        using var process = Process.Start(info) ?? throw new IOException("KeePassXC's tool did not start");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        foreach (var line in input)
        {
            process.StandardInput.Write(line);
            process.StandardInput.Write('\n');
        }

        process.StandardInput.Close();
        if (!process.WaitForExit(TimeoutMs))
        {
            process.Kill(entireProcessTree: true);
            return new CliResult(-1, string.Empty, $"KeePassXC's tool took over {TimeoutMs / 1000} seconds and was ended");
        }

        return new CliResult(process.ExitCode, output.Result, error.Result);
    }

    /// <summary>
    /// Starts a command that goes on running (<c>clip</c>, which clears the clipboard itself after its seconds, PW-5.4)
    /// and waits <paramref name="settleMs"/> for it: its result when it ended by then (a failure, most often), null
    /// when it is still running as it should. The process is left to end by itself.
    /// </summary>
    public static CliResult? StartLasting(string cli, IReadOnlyList<string> arguments, IReadOnlyList<string> input, int settleMs)
    {
        var info = new ProcessStartInfo(cli)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = false,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardErrorEncoding = Encoding.UTF8,
            StandardInputEncoding = new UTF8Encoding(false),
        };
        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        var process = Process.Start(info) ?? throw new IOException("KeePassXC's tool did not start");
        var error = process.StandardError.ReadToEndAsync();
        foreach (var line in input)
        {
            process.StandardInput.Write(line);
            process.StandardInput.Write('\n');
        }

        process.StandardInput.Close();
        if (!process.WaitForExit(settleMs))
        {
            process.EnableRaisingEvents = true;
            process.Exited += (_, _) => process.Dispose();
            return null;
        }

        var result = new CliResult(process.ExitCode, string.Empty, error.Result);
        process.Dispose();
        return result;
    }

    /// <summary>The arguments of a command on a database: the command, -q (so that the password prompt goes nowhere), the options, the database, the rest.</summary>
    public static IReadOnlyList<string> On(string command, string database, IEnumerable<string>? options = null, params string[] rest) =>
        [command, "-q", .. options ?? [], database, .. rest];
}
