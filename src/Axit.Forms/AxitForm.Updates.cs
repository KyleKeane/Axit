using System.Net.Http;
using Axit.Core;
using Axit.Core.Updates;

namespace Axit.Forms;

public abstract partial class AxitForm
{
    // ---- updates (AX-3): the check, the offer in a notice and in the Axit menu, the install in place, the restart ----

    private readonly ToolStripMenuItem _checkItem = new($"Check for &updates (Axit {BundleInfo.Version})...");
    private readonly ToolStripMenuItem _updateItem = new() { Visible = false };
    private ReleaseInfo? _update;
    private bool _updateBusy;

    /// <summary>After Update and restart: how the installed new version is started when this window closes; null when nothing is to start.</summary>
    private string[]? _restartArguments;

    /// <summary>What the last check found, for About, Version: "not checked yet", "checking...", "Axit 3.5.0 (newer than this one)"; the Axit menu, in this project, reads it.</summary>
    protected internal string LatestReleaseState { get; private set; } = "not checked yet";

    /// <summary>Update and restart was chosen and the update is installed: the new version starts when the window closes.</summary>
    protected bool RestartPending => _restartArguments is not null;

    /// <summary>The update is installed and the window is closing for it (AX-3.2): a window that otherwise hides on closing (axStart) closes.</summary>
    protected bool ClosingForUpdate { get; private set; }

    /// <summary>
    /// AX-3: asks GitHub for the latest release, every time. The state for About, Version is set first; then one
    /// thing is shown: a newer release in a notice (unprompted at startup, or said as "in the Axit menu" while a notice
    /// is up), the newest version or a failure when the check was manual (Axit → Check for updates), nothing otherwise.
    /// </summary>
    protected async void CheckForUpdates(bool manual)
    {
        if (_updateBusy)
        {
            return;
        }

        SetUpdateBusy(true);
        LatestReleaseState = "checking...";
        ReleaseInfo? release;
        try
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            release = await Updater.CheckAsync(cancellation.Token);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException or InvalidOperationException)
        {
            Log.Error("The update check failed", ex);
            LatestReleaseState = "unknown, GitHub could not be reached";
            SetUpdateBusy(false);
            if (manual && !IsDisposed)
            {
                ShowNotice(new Notice("Check for updates",
                    $"GitHub could not be reached: {ex.Message}\nThe releases page: {UpdateCheck.ReleasesPage}",
                [
                    new OverlayChoice("&Open releases page", () => OpenUrl(UpdateCheck.ReleasesPage), StaysOpen: true),
                    OverlayChoice.Close,
                ]));
            }

            return;
        }

        SetUpdateBusy(false);
        if (IsDisposed)
        {
            return;
        }

        // A check answers for itself: an update known from an earlier one is replaced, or gone.
        _update = null;
        _updateItem.Visible = false;
        var newer = release is not null && UpdateCheck.IsNewer(release, BundleInfo.Version);
        var version = release?.Version.ToString(3);
        LatestReleaseState = release is null ? "none published yet"
            : newer ? $"Axit {version} (newer than this one)"
            : release.Version == UpdateCheck.ParseVersion(BundleInfo.Version) ? $"Axit {version} (this version)"
            : $"Axit {version} (older than this build)";
        if (newer)
        {
            _update = release;
            _updateItem.Text = $"Update &to Axit {version}...";
            _updateItem.Visible = true;
            Log.Info($"Update available: {release!.Tag}, zip {release.ZipUrl ?? "missing"}");
            if (!manual)
            {
                UpdateNoted($"Axit {version} is available. Axit menu, Update to Axit {version}.");
            }
        }

