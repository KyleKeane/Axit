using System.Diagnostics;
using Axit.Core;

namespace Axit.Forms;

/// <summary>
/// Installing a program an app needs (docs/axit/SPEC.md AX-2.9, 2026-10-24): from the notice that says the program is
/// missing, the window runs its installer without a window of its own, says what to expect, and checks again when it
/// ends. Started by the user only.
/// </summary>
public abstract partial class AxitForm
{
    /// <summary>How long an install may take before the window stops waiting for it and says so.</summary>
    private static readonly TimeSpan InstallTimeout = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Installs <paramref name="program"/>: a progress notice, the installer run, the PATH read again, then
    /// <paramref name="installed"/> asked; true runs <paramref name="then"/> after "Git is installed", false says what
    /// happened with the program's page to open. Without a way to install it here (no winget), the page opens at once.
    /// </summary>
    protected void InstallProgram(ProgramInstall program, Func<bool> installed, Action then)
    {
        if (program.StartInfo() is not { } info)
        {
            OpenUrl(program.Page);
            Announce($"{program.Name} cannot be installed from here: its download page is opening in the browser", true);
            return;
        }

        Do(async () =>
        {
            ShowNotice(new Notice("Installing " + program.Name,
                (program.AsksForAdministrator
                    ? "Windows will ask whether the installer may make changes to this computer: choose Yes. "
                    : "It installs for your account only; Windows asks nothing. ")
                + "This takes a minute or a few; the notice closes by itself and says when it is done. Escape hides this notice; the install goes on.",
                [OverlayChoice.Close])
            {
                Progress = true,
            });
            Log.Info("Installing " + program.Name);
            var (exitCode, lastLine) = await RunInstallerAsync(info);
            ProgramInstall.RefreshPath();
            if (IsDisposed)
            {
                return;
            }

            if (installed())
            {
                Log.Info(program.Name + " installed");
                if (NoticeOpen)
                {
                    CloseNotice(null);
                }

                Announce(program.Name + " is installed", true);
                then();
                return;
            }

            Log.Info($"{program.Name} not installed: exit {exitCode}");
            ShowNotice(new Notice(program.Name + " was not installed",
                (exitCode is null ? "The installer took over a quarter of an hour, and the window stopped waiting for it." : $"The installer ended with code {exitCode}{(lastLine.Length > 0 ? ": " + lastLine : string.Empty)}.")
                + (program.AsksForAdministrator ? " If Windows asked whether to allow it and the answer was No, try again." : string.Empty)
                + " Its download page installs it by hand.",
            [
                new OverlayChoice("&Try again", () => InstallProgram(program, installed, then)),
                new OverlayChoice("&Open the download page", () => OpenUrl(program.Page), StaysOpen: true),
                OverlayChoice.Close,
            ]));
        });
    }

    /// <summary>The installer run to its end off the UI thread: its exit code (null past the timeout) and the last line it printed.</summary>
    private static async Task<(int? ExitCode, string LastLine)> RunInstallerAsync(ProcessStartInfo info)
    {
        try
        {
            using var process = Process.Start(info);
            if (process is null)
            {
                return (-1, "it did not start");
            }

            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(InstallTimeout);
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                return (null, string.Empty);
            }

            var all = (await output) + "\n" + (await error);
            var last = all.ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .LastOrDefault(line => line.Any(char.IsLetter)) ?? string.Empty;
            return (process.ExitCode, last);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            return (-1, ex.Message);
        }
    }
}
