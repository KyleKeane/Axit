using System.Diagnostics;
using Axit.Core;

namespace Axit.Forms;

/// <summary>
/// Installing a program an app needs (docs/axit/SPEC.md AX-2.9, 2026-10-24): from the notice that says the program is
/// missing, the window runs its installer without a window of its own, says what to expect, and checks again when it
/// ends. Started by the user only; one at a time.
/// </summary>
public abstract partial class AxitForm
{
    /// <summary>How long an install may take before the window stops waiting for it and says so.</summary>
    private static readonly TimeSpan InstallTimeout = TimeSpan.FromMinutes(15);

    /// <summary>
    /// The installer running, with its program's name: a second Install it now is said, not started (two winget runs
    /// at once fail each other), and that includes an installer the window stopped waiting for, which is never killed:
    /// it is cleared only when the installer's process ends.
    /// </summary>
    private (string Name, Task Exited)? _installing;

    /// <summary>
    /// Installs <paramref name="program"/>: a progress notice, the installer run, the PATH read again, then
    /// <paramref name="installed"/> asked; true runs <paramref name="then"/> after "Git is installed", false says what
    /// happened with the program's page to open. Without a way to install it here (no winget), the page opens at once.
    /// </summary>
    protected void InstallProgram(ProgramInstall program, Func<bool> installed, Action then)
    {
        if (_installing is { Exited.IsCompleted: false } running)
        {
            Announce($"{running.Name} is being installed already; this window says when it is done", true);
            return;
        }

        if (program.StartInfo() is not { } info)
        {
            OpenUrl(program.Page);
            Announce($"{program.Name} cannot be installed from here: its download page is opening in the browser", true);
            return;
        }

        Do(async () =>
        {
            // Its own notice, kept, so that the end closes this one and never one the user opened after hiding it.
            var progress = new Notice("Installing " + program.Name,
                (program.AsksForAdministrator
                    ? "Windows will ask whether the installer may make changes to this computer: choose Yes. "
                    : "It installs for your account only; Windows asks nothing. ")
                + "This takes a minute or a few; the notice closes by itself and says when it is done. Escape hides this notice; the install goes on.",
                [OverlayChoice.Close])
            {
                Progress = true,
            };
            ShowNotice(progress);
            Log.Info("Installing " + program.Name);
            var (exitCode, lastLine) = await RunInstallerAsync(info, exited => _installing = (program.Name, exited));
            ProgramInstall.RefreshPath();
            if (IsDisposed)
            {
                return;
            }

            var ownShowing = NoticeOpen && ReferenceEquals(Overlay.Shown, progress);
            if (installed())
            {
                Log.Info(program.Name + " installed");
                if (ownShowing)
                {
                    CloseNotice(null);
                }

                Announce(program.Name + " is installed", true);
                then();
                return;
            }

            Log.Info($"{program.Name} not installed: exit {exitCode}");
            var why = (exitCode is null ? "The installer took over a quarter of an hour, and the window stopped waiting for it." : $"The installer ended with code {exitCode}{(lastLine.Length > 0 ? ": " + lastLine : string.Empty)}.")
                + (program.AsksForAdministrator ? " If Windows asked whether to allow it and the answer was No, try again." : string.Empty);
            if (NoticeOpen && !ownShowing)
            {
                // The user is in another notice: said, not shown over it.
                Announce($"{program.Name} was not installed. {why}", true);
                return;
            }

            ShowNotice(new Notice(program.Name + " was not installed", why + " Its download page installs it by hand.",
            [
                new OverlayChoice("&Try again", () => InstallProgram(program, installed, then)),
                new OverlayChoice("&Open the download page", () => OpenUrl(program.Page), StaysOpen: true),
                OverlayChoice.Close,
            ]));
        });
    }

    /// <summary>
    /// The installer run to its end off the UI thread: its exit code (null past the timeout) and the last line it
    /// printed. <paramref name="started"/> gets the task of the process's end, which outlives the timeout. The output is
    /// read under the same deadline, and for a few seconds past the end at most: a helper the installer left running can
    /// hold the pipes open long after it, as ClaudeLauncher.VersionAsync found.
    /// </summary>
    private static async Task<(int? ExitCode, string LastLine)> RunInstallerAsync(ProcessStartInfo info, Action<Task> started)
    {
        Process? process;
        try
        {
            process = Process.Start(info);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            return (-1, ex.Message);
        }

        if (process is null)
        {
            return (-1, "it did not start");
        }

        // The exit code read as the process ends, then the process let go; the task outlives a timeout.
        var exited = process.WaitForExitAsync().ContinueWith(_ =>
        {
            var code = process.ExitCode;
            process.Dispose();
            return code;
        }, TaskScheduler.Default);
        started(exited);
        // The pipes are read from the start, or a full pipe would stop the installer.
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (await Task.WhenAny(exited, Task.Delay(InstallTimeout)) != exited)
        {
            return (null, string.Empty);
        }

        var reads = Task.WhenAll(output, error);
        var all = await Task.WhenAny(reads, Task.Delay(TimeSpan.FromSeconds(5))) == reads ? output.Result + "\n" + error.Result : string.Empty;
        var last = all.ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .LastOrDefault(line => line.Any(char.IsLetter)) ?? string.Empty;
        return (exited.Result, last);
    }
}