        if (newer && (manual || !NoticeOpen))
        {
            // At startup the notice opens by itself: Enter updates, Escape keeps this version.
            ShowUpdateNotice(release!, unprompted: !manual);
        }
        else if (newer)
        {
            Announce($"Update {version} in the Axit menu", false);
        }
        else if (manual)
        {
            ShowNotice(Notice.Plain("Check for updates",
                $"You have the newest version, Axit {BundleInfo.Version}.\n" +
                (release is null ? "No release is published yet.\n" : string.Empty) +
                $"Releases: {UpdateCheck.ReleasesPage}"));
        }
    }

    /// <summary>Axit → Update to Axit n: the notice for the release the last check found.</summary>
    private void OfferKnownUpdate()
    {
        if (_update is { } known)
        {
            ShowUpdateNotice(known);
        }
    }

    /// <summary>While a check or an install runs, the two update items of the Axit menu are disabled, so that neither starts twice.</summary>
    private void SetUpdateBusy(bool busy)
    {
        _updateBusy = busy;
        if (!IsDisposed)
        {
            _checkItem.Enabled = !busy;
            _updateItem.Enabled = !busy;
        }
    }

    /// <summary>The button that opens a release on GitHub and leaves the notice open.</summary>
    private OverlayChoice ReleasePageChoice(ReleaseInfo release) => new("&Open release page", () => OpenUrl(release.PageUrl), StaysOpen: true);

    /// <summary>The acceptance of the release's terms kept (AX-11.7), then the install.</summary>
    private void AcceptAndInstall(ReleaseInfo release, bool restart)
    {
        Consent.Record(AppPaths.ConsentFile, Consent.UpdateApp, $"Axit {release.Version.ToString(3)}", BundleInfo.Version, DateTime.Now);
        InstallUpdate(release, restart);
    }

    /// <summary>The release notes with I accept all terms, install and restart (or close), Open release page and Later; <paramref name="unprompted"/> when the check at startup found it.</summary>
    private void ShowUpdateNotice(ReleaseInfo release, bool unprompted = false)
    {
        var version = release.Version.ToString(3);
        var size = release.ZipSize > 0 ? $" The download is {release.ZipSize / (1024.0 * 1024.0):0} MB." : string.Empty;
        // AX-11.7: installing means accepting the release's terms, said first and in the buttons' own words.
        var text =
            $"You have Axit {BundleInfo.Version}. Axit {version} is available.\n" +
            $"Installing this update means you accept the terms of Axit {version}: its licence and NOTICE.md (who is responsible for what, the access layers and the disclaimer), linked in the release notes below and on the release page; About, Licence shows them once it is installed.\n\n" +
            (release.Notes.Length > 0 ? release.Notes + "\n\n" : string.Empty) +
            $"I accept all terms, install and restart downloads the new version, {UpdateRestartText}. I accept all terms, install and close installs without starting it again. " +
            $"A progress bar shows the download.{size}\n" +
            "Later keeps this version; the Axit menu offers the update again.";
        ShowNotice(new Notice($"Update to Axit {version}", text,
        [
            new OverlayChoice("I &accept all terms, install and restart", () => AcceptAndInstall(release, restart: true), IsDefault: true),
            new OverlayChoice("I accept all terms, install and &close", () => AcceptAndInstall(release, restart: false)),
            ReleasePageChoice(release),
            new OverlayChoice("&Later", IsCancel: true),
        ])
        {
            Unprompted = unprompted,
        });
    }

    /// <summary>
    /// Update and restart, or Update and close: downloads the release and installs it in place behind one notice with
    /// one progress bar (the download up to 80 percent, the installer's steps to 100; Cancel stops the download, not
    /// the install), then closes the window; on the way out, OnFormClosing starts the installed new version when
    /// <paramref name="restart"/> was chosen (AX-3.2).
    /// </summary>
    private async void InstallUpdate(ReleaseInfo release, bool restart)
    {
        if (_updateBusy)
        {
            return;
        }

        SetUpdateBusy(true);
        var version = release.Version.ToString(3);
        var size = release.ZipSize > 0 ? $" ({release.ZipSize / (1024.0 * 1024.0):0} MB)" : string.Empty;
        UpdateNoted($"Updating to Axit {version}...");
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var cancelled = false;
        var installing = false;
        ShowNotice(new Notice($"Updating to Axit {version}",
            $"The new version is downloaded{size} and installed; the progress bar shows how far it is. Cancel during the download keeps this version.",
        [
            new OverlayChoice("&Cancel", () =>
            {
                if (installing)
                {
                    Announce("Installing, this cannot be cancelled now", true);
                    return;
                }

                cancelled = true;
                cancellation.Cancel();
                CloseNotice(null);
            }, IsDefault: true, IsCancel: true, StaysOpen: true),
        ])
        {
            Progress = true,
        });
        try
        {
            var folder = await Updater.DownloadAsync(release, cancellation.Token, new Progress<int>(percent => Overlay.SetProgress(percent * 80 / 100)));
            // Cancel pressed while the zip was being unpacked, which the download does not watch the token for.
            cancellation.Token.ThrowIfCancellationRequested();
            if (IsDisposed)
            {
                return;
            }

            installing = true;
            Overlay.SetProgress(80);
            Announce("Downloaded. Installing", true);
            var step = 80;
            var result = await Updater.InstallInPlaceAsync(folder, new Progress<string>(_ => Overlay.SetProgress(step = Math.Min(99, step + 3))));
            if (IsDisposed)
            {
                return;
            }

            if (result.ExitCode != 0)
            {
                Log.Error($"The installer failed with code {result.ExitCode}:\r\n{result.Output}");
                UpdateNoted($"The installation of Axit {version} failed. This version keeps running.");
                ShowUpdateFailure(release, "The installer failed; this version keeps running.",
                    $"The installer ended with exit code {result.ExitCode}. What it printed:\n{result.Output}");
                return;
            }

            Overlay.SetProgress(100);
            _restartArguments = restart ? [.. RestartArguments()] : null;
            Log.Info($"Update {release.Tag} installed in place; restart: {restart}");
            UpdateNoted(restart
                ? $"Axit {version} installed. {AppName} closes now and starts again."
                : $"Axit {version} installed. {AppName} closes now; the next start is the new version.");
            Announce(restart ? "Installed. Restarting" : "Installed. Closing", true);
            ClosingForUpdate = true;
            Close();
        }
        catch (OperationCanceledException) when (cancelled && !IsDisposed)
        {
            UpdateNoted($"The download of Axit {version} was cancelled.");
            Announce("Update cancelled", true);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException or InvalidDataException or InvalidOperationException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            // InvalidDataException: a download that is not a zip; OperationCanceledException here: the ten minutes
            // ran out; Win32Exception: PowerShell could not be started for the installer (a policy).
            Log.Error("The update failed", ex);
            if (!IsDisposed)
            {
                UpdateNoted($"The update to Axit {version} failed: {ex.Message}");
                ShowUpdateFailure(release, $"The update failed: {ex.Message}\nThis version keeps running.",
                    $"{ex.GetType().Name}: {ex.Message}{(ex.InnerException is { } inner ? $"\nCaused by {inner.GetType().Name}: {inner.Message}" : string.Empty)}");
            }
        }
        finally
        {
            SetUpdateBusy(false);
        }
    }

    /// <summary>
    /// A failed update's notice (AX-3.6): what failed, then the details to send, with Copy details, Open release page
    /// and Close. The details (<see cref="Updater.FailureDetails"/>) are the versions, Windows' version and what went
    /// wrong (the installer's output, or the error), generic, with nothing that names the user; the log keeps the whole text.
    /// </summary>
    private void ShowUpdateFailure(ReleaseInfo release, string summary, string what)
    {
        var version = release.Version.ToString(3);
        var details = Updater.FailureDetails(BundleInfo.Version, version, AppName, what);
        ShowNotice(new Notice($"Update to Axit {version}",
            $"{summary}\n\nYou can download the zip from the release page and run install.cmd yourself. Copy details puts the lines below on the clipboard, with your account's name and folders left out, to send with a report.\n\n{details}",
        [
            new OverlayChoice("Copy &details", () => CopyUpdateDetails(details), StaysOpen: true),
            ReleasePageChoice(release),
            OverlayChoice.Close,
        ]));
    }

    private void CopyUpdateDetails(string details)
    {
        try
        {
            Clipboard.SetText(details.Replace("\n", "\r\n"));
            Announce("Copied the details", true);
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.ExternalException or InvalidOperationException)
        {
            Announce("The clipboard is busy; try again", true);
        }
    }

    /// <summary>On the way out after Update and restart (AX-3.2): the new version starts on what this window held as this process ends.</summary>
    private void StartInstalledVersion()
    {
        if (_restartArguments is { } restart)
        {
            Updater.StartInstalled(restart);
        }
    }

    /// <summary>Keep working after Update and restart: the update is installed already; this window stays, and the new version comes with the next start.</summary>
    protected void KeepWorkingAfterUpdate()
    {
        if (_restartArguments is null)
        {
            return;
        }

        _restartArguments = null;
        Announce($"Update installed. It starts with the next {AppName}", true);
    }
}
