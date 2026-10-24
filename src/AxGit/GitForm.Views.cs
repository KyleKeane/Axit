using System.Diagnostics;
using System.Text.RegularExpressions;
using AxGit.Core;
using Axit.Core;
using Axit.Forms;

namespace AxGit;

/// <summary>axGIT's window, the views (AG-2.2): three records over one list, and the details and the status bar (AG-2.1, AG-2.3, AG-2.4).</summary>
public sealed partial class GitForm
{
    // ---- the views (AG-2.2): three records, one list ----

    private ListViewState Current => _views[(int)_view];

    /// <summary>F5, and every command's end: the status read again, the view shown read again with it, the list refilled on the item it had.</summary>
    private async Task RefreshAsync(bool announce)
    {
        if (_root is null)
        {
            Announce("No repository", true);
            return;
        }

        RememberSelection();
        if (!await LoadAsync(View.Changes))
        {
            return;
        }

        if (_view != View.Changes)
        {
            await LoadAsync(_view);
        }

        FillList();
        UpdateStatusBar();
        if (announce)
        {
            Announce("Refreshed: " + _list.AccessibleName, true);
        }
    }

    /// <summary>
    /// Reads one view from git into its record: the status for Changes (<see cref="_repo"/>, which the status bar and
    /// the commit need too), the log, the branches. False when git did not run or failed, which is said.
    /// </summary>
    private async Task<bool> LoadAsync(View view)
    {
        var (arguments, verb, title) = view switch
        {
            View.Changes => (GitStatus.Arguments, "status", "Status failed"),
            View.Log => (GitLog.Arguments, "log", "Log failed"),
            _ => (GitBranches.Arguments, "branches", "Branches failed"),
        };
        var result = await RunGitAsync(arguments, GitRunner.Short, verb);
        if (result is null)
        {
            return false;
        }

        if (!result.Ok)
        {
            Failed(title, result);
            return false;
        }

        var state = _views[(int)view];
        switch (view)
        {
            case View.Changes:
                _repo = GitStatus.Parse(result.Output);
                state.Set(_repo.ListName, _repo.Files.Select(file => file.Label), _repo.Files.Select(file => file.Path));
                break;
            case View.Log:
                _commits = GitLog.Parse(result.Output);
                state.Set(GitLog.ListName(_commits.Count), _commits.Select(commit => commit.Label), _commits.Select(commit => commit.Hash));
                break;
            default:
                _branches = GitBranches.Parse(result.Output);
                state.Set(GitBranches.ListName(_branches.Count), _branches.Select(branch => branch.Label), _branches.Select(branch => branch.Name));
                break;
        }

        return true;
    }

    /// <summary>The views to read again before they show next: a commit, a switch, a fetch changed what they list.</summary>
    private void Stale(params View[] views)
    {
        foreach (var view in views)
        {
            _views[(int)view].Loaded = false;
        }
    }

    /// <summary>View → Changes, Log, Branches (Ctrl+1, 2, 3): the list refilled, read on the first visit or when stale, and its name said with the item the focus is on (axClaude D26).</summary>
    private async Task ShowViewAsync(View view)
    {
        if (_root is null)
        {
            Announce("No repository", true);
            return;
        }

        if (_busy is not null)
        {
            // The view's first load would be refused and the list shown empty under the new name.
            Announce("Working…", true);
            return;
        }

        RememberSelection();
        _view = view;
        if (!Current.Loaded)
        {
            await LoadAsync(view);
        }

        FillList();
        var name = _list.AccessibleName ?? string.Empty;
        Announce(_list.SelectedItem is string item ? $"{name}. {item}" : name, true);
    }

    /// <summary>The list's items for the view shown, named with the view and the count, the selection on the view's key when it is still listed, else the first item.</summary>
    private void FillList() => Current.FillInto(_list);

    /// <summary>The key of the item selected in the view shown, kept in its record before the list changes under it (a refresh, a view switch).</summary>
    private void RememberSelection() => Current.Remember(_list);

    private ChangedFile? CurrentFile() =>
        _view == View.Changes && _list.SelectedIndex is var i && i >= 0 && i < _repo.Files.Count ? _repo.Files[i] : null;

    private Commit? CurrentCommit() =>
        _view == View.Log && _list.SelectedIndex is var i && i >= 0 && i < _commits.Count ? _commits[i] : null;

    private Branch? CurrentBranch() =>
        _view == View.Branches && _list.SelectedIndex is var i && i >= 0 && i < _branches.Count ? _branches[i] : null;

    /// <summary>
    /// Whether the list shows the view a command belongs to; otherwise which view first is said, "Changes view
    /// first: Ctrl+1" (AG-6.2). One rule for every command of a view since 2026-10-23: Space, Enter and the menu's
    /// Show diff switched the view silently, Delete said this, and Switch to branch switched too.
    /// </summary>
    private bool InView(View view)
    {
        if (_view == view)
        {
            return true;
        }

        Announce(view switch
        {
            View.Changes => "Changes view first: Ctrl+1",
            View.Log => "Log view first: Ctrl+2",
            _ => "Branches view first: Ctrl+3",
        }, true);
        return false;
    }

    // ---- the details and the status bar (AG-2.1, AG-2.3, AG-2.4) ----

    /// <summary>Text into the details, named for what it is; with <paramref name="focus"/> the focus goes to its first line and the name and length are said after it (AG-D5).</summary>
    private void ShowDetails(string name, string text, bool focus)
    {
        var normalized = text.Replace("\r\n", "\n").TrimEnd('\n').Replace("\n", "\r\n");
        var lines = normalized.Length == 0 ? 0 : normalized.Split("\r\n").Length;
        _details.AccessibleName = name;
        _details.Text = normalized;
        _details.Select(0, 0);
        _details.ScrollToCaret();
        if (focus)
        {
            _details.Focus();
        }

        Announce($"{name}, {lines} {(lines == 1 ? "line" : "lines")}", !focus);
    }

    private void UpdateStatusBar()
    {
        _stateLabel.Text = _busy is not null ? $"Working: {_busy}…"
            : _gitMissing ? "git was not found"
            : _root is null ? "No repository"
            : _repo.StatusText;
        _pathLabel.Text = _root ?? string.Empty;
        StatusLayout.Fit(_status, _stateLabel, _pathLabel);
    }

    /// <summary>An item that runs git: disabled while a command runs and when git is missing.</summary>
    private ToolStripMenuItem Git(ToolStripMenuItem item)
    {
        _gitItems.Add(item);
        return item;
    }
}
