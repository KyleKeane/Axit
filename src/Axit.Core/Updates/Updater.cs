using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;

namespace Axit.Core.Updates;

/// <summary>
/// The bundle's side of updating (docs/axit/SPEC.md AX-3; AxClaude's FR-1.10): asks GitHub for the latest release,
/// downloads its zip into <c>%LOCALAPPDATA%\Axit\updates\&lt;version&gt;</c>, and hands over to that version's own
/// install.ps1, which waits for this process to end, installs over the current installation and starts the app
/// again. The running executable is never overwritten while it runs (AxClaude's D25).
/// </summary>
public static class Updater
{
    private static readonly HttpClient Http = CreateClient();

    public static string Folder { get; } = AppPaths.UpdatesFolder;

    /// <summary>What the installer did after this process ended, for a bug report.</summary>
    public static string LogPath { get; } = Path.Combine(AppPaths.LogsFolder, "update.log");

    /// <summary>The latest release, or null when none is published. Throws when GitHub cannot be reached.</summary>
    public static Task<ReleaseInfo?> CheckAsync(CancellationToken cancellation) => UpdateCheck.FetchLatestAsync(Http, cancellation);

    /// <summary>
    /// Downloads and extracts the release's zip and returns the folder that holds the new program file and install.ps1.
    /// A complete earlier download of the same version is reused. <paramref name="progress"/>, when given, hears the
    /// percentage downloaded each time it changes (the window shows it on a progress bar, AX-3.2).
    /// </summary>
    public static async Task<string> DownloadAsync(ReleaseInfo release, CancellationToken cancellation, IProgress<int>? progress = null)
    {
        if (release.ZipUrl is null)
        {
            throw new InvalidOperationException("The release has no Windows zip attached.");
        }

        var version = release.Version.ToString(3);
        var folder = Path.Combine(Folder, version);
        if (IsComplete(folder))
        {
            return folder;
        }

        Directory.CreateDirectory(Folder);
        // Only the version being installed is kept: earlier downloads are about 45 MB each.
        foreach (var old in Directory.GetDirectories(Folder).Where(path => !string.Equals(path, folder, StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                Directory.Delete(old, recursive: true);
            }
            catch (IOException)
            {
                // Still in use; the next update tries again.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        var zip = Path.Combine(Folder, $"Axit-{version}{UpdateCheck.ZipSuffix}");
        using (var response = await Http.GetAsync(release.ZipUrl, HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false))
        {
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength ?? release.ZipSize;
            try
            {
                await using var file = File.Create(zip);
                await using var stream = await response.Content.ReadAsStreamAsync(cancellation).ConfigureAwait(false);
                var buffer = new byte[64 * 1024];
                long done = 0;
                var lastPercent = -1;
                int read;
                while ((read = await stream.ReadAsync(buffer, cancellation).ConfigureAwait(false)) > 0)
                {
                    await file.WriteAsync(buffer.AsMemory(0, read), cancellation).ConfigureAwait(false);
                    done += read;
                    if (progress is not null && total > 0)
                    {
                        var percent = (int)Math.Min(100, done * 100 / total);
                        if (percent != lastPercent)
                        {
                            lastPercent = percent;
                            progress.Report(percent);
                        }
                    }
                }
            }
            catch
            {
                // A partial zip must not be taken for a download next time (the file is closed by now).
                try
                {
                    File.Delete(zip);
                }
                catch (IOException)
                {
                }

                throw;
            }
        }

        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }

        ZipFile.ExtractToDirectory(zip, folder);
        File.Delete(zip);
        if (!IsComplete(folder))
        {
            throw new InvalidOperationException("The downloaded zip does not contain a program file and install.ps1.");
        }

        return folder;
    }

    /// <summary>
    /// Starts the downloaded version's install.ps1 without a window. It waits for this process to end, installs, and
    /// starts the app again: AxClaude on <paramref name="projectFolder"/> when one is given, with <c>-- --continue</c>
    /// when asked, so that the conversation is picked up again, or AxDown on <paramref name="file"/> (AX-2.7 keeps
    /// these parameters). Its output goes to <see cref="LogPath"/>.
    /// </summary>
    public static void LaunchInstaller(string folder, string? projectFolder, bool continueConversation, string? file = null)
    {
        var start = new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = folder,
        };
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-ExecutionPolicy");
        start.ArgumentList.Add("Bypass");
        start.ArgumentList.Add("-File");
        start.ArgumentList.Add(Path.Combine(folder, "install.ps1"));
        start.ArgumentList.Add("-WaitForProcess");
        start.ArgumentList.Add(Environment.ProcessId.ToString());
        start.ArgumentList.Add("-LogFile");
        start.ArgumentList.Add(LogPath);
        if (projectFolder is not null)
        {
            start.ArgumentList.Add("-Start");
            start.ArgumentList.Add(projectFolder);
            if (continueConversation)
            {
                start.ArgumentList.Add("-ContinueConversation");
            }
        }

        if (file is not null)
        {
            start.ArgumentList.Add("-StartFile");
            start.ArgumentList.Add(file);
        }

        Process.Start(start)?.Dispose();
        Log.Info($"Update installer started from {folder}; it installs when this process ends and logs to {LogPath}");
    }

    /// <summary>
    /// A download is complete when it has install.ps1 and a program file. The program file is not named: the
    /// installer knows what to install, and 1.7.0 relied on this to update into Axit (docs/axit/SPEC.md B-6).
    /// </summary>
    private static bool IsComplete(string folder) =>
        File.Exists(Path.Combine(folder, "install.ps1")) && Directory.Exists(folder) && Directory.GetFiles(folder, "*.exe").Length > 0;

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Axit", BundleInfo.Version));
        return client;
    }
}
