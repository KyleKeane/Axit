using System.Diagnostics;
using System.Text.RegularExpressions;
using AxGit.Core;
using Axit.Core;
using Axit.Forms;

namespace AxGit;

/// <summary>axGIT's window, running git (AG-1.5, AG-D6): every command on a background task, its answer back on this thread.</summary>
public sealed partial class GitForm
{
    // ---- running git (AG-1.5, AG-D6) ----

    /// <summary>
    /// One git command in the repository: the status bar says what runs, the git items are disabled meanwhile, and
    /// the result comes back for the caller to announce. Null when nothing ran: no repository, git missing, or a
    /// command still running ("Working…").
    /// </summary>
    private async Task<GitResult?> RunGitAsync(IReadOnlyList<string> arguments, TimeSpan timeout, string verb)
    {
        if (_root is null)
        {
            Announce("No repository", true);
            return null;
        }

        if (_gitMissing)
        {
            GitMissing();
            return null;
        }

        if (_busy is not null)
        {
            Announce("Working…", true);
            return null;
        }

        _busy = verb;
        SetGitItems(false);
        UpdateStatusBar();
        var watch = Stopwatch.StartNew();
        try
        {
            var result = await GitRunner.RunAsync(_root, arguments, timeout);
            // The subcommand alone (AG-8.2): the arguments hold the commit message and the paths.
            Log.Info($"git {arguments[0]}: exit {result.ExitCode} in {watch.ElapsedMilliseconds} ms{(result.Ok ? string.Empty : ", " + result.FirstLine)}");
            return result;
        }
        catch (GitNotFoundException)
        {
            GitMissing();
            return null;
        }
        finally
        {
            _busy = null;
            if (!IsDisposed)
            {
                SetGitItems(true);
                UpdateStatusBar();
            }
        }
    }

    private void SetGitItems(bool enabled)
    {
        foreach (var item in _gitItems)
        {
            item.Enabled = enabled && !_gitMissing;
        }
    }

    /// <summary>git is not on the PATH (AG-1.5): a notice each time a command is asked for, with the chime the first time, and every command stays disabled.</summary>
    private void GitMissing()
    {
        var first = !_gitMissing;
        _gitMissing = true;
        SetGitItems(false);
        UpdateStatusBar();
        ShowNotice(new Notice("Git was not found",
            "axGIT runs the git program, and it is not on the PATH of this computer. Install Git for Windows from git-scm.com, then start axGIT again.",
        [
            new OverlayChoice("&Open git-scm.com", () => OpenUrl("https://git-scm.com"), StaysOpen: true),
            OverlayChoice.Close,
        ])
        {
            Unprompted = first,
        });
    }

    /// <summary>A failed command: one announcement when git said one line, a notice with everything it said otherwise (AG-7).</summary>
    private void Failed(string title, GitResult result)
    {
        var text = result.AllText;
        if (text.Contains('\n'))
        {
            ShowNotice(Notice.Plain(title, text) with { Unprompted = true });
        }
        else
        {
            Announce("Failed: " + result.FirstLine, true);
        }
    }
}
