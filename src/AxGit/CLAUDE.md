# AxGit, the window

The WinForms side of AxGit: one list with three views, one read-only details text, a status bar, the bundle's notices. The pure code (git as a process, the parsers, the settings) is `src/AxGit.Core`, with its own `CLAUDE.md`. The spec is `docs/axgit/SPEC.md` ("SPEC.md" below); the guide `docs/axgit/user-guide.md` is embedded by `AxGit.csproj` for Help → User guide; the manual test plan is `docs/axgit/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\run-axgit.ps1 C:\path\to\repo   # build Axit and start AxGit on the repository that holds the path (run.ps1 -App git)
.\run-axgit.ps1                   # the last repository opened
dotnet build Axit.sln
dotnet test tests/AxGit.Tests/AxGit.Tests.csproj
```

## Where things are

- `AxGitApp`: the entry point `Run` that Axit's `Program` calls (bundle AX-1.7), one call into `Axit.Forms/AppHost.Run` with the one folder or file of the command line, the settings and the window; the log (`axgit.log`), the crash handling, `--help` and `--version` are the host's.
- `GitForm`: the window (SPEC.md AG-2). The menus (AG-6.1); `ProcessCmdKey`, the one key switch, from `BundleKeys` and `AxGitKeys`; the views (`ShowViewAsync`, `FillList`, the selection kept per view by path, hash or name); the commands, each one `RunGitAsync` call with the argument list the Core's records give (`StageToggleAsync`, `ShowDiffAsync`, `Discard`, `Commit`, `ShowCommitAsync`, `SwitchBranchAsync`, `NewBranch`, `FetchAsync`, `PullAsync`, `PushAsync`); `ShowDetails`; the notice flow, the fonts, the window placement, find and the update flow are the window base's (`Axit.Forms/AxitForm`, bundle AX-6.5), which `GitForm` fills in with its hooks: the list and the details hidden for a notice, the list as the focus target (the details keep the focus across a notice by the base's rule), the font on both, `git` plus the repository as the restart arguments, Ctrl+S's "Nothing to save here. Space stages, Ctrl+Enter commits" (`SaveCommand`).
- `AxGitKeys`: the window's keys (SPEC.md AG-6.2; bundle AX-8), the one place a key is changed; `All` feeds the collision test and the F1 text names the same keys.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's.

## Rules

1. Every control gets an `AccessibleName`; the list is named with its view and count; nothing speaks on a plain arrow key; a command's result is one announcement (AG-7), on the list, or on the notice while one shows.
2. git never runs on the UI thread: `RunGitAsync` awaits the Core's process on a task, one command at a time (`_busy`), the status bar saying what runs and the git menu items disabled meanwhile.
3. No second window (bundle AX-7.2): questions and errors are notices; the folder picker and File Explorer are the system's own windows.
4. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan.

## Gotchas already learned

- Enter in a `ListBox` reaches `ProcessCmdKey` as `Keys.Return`; handled there with a `_list.Focused` check, so that Enter in the details stays the control's own, and returning true keeps the list from dinging.
- The base's `Do(Func<Task>)` wraps every command: an exception out of an awaited task would otherwise be unobserved; there it becomes an error notice.
- Changing the list's `AccessibleName` while it has the focus is silent for NVDA; a view switch therefore announces the name and the focused item itself (AG-2.2).
- The focus moves to the details after Enter on a file or a commit (AG-D5); the announcement is raised after the focus change and without interrupting, so that NVDA's reading of the control is not cut off.
