using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace AxGit.Core;

/// <summary>What one git command gave back: the exit code, what it printed and what it complained.</summary>
public sealed record GitResult(int ExitCode, string Output, string Error)
{
    public bool Ok => ExitCode == 0;

    /// <summary>The first line of the complaint, or of the output when git complained in none; "git failed" when there is nothing.</summary>
    public string FirstLine
    {
        get
        {
            foreach (var text in new[] { Error, Output })
            {
                foreach (var line in text.Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (trimmed.Length > 0)
                    {
                        return trimmed;
                    }
                }
            }

            return "git failed";
        }
    }

    /// <summary>Everything git said, the complaint first, for a notice.</summary>
    public string AllText => string.Join("\n", new[] { Error.Trim(), Output.Trim() }.Where(text => text.Length > 0));
}

/// <summary>git is not on the PATH (SPEC.md AG-1.5).</summary>
public sealed class GitNotFoundException(string message, Exception inner) : Exception(message, inner);

/// <summary>
/// Runs the git program (SPEC.md AG-1.5, AG-D2): one process per command, in the repository, with colour and path
/// quoting off and no terminal prompts, both streams read in UTF-8 on background tasks, a timeout that kills the
/// process. The window calls it with the argument lists the parsers give.
/// </summary>
public static class GitRunner
{
    public static readonly TimeSpan Short = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan Long = TimeSpan.FromMinutes(10);

    /// <summary>The program run: git, found on the PATH (AG-1.5); the tests point it at a fake git.cmd of their own.</summary>
    public static string Program { get; set; } = "git";

    /// <summary>The whole argument list of a command: the repository, the fixed configuration, then <paramref name="arguments"/>. Pure, tested.</summary>
    public static IReadOnlyList<string> Command(string? repository, IReadOnlyList<string> arguments)
    {
        var list = new List<string>();
        if (repository is not null)
        {
            list.Add("-C");
            list.Add(repository);
        }

        list.AddRange(["-c", "core.quotepath=false", "-c", "color.ui=never"]);
        list.AddRange(arguments);
        return list;
    }

    public static async Task<GitResult> RunAsync(string? repository, IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken cancellation = default)
    {
        var info = new ProcessStartInfo(Program)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = new UTF8Encoding(false),
            StandardErrorEncoding = new UTF8Encoding(false),
        };
        foreach (var argument in Command(repository, arguments))
        {
            info.ArgumentList.Add(argument);
        }

        // No prompts a terminal would show, English messages, no pager.
        info.Environment["GIT_TERMINAL_PROMPT"] = "0";
        info.Environment["LC_ALL"] = "C.UTF-8";
        info.Environment["GIT_PAGER"] = "cat";

        using var process = new Process { StartInfo = info };
        try
        {
            process.Start();
        }
        catch (Win32Exception ex)
        {
            throw new GitNotFoundException("git was not found on the PATH.", ex);
        }

        process.StandardInput.Close();
        // The reads run under the timeout as well as the wait: a program git started that outlives it (an agent, a
        // hook's daemon) keeps the pipes open after git has exited, and a read with no end would hang the window.
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        limit.CancelAfter(timeout);
        var output = process.StandardOutput.ReadToEndAsync(limit.Token);
        var error = process.StandardError.ReadToEndAsync(limit.Token);
        try
        {
            await process.WaitForExitAsync(limit.Token);
            return new GitResult(process.ExitCode, await output, await error);
        }
        catch (OperationCanceledException)
        {
            // Whether git itself had finished, taken before the kill, which ends it either way (until 2026-10-23 it
            // was read after the kill, so every timeout read as the open-pipe case; the fake git's test found it).
            var finished = process.HasExited;
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
            {
            }

            cancellation.ThrowIfCancellationRequested();
            var reason = finished
                ? "git finished, but a program it started kept its output open; the output read so far is what there is."
                : $"git did not finish within {timeout.TotalSeconds:0} seconds and was stopped.";
            return new GitResult(finished ? process.ExitCode : -1, await ReadSoFar(output), reason);
        }
    }

    /// <summary>What a cancelled read gave: nothing, since the read throws rather than return a part.</summary>
    private static async Task<string> ReadSoFar(Task<string> read)
    {
        try
        {
            return await read;
        }
        catch (OperationCanceledException)
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// The repository that holds <paramref name="path"/> (a folder, or a file whose folder is taken), as the full
    /// path of its root; null when the path is in none (AG-1.2).
    /// </summary>
    public static async Task<string?> RootAsync(string path, CancellationToken cancellation = default)
    {
        var folder = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
        if (folder is null || !Directory.Exists(folder))
        {
            return null;
        }

        var result = await RunAsync(folder, ["rev-parse", "--show-toplevel"], Short, cancellation);
        if (!result.Ok)
        {
            return null;
        }

        var root = result.Output.Trim();
        return root.Length == 0 ? null : Path.GetFullPath(root.Replace('/', Path.DirectorySeparatorChar));
    }
}
