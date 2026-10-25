using System.ComponentModel;
using System.Diagnostics;

namespace Axit.Core;

/// <summary>
/// What a program run to its end gave (<see cref="ProcessRun"/>): its exit code, what it printed on each stream, and
/// whether the time ran out. <see cref="Finished"/> tells, after a timeout, whether the program itself had ended and
/// only a program it started still held its output open (a credential helper, a hook's daemon).
/// </summary>
public sealed record ProcessResult(int ExitCode, string Output, string Error, bool TimedOut, bool Finished);

/// <summary>
/// A program run without a window to its end, under a time limit (inventory item 3, 2026-10-25: git's runner, Claude
/// Code's version and KeePassXC's tool each wrote it): standard input given as lines, each ended by a line feed, then
/// closed; both outputs read to their end, under the limit as well as the wait, so that a full pipe never holds the
/// program and a pipe a child keeps open never hangs the caller; on a timeout the whole process tree is ended. The
/// caller sets the program, its arguments as a list, its environment and its encodings on the start info; this sets
/// the redirections. A program that cannot be started throws as <see cref="Process.Start()"/> does.
/// </summary>
public static class ProcessRun
{
    public static async Task<ProcessResult> RunAsync(ProcessStartInfo start, IReadOnlyList<string> input, TimeSpan timeout, CancellationToken cancellation = default)
    {
        start.UseShellExecute = false;
        start.CreateNoWindow = true;
        start.RedirectStandardInput = true;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        using var process = new Process { StartInfo = start };
        process.Start();

        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        limit.CancelAfter(timeout);
        // The reads start before the input is written: a program that answers as it reads could fill a pipe first.
        var output = process.StandardOutput.ReadToEndAsync(limit.Token);
        var error = process.StandardError.ReadToEndAsync(limit.Token);
        try
        {
            foreach (var line in input)
            {
                await process.StandardInput.WriteAsync(line.AsMemory(), limit.Token).ConfigureAwait(false);
                await process.StandardInput.WriteAsync("\n".AsMemory(), limit.Token).ConfigureAwait(false);
            }

            process.StandardInput.Close();
            await process.WaitForExitAsync(limit.Token).ConfigureAwait(false);
            return new ProcessResult(process.ExitCode, await output.ConfigureAwait(false), await error.ConfigureAwait(false), TimedOut: false, Finished: true);
        }
        catch (OperationCanceledException)
        {
            // Whether the program itself had finished, taken before the kill, which ends it either way.
            var finished = process.HasExited;
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception kill) when (kill is InvalidOperationException or Win32Exception)
            {
                // Gone already.
            }

            cancellation.ThrowIfCancellationRequested();
            return new ProcessResult(finished ? process.ExitCode : -1, await SoFar(output).ConfigureAwait(false), await SoFar(error).ConfigureAwait(false), TimedOut: true, Finished: finished);
        }
    }

    /// <summary>The same, waited for on the calling thread, for code that runs on a thread of its own already (axPass's tool, under <c>Task.Run</c>).</summary>
    public static ProcessResult Run(ProcessStartInfo start, IReadOnlyList<string> input, TimeSpan timeout) =>
        RunAsync(start, input, timeout).GetAwaiter().GetResult();

    /// <summary>What a read gave, or nothing when it was cancelled: a cancelled read throws rather than return a part.</summary>
    private static async Task<string> SoFar(Task<string> read)
    {
        try
        {
            return await read.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return string.Empty;
        }
    }
}
