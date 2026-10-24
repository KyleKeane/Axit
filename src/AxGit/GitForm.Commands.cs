using System.Diagnostics;
using System.Text.RegularExpressions;
using AxGit.Core;
using Axit.Core;
using Axit.Forms;

namespace AxGit;

/// <summary>axGIT's window, the commands of the three views: changes (AG-3), history (AG-4) and branches (AG-5).</summary>
public sealed partial class GitForm
{
    // ---- changes (AG-3) ----

    private async Task StageToggleAsync()
    {
        if (!InView(View.Changes))
        {
            return;
        }

        if (CurrentFile() is not { } file)
        {
            Announce("No file selected", true);
            return;
        }

        if (!file.CanStage)
        {
            Announce("A conflict: resolve it in the editor or the terminal first", true);
            return;
        }

        var result = await RunGitAsync(file.StageArguments, GitRunner.Short, file.Unstaged ? "stage" : "unstage");
        if (result is null)
        {
            return;
        }

        if (!result.Ok)
        {
            Failed(file.Unstaged ? "Stage failed" : "Unstage failed", result);
            return;
        }

        Announce(file.StageAnnouncement, true);
        await RefreshAsync(announce: false);
    }

    private async Task StageAllAsync(bool stage)
    {
        var result = await RunGitAsync(stage ? ["add", "-A"] : ["restore", "--staged", "."], GitRunner.Short, stage ? "stage all" : "unstage all");
        if (result is null)
        {
            return;
        }

        if (!result.Ok)
        {
            Failed(stage ? "Stage failed" : "Unstage failed", result);
            return;
        }

        Announce(stage ? "Staged all" : "Unstaged all", true);
        await RefreshAsync(announce: false);
    }

    /// <summary>Enter on a file (AG-3.3): its diff in the details, with the focus on the first line; an untracked file shows itself.</summary>
    private async Task ShowDiffAsync()
    {
        if (!InView(View.Changes))
        {
            return;
        }

        if (_root is null || CurrentFile() is not { } file)
        {
            Announce(_root is null ? "No repository" : "No file selected", true);
            return;
        }

        if (file.Kind == ChangeKind.Untracked)
        {
            var path = Path.Combine(_root, file.Path.Replace('/', Path.DirectorySeparatorChar));
            string text;
            try
            {
                text = await File.ReadAllTextAsync(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Fail($"{file.Path} could not be read: {ex.Message}");
                return;
            }

            if (text.Length > UntrackedLimit)
            {
                text = text[..UntrackedLimit] + "\n(cut after 200 KB)";
            }

            var lines = text.Length == 0 ? 0 : text.Split('\n').Length;
            ShowDetails($"Diff of {file.Path}", $"Untracked file, {lines} lines\n{text}", focus: true);
            return;
        }

        var result = await RunGitAsync(file.DiffArguments, GitRunner.Short, "diff");
        if (result is null)
        {
            return;
        }

        if (!result.Ok)
        {
            Failed("Diff failed", result);
            return;
        }

        if (result.Output.Trim().Length == 0)
        {
            Announce("No changes", true);
            return;
        }

        ShowDetails($"Diff of {file.Path}", result.Output, focus: true);
    }

    /// <summary>Changes → Discard changes… (Delete, AG-3.4): a question first; an untracked file is not deleted from here.</summary>
    private void Discard()
    {
        if (!InView(View.Changes))
        {
            return;
        }

        if (CurrentFile() is not { } file)
        {
            Announce("No file selected", true);
            return;
        }

        if (file.Kind == ChangeKind.Untracked)
        {
            Announce("Untracked file: delete it in File Explorer", true);
            return;
        }

        ShowNotice(new Notice($"Discard the changes to {file.Path}?", "They cannot be brought back.",
        [
            new OverlayChoice("&Discard", () => Do(async () =>
            {
                var result = await RunGitAsync(file.DiscardArguments, GitRunner.Short, "discard");
                if (result is null)
                {
                    return;
                }

                if (!result.Ok)
                {
                    Failed("Discard failed", result);
                    return;
                }

                Announce($"Discarded {file.Path}", true);
                await RefreshAsync(announce: false);
            }), IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ]));
    }

    /// <summary>Changes → Commit… (Ctrl+Enter, AG-3.5): the message in a notice, then git commit.</summary>
    private void Commit()
    {
        if (_root is null)
        {
            Announce("No repository", true);
            return;
        }

        if (_busy is not null)
        {
            // A stage or its refresh still runs: the count below is the one before it (found by tools/AxAutomate,
            // 2026-10-23: Space then Ctrl+Enter said "Nothing staged").
            Announce("Working…", true);
            return;
        }

        var staged = _repo.StagedCount;
        if (staged == 0)
        {
            Announce("Nothing staged. Space stages a file", true);
            return;
        }

        ShowNotice(new Notice($"Commit {staged} staged {(staged == 1 ? "file" : "files")}", string.Empty,
        [
            new OverlayChoice("&Commit", () =>
            {
                var message = Overlay.InputText.Trim();
                Do(async () =>
                {
                    var result = await RunGitAsync(["commit", "-m", message], GitRunner.Short, "commit");
                    if (result is null)
                    {
                        return;
                    }

                    if (!result.Ok)
                    {
                        Failed("Commit failed", result);
                        return;
                    }

                    var hash = CommitHashRegex().Match(result.Output);
                    Announce(hash.Success ? $"Committed {hash.Groups[1].Value}" : "Committed", true);
                    Stale(View.Log, View.Branches);
                    await RefreshAsync(announce: false);
                });
            }, IsDefault: true),
            new OverlayChoice("C&ancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("Message:", "Message", string.Empty, "Type the commit message"),
        });
    }

    // "[main 3a1f2c0] Subject" on the first line of git commit's output.
    [GeneratedRegex(@"^\[[^\]]+ ([0-9a-f]{7,})\]", RegexOptions.Multiline)]
    private static partial Regex CommitHashRegex();

    // ---- history (AG-4) ----

    private async Task ShowCommitAsync()
    {
        if (CurrentCommit() is not { } commit)
        {
            Announce("No commit selected", true);
            return;
        }

        var result = await RunGitAsync(commit.ShowArguments, GitRunner.Short, "show");
        if (result is null)
        {
            return;
        }

        if (!result.Ok)
        {
            Failed("Show failed", result);
            return;
        }

        ShowDetails($"Commit {commit.Hash}", result.Output, focus: true);
    }

    // ---- branches (AG-5) ----

    private async Task SwitchBranchAsync()
    {
        if (!InView(View.Branches))
        {
            return;
        }

        if (CurrentBranch() is not { } branch)
        {
            Announce("No branch selected", true);
            return;
        }

        if (branch.Current)
        {
            Announce($"Already on {branch.Name}", true);
            return;
        }

        var result = await RunGitAsync(branch.SwitchArguments, GitRunner.Short, "switch");
        if (result is null)
        {
            return;
        }

        if (!result.Ok)
        {
            Failed("Switch failed", result);
            return;
        }

        Announce($"Switched to {branch.Name}", true);
        Stale(View.Log, View.Branches);
        await RefreshAsync(announce: false);
    }

    /// <summary>Branches → New branch… (Ctrl+N, AG-5.3): the name in a notice, then git switch -c.</summary>
    private void NewBranch()
    {
        if (_root is null)
        {
            Announce("No repository", true);
            return;
        }

        ShowNotice(new Notice("New branch", "A new branch from the current commit, checked out at once.",
        [
            new OverlayChoice("C&reate", () =>
            {
                var name = Overlay.InputText.Trim();
                Do(async () =>
                {
                    var result = await RunGitAsync(GitBranches.CreateArguments(name), GitRunner.Short, "new branch");
                    if (result is null)
                    {
                        return;
                    }

                    if (!result.Ok)
                    {
                        Failed("New branch failed", result);
                        return;
                    }

                    Announce($"Switched to {name}", true);
                    Stale(View.Log, View.Branches);
                    await RefreshAsync(announce: false);
                });
            }, IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("Name:", "Name", string.Empty, "Type the branch name"),
        });
    }

    private async Task FetchAsync()
    {
        var result = await RunGitAsync(["fetch", "--prune"], GitRunner.Long, "fetch");
        if (result is null)
        {
            return;
        }

        if (!result.Ok)
        {
            Failed("Fetch failed", result);
            return;
        }

        Announce("Fetched", true);
        Stale(View.Branches);
        await RefreshAsync(announce: false);
    }

    private async Task PullAsync()
    {
        var result = await RunGitAsync(["pull", "--ff-only"], GitRunner.Long, "pull");
        if (result is null)
        {
            return;
        }

        if (!result.Ok)
        {
            Failed("Pull failed", result);
            return;
        }

        Announce(result.Output.Contains("Already up to date", StringComparison.OrdinalIgnoreCase) ? "Already up to date" : "Pulled", true);
        Stale(View.Log, View.Branches);
        await RefreshAsync(announce: false);
    }

    private async Task PushAsync()
    {
        IReadOnlyList<string> arguments = _repo.Upstream is null && _repo.Branch is { } branch
            ? ["push", "--set-upstream", "origin", branch]
            : ["push"];
        var result = await RunGitAsync(arguments, GitRunner.Long, "push");
        if (result is null)
        {
            return;
        }

        if (!result.Ok)
        {
            Failed("Push failed", result);
            return;
        }

        Announce("Pushed", true);
        Stale(View.Branches);
        await RefreshAsync(announce: false);
    }
}
