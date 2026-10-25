# axGIT — Specification

axGIT is the fourth app of the Axit bundle (`docs/axit/SPEC.md`): a git manager for one repository, for a blind
developer who uses the NVDA screen reader. Built 2026-10-18 and released in Axit 2.12.0; the whole test plan passed under NVDA on
2026-10-22 (the record: `docs/axit/nvda-test-record.md`); what each release brought is section 8. The document is kept
in step with the code (a behaviour change updates it in the same commit).
Requirements are numbered `AG-n.m` and decisions `AG-Dn`; the bundle's rules, AX-7 (accessibility), AX-8 (keys) and
AX-9 (the Axit menu), are cited by number and not repeated. In axGIT's code and documents, "SPEC.md" means this file.

## 1. Summary

A developer who works in axClaude and axDown wants to know the state of the repository under them and to act on it
without a terminal: which files changed, what changed in one of them, stage some, commit with a message, look at the
log, switch a branch, pull and push. axGIT opens one repository in one window, named after it. The window is one list
with three views (Changes, Log, Branches), one read-only text for details (a diff, a commit, a command's output), a
status bar with the branch, and the bundle's notices for the commit message and every question. Everything is the
`git` program the user already has: axGIT runs it and reads its output, and reimplements nothing (AG-D2).

The window is reached from the other windows: axClaude's Session → Show current folder in ▸ axGIT and the editors'
File → Show current file in ▸ axGIT open axGIT on the repository that holds the folder or file, and axGIT's Repository
→ Show current repository in ▸ sends the user back to axClaude on the repository, to axTerminal, to axFile or to File
Explorer; a changed file
opens in the editor for its type (AG-1.4, AG-D1).

## 2. Goals and non-goals

- **G1 The daily set, streamlined.** Status, diff, stage and unstage, discard, commit, log and a commit's details,
  branches with switch and new, fetch, pull, push. Conflicts are shown as what they are and left to the terminal or to
  Claude (AG-D4).
- **G2 Native controls only.** A list box, a read-only edit control, a status bar, the bundle's notices (AX-7.1). No
  tree, no grid, no colours that carry meaning.
- **G3 Nothing spoken unprompted** (AX-7.3; axClaude D26): arrows explore, a key acts, and the result is one
  announcement. The focus moves by itself only to the details text after Enter on a file or a commit, which is the
  user's own command to read it.
- **G4 git as it is.** Every action is one git command with its own messages; axGIT adds no state of its own beyond
  the recent repositories and the window.
- **Non-goals.** Merge and rebase, stashes, tags, remotes management, submodules, interactive hunks, conflict
  resolution, a history graph, credentials (git's own helpers do that; a prompt that git would show in a terminal
  fails here and is reported). Several repositories in one window.

## 3. Scenarios

1. In axClaude on a project, Alt+S, I, G: axGIT opens on the repository, the Changes list focused: "Changes, 3 files.
   Modified src/Foo.cs, unstaged, 1 of 3". Space: "Staged src/Foo.cs". Ctrl+Enter, type the message, Enter:
   "Committed 3a1f2c0". Alt+A, O, 1 back to axClaude.
2. Enter on a changed file: the diff in the details text, the focus on its first line; Down Arrow reads it line by
   line; Ctrl+Tab back to the list.
3. Ctrl+2: "Log, 200 commits. 3a1f2c0 2026-10-18 Kyle: Release 2.11.0, 1 of 200". Enter: the commit's files and
   message in the details.
4. Ctrl+3: "Branches, 2. main, current, origin/main, 1 ahead". Down, Enter on `axit`: "Switched to axit". Ctrl+Shift+H:
   "Pushed".
5. In axDown on `docs/plan.md`, Alt+F, I, G: axGIT on the repository that holds the file. Ctrl+1 shows it among the
   changes if it changed.

## 4. Functional requirements

### AG-1 Launch

- AG-1.1 `Axit.exe git [<folder or file>]`; the console command `axgit`; the Start menu entry "Axit axGIT"; axGIT under
  "Open with Axit" on folders in File Explorer (AX-2.3). A folder or file given alone to `Axit.exe` still means axClaude or an editor
  (AX-1.2); axGIT is always named by its verb.
- AG-1.2 The repository is the one that holds the path: `git rev-parse --show-toplevel` from the folder (or the file's
  folder). Without a path, the last repository opened, or none: the window opens with "No repository" and Open
  repository… (Ctrl+O), the Windows folder picker, which resolves the chosen folder the same way. A folder in no
  repository gives the notice "Not a git repository" naming the folder; a repository already open stays open behind
  the notice (until 2026-10-23 it was dropped first).
- AG-1.3 `AxGitApp.Run(args)` is the entry point (AX-1.7): `Log.UseApp("axgit")`, the crash handling, `--help` and
  `--version`, the settings (`axgit.json`), then `GitForm`.
- AG-1.4 **To and from the other windows.** axClaude's Session → Show current folder in ▸ **axGIT** starts axGIT on
  the project folder (axClaude FR-8.2); axDown's and axJSON's File → Show current file in ▸ **axGIT** on the file
  (axDown AD-4.1). Each is a new window, a new process (`AxitMenu.Start`), found again in the Axit menu's Open windows
  (AX-9.3). From axGIT, Repository → Show current repository in ▸ **File Explorer** (Ctrl+W), ▸ **axClaude**
  (a new axClaude on the repository's folder), ▸ **axTerminal** (a shell there, docs/axterminal/SPEC.md AT-1.5) and
  ▸ **axFile** (the folder listed, docs/axfile/SPEC.md AF-1.3), and Changes → **Open file in editor** (Ctrl+E) opens
  the selected file in axJSON for `.json` and axDown otherwise.
- AG-1.5a When git is missing (neither on the PATH nor in Program Files, `GitRunner.Program`), its notice offers
  Install Git now (bundle AX-2.9) and, once git is there, opens the repository.
- AG-1.5 Git itself: `git.exe` in the PATH's full folders, never the current folder (`GitRunner.Program`,
  `SystemPrograms.OnPath`; the tests point it at a fake `git.cmd` for the output, the timeout and the open pipe),
  run with `-C <repository>`, `-c core.quotepath=false`, `-c color.ui=never`, `-c core.fsmonitor=false`,
  `-c protocol.ext.allow=never` (security audit S-1, S-2), diffs and `show` with `--no-ext-diff --no-textconv`,
  `GIT_TERMINAL_PROMPT=0` and UTF-8 output; one command at a time, on a background task, never on the UI thread
  (AX-7.5), with a timeout (60 seconds, 10 minutes for fetch, pull and push). When `git` is not found, the notice
  **Git was not found** says where to get it (git-scm.com) and the window stays with every command disabled.
- AG-1.6 **A repository's own programs** (2026-10-24, security audit S-2). A repository that arrives with its `.git`
  folder (an archive, a USB stick, a share) can name programs git runs: a file monitor (`git status`), diff and filter
  drivers, hooks (commit, switch), an ssh command (fetch, push), a credential helper. On opening, before any other
  command, axGIT reads the names of the repository's configuration (`git config --local --includes --name-only
  --list`, which runs nothing) and its hooks folder (`git rev-parse --git-path hooks`); each that runs a program
  (`GitTrust`) is set to nothing on every command (`-c <name>=`, the hooks folder `NUL`), and the notice **This
  repository runs programs of its own** names them, with **Keep them off** (Enter, Escape) and **Trust this
  repository**, which keeps the repository in the settings (`trustedRepositories`) and lets git run them as in a
  console from then on.

### AG-2 The window

- AG-2.1 From the top: the menu bar; the list, named with its view and count ("Changes, 3 files", "Log, 200 commits",
  "Branches, 2"); the details text under it, read-only, named "Details" and, after a diff, "Diff of <file>"; the
  status bar. Tab, Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab and F6 move between the list and the details (AX-8.2); a
  notice takes the place of both while it shows (AX-6.4).
- AG-2.2 Views: Ctrl+1 Changes, Ctrl+2 Log, Ctrl+3 Branches (View menu). The key says the list's name and the item
  the selection is on, also when the list already shows the view (axClaude D26); while a command runs it says
  "Working…" and the view stays (2026-10-23: the switch went through with an empty list under the new name). Each
  view keeps its selection while the window is open, by the item's key (the file's path, the commit's hash, the
  branch's name), taken before the lists change (2026-10-23: taken after, two views with the same count swapped
  selections); F5 refreshes the view and the status bar the same way and says "Refreshed: <the list's name>". Each view is one
  record in the window (`ViewState`: the list's name, the items, their keys, whether it has been read since it was
  last made stale, the selected key); the three records are the whole state of the list (2026-10-23).
- AG-2.3 The title is `<repository folder name> - axGIT`, "No repository - axGIT" without one. The status bar's left
  label: the branch, then the upstream state when there is an upstream ("main · 2 ahead" / "main · 1 behind" / "main
  · up to date" / "main · no upstream"), then the counts ("3 changed, 1 staged"); "Working: pull…" while a command
  runs. The right label: the repository's full path. No AccessibleName, no Spring (axClaude §6.2).
- AG-2.4 The details text: the bundle's edit keys (Page Up and Page Down, Ctrl+Up and Ctrl+Down by paragraph, AX-8.2),
  Ctrl+A and Ctrl+C, find with Ctrl+F, F3 and Shift+F3 (the match selected, "Not found" when none); with the focus in the
  list, Ctrl+F, F3 and Shift+F3 find in the list's lines instead ("Name contains", the bundle's list find, as every list
  window; since 2026-10-25, the user's choice; they always searched the details before). Nothing moves the
  caret but the user, except that Enter on a file or a commit puts the focus on the first line of what it loaded.

### AG-3 Changes

- AG-3.1 The Changes view lists `git status --porcelain=v2 --branch` (`GitStatus.Parse`): one item per file, "<kind>
  <path>, <state>": kinds Modified, Added, Deleted, Renamed (with "old → new"), Copied, Type changed, Untracked,
  Conflict; states "staged", "unstaged", "staged and unstaged"; an untracked file and a conflict have no state. Order:
  conflicts, then the rest as git lists them. The list's name is "Changes, N files" ("Changes, no changes" when the
  tree is clean).
- AG-3.2 Space on an item stages the whole file (`git add -- <path>`, also an untracked one) when any of it is
  unstaged, otherwise unstages it (`git restore --staged -- <path>`); the item's text changes in place and the
  announcement is "Staged <path>" or "Unstaged <path>". Changes → Stage all (Ctrl+Shift+A, `git add -A`) and Unstage
  all (Ctrl+Shift+U, `git restore --staged .`) say "Staged all" and "Unstaged all". A conflict is not staged from here.
- AG-3.3 Enter on an item shows its diff in the details ("Diff of <path>"): `git diff --cached` for a staged file,
  `git diff` for an unstaged one, `git diff HEAD` for one that is both, the file's own text for an untracked one
  (first 200 KB, "Untracked file, N lines" as the first line), `git diff` for a conflict. The focus goes to the first
  line of the details; "No changes" is announced and the focus stays when the diff is empty.
- AG-3.4 Changes → Discard changes… (Delete) on a tracked file asks "Discard the changes to <path>? They cannot be
  brought back." with Discard and Cancel; Discard runs `git restore --staged --worktree -- <path>` and says
  "Discarded <path>". An untracked file is not deleted from here ("Untracked file: delete it in File Explorer").
- AG-3.5 Changes → Commit… (Ctrl+Enter): with nothing staged, "Nothing staged. Space stages a file" and no notice;
  otherwise the notice "Commit N staged files" with a text field "Message" (required; the first line is the subject)
  and the buttons Commit and Cancel. Commit runs `git commit -m <message>` and says "Committed <short hash>"; the
  Changes view refreshes. A failure (a hook, an identity not set) is the notice **Commit failed** with git's output.

### AG-4 History

- AG-4.1 The Log view lists the last 200 commits of the current branch (`git log -n 200 --date=short
  --format=<hash><tab><date><tab><author><tab><subject>`, `GitLog.Parse`): "<short hash> <date> <author>: <subject>".
  The list's name is "Log, N commits".
- AG-4.2 Enter on a commit shows `git show --stat --format=medium <hash>` in the details ("Commit <short hash>") with
  the focus on its first line.

### AG-5 Branches

- AG-5.1 The Branches view lists the local branches (`git branch --list --format=…`, `GitBranches.Parse`): "<name>,
  current" for the branch checked out, then its upstream and state when it has one ("origin/main, 1 ahead", "up to
  date", "2 behind, 1 ahead"), "no upstream" otherwise. The list's name is "Branches, N". A detached HEAD is no branch: the line git prints
  for it, "(HEAD detached at <hash>)", is left out and no branch reads current (until 2026-10-23 it was listed as
  one); the status bar says "detached" (AG-2.3).
- AG-5.2 Enter on a branch switches to it (`git switch <name>`): "Switched to <name>", the views refresh; Enter on the
  current one says "Already on <name>". git's refusal (changes that would be overwritten) is the notice **Switch
  failed** with its output.
- AG-5.3 Branches → New branch… (Ctrl+N): the notice "New branch" with a field "Name" (required) and the buttons Create
  and Cancel; Create runs `git switch -c <name>` and says "Switched to <name>".
- AG-5.4 Branches → Fetch (Ctrl+Shift+F, `git fetch --prune`), Pull (Ctrl+Shift+L, `git pull --ff-only`) and Push
  (Ctrl+Shift+H, Ctrl+Shift+P until the command palette took it, bundle AX-10.6; `git push`; with no upstream `git push --set-upstream origin <branch>`). Each says "Fetched",
  "Pulled" ("Already up to date" when git says so) or "Pushed" and refreshes the status bar; a failure is the notice
  **Fetch failed** (and so on) with git's output, so that a password prompt that git could not show, a diverged
  branch or a rejected push is read in full. While one runs the status bar says "Working: <verb>…" and the menu
  items that run git are disabled; nothing else is blocked.

### AG-6 Menus, keys, texts

- AG-6.1 Menus, in the standard order of a first menu (bundle AX-6): **Repository (R):** Open repository… Ctrl+O |
  Open recent repository ▸ (ten, "1. name (path)", AX-9.5; "No recent repositories" when none) | ─ | Refresh F5 | ─ |
  Show current repository in ▸ (File Explorer Ctrl+W | axClaude | axTerminal | axFile) | ─ | Exit Alt+F4. **Changes (C):** Stage or unstage
  Space | Stage all Ctrl+Shift+A | Unstage all Ctrl+Shift+U | ─ | Show diff Enter | Open file in editor Ctrl+E |
  Discard changes… Delete | ─ | Commit… Ctrl+Enter. **Branches (B):** Switch to branch Enter | New branch… Ctrl+N |
  ─ | Fetch Ctrl+Shift+F | Pull Ctrl+Shift+L | Push Ctrl+Shift+H. **View (V):** Changes Ctrl+1 | Log Ctrl+2 |
  Branches Ctrl+3. **Options (O):** Text ▸ (Font… | Larger text Ctrl+Plus | Smaller text Ctrl+Minus | Windows text
  size) | General ▸ (Check for updates at start ✓) | Developer ▸ (Open settings file | Copy diagnostics). **Axit (A)**
  (AX-9). **Help (H):** Keyboard shortcuts F1 | User guide | ─ | Git resources ▸ (git-scm.com | Pro Git book).
  Every item has a mnemonic (AX-7.4).
- AG-6.2 Keys (AX-8). Bundle-wide: F1, the control keys, Ctrl+F, F3, Shift+F3, Ctrl+S (axGIT holds nothing to save:
  "Nothing to save here. Space stages, Ctrl+Enter commits"), Ctrl+Plus, Ctrl+Minus, the edit keys of the details,
  Alt+F4. Window-specific (`AxGitKeys`): Ctrl+O, F5, Ctrl+W, Ctrl+1, Ctrl+2, Ctrl+3, Space and Enter and Delete in the
  list, Ctrl+Shift+A, Ctrl+Shift+U, Ctrl+E, Ctrl+Enter, Ctrl+N, Ctrl+Shift+F, Ctrl+Shift+L, Ctrl+Shift+H. Space,
  Enter and Delete act only while the list has the focus; in the details they are the control's own. The collision
  test covers the table. A command of one view asked for in another says which view first: "Changes view first:
  Ctrl+1" for Space, Delete, Stage or unstage, Show diff and Discard changes, "Branches view first: Ctrl+3" for
  Switch to branch (one rule since 2026-10-23: Space, Enter and Show diff switched the view silently, Delete said this).
- AG-6.3 The F1 text (`HelpText.Shortcuts`) names them all; `docs/axgit/user-guide.md` is embedded (Help → User guide);
  About is the bundle's (AX-9.9).

### AG-7 Announcements

One UI Automation notification each, on the list, or on the notice while one shows (AX-7.3): "Staged <path>",
"Unstaged <path>", "Staged all", "Unstaged all", "Nothing staged. Space stages a file", "Committed <hash>", "Discarded
<path>", "Untracked file: delete it in File Explorer", "No changes", "Diff of <path>, N lines", "Commit <hash>, N
lines", "Switched to <name>", "Already on <name>", "Fetched", "Pulled", "Already up to date", "Pushed", "Refreshed:
<list name>", "Changes, N files" and the other view names when the view's key is pressed on the view already shown,
"Working…" when a command is asked for while one runs, "Nothing to save here. Space stages, Ctrl+Enter commits",
"Not found" (find), "Text size n", "Windows text size", "Copied diagnostics", "No repository" (a command without
one), "No file selected", "No commit selected", "No branch selected", "Changes view first: Ctrl+1", "Branches view first:
Ctrl+3" (a command of one view asked for in another, AG-6.2), "A conflict: resolve it in the editor or the terminal first" (a pull or a switch that git refuses for
one). A failure that git explains in one line is announced as "Failed: <line>"; more than one line is a notice.

### AG-8 Settings, status

- AG-8.1 `%APPDATA%\Axit\axgit.json` (AX-5.3; `GitSettings`): `fontFamily`, `fontSize`, `fontBold`,
  `checkForUpdates`, `recentRepositories` (ten, newest first), `window`. The defaults are the Windows font, the update
  check on, no repositories.
- AG-8.2 The log is `%LOCALAPPDATA%\Axit\logs\axgit.log`: every git command by its subcommand ("git commit") with its
  exit code and duration, never its arguments (a commit's message, the paths; until 2026-10-23 the whole argument
  list was logged) nor its output beyond the first line of an error. A git command that cannot end (a program it
  started keeps its output open) is cut off at the timeout like one that hangs, with what was read so far (AG-1.5).

## 5. Architecture

```
src/AxGit.Core/   class library (net10.0): GitRunner (git.exe as a process: arguments in, exit code and texts out,
                  async, with the repository and the environment), GitStatus (porcelain v2 to ChangedFile records and
                  the branch state), GitLog (the log lines to Commit records), GitBranches (the branch lines to Branch
                  records), GitSettings; references Axit.Core
src/AxGit/        class library (net10.0-windows): AxGitApp (Run, Usage), GitForm (the window), AxGitKeys, HelpText
                  (F1 text, the embedded user guide); references AxGit.Core, Axit.Core, Axit.Forms
tests/AxGit.Tests/ GitStatusTests, GitLogTests, GitBranchesTests, GitRunnerTests (the argument list and the settings):
                  the parsers and the labels, from recorded git output, no git process
docs/axgit/       this spec, user-guide.md (embedded), nvda-test-plan.md
```

`GitForm` derives from the bundle's window base, `Axit.Forms/AxitForm` (bundle AX-6.5), which holds the notice flow,
the fonts, the window's place, find and the update flow; the base was made from the editor shell when axGIT would
have been the third copy of them.

## 6. Design decisions

- **AG-D1 A window per repository, reached from where the user is.** One window holds one thing (bundle B-2), so a
  repository gets a window named after it, and the Axit menu finds it again. The worry was a window detached from
  the file or folder under work; the answer is the way in and out: the "Show current … in ▸" submenus the other
  windows already keep for an Axit app beside File Explorer open axGIT on the repository of the folder or file the
  user is in, and axGIT's own submenu sends back. Inside axClaude or the editors it would have been a Git menu three
  times over with a second set of lists in windows built around one thing; and in axClaude much of it can be asked of
  Claude, so axGIT's worth is the quick, deterministic read of a repository without spending a turn.
- **AG-D2 git.exe, nothing reimplemented.** A library would be a dependency (B-10) and a second git; the program the
  user has is the one source of truth, its messages are the ones the user knows, and reading its porcelain output is
  a parser of a hundred lines, tested.
- **AG-D3 One list with views, the state in the words.** NVDA reads a list box best. The staged state is part of the
  item's text ("…, staged") rather than a check box, so that arrows explore without touching anything and Space is
  the one key that acts; the same list shows the log and the branches with Enter acting on the view's item.
- **AG-D4 Streamlined scope.** The daily set only; everything else is a non-goal until it is asked for, since the
  whole of git is endless and a file-manager-sized menu is what the user said to avoid.
- **AG-D5 Announce on demand, focus to the details on Enter.** Nothing speaks on arrows. Enter on a file or a commit
  is the command to read it, so the focus goes to the details, the one place the focus moves by itself, like a notice.
- **AG-D6 Commands on a background task.** git can take seconds (a push) or minutes (a fetch over a slow line); the
  window stays alive, the status bar says what is running, and one command runs at a time so that the lists never
  show two states.

## 7. Open questions

- Whether Enter on a conflict should open the file in the editor rather than show the diff.
- A "next hunk" key in the details text (a `TextJump` pair for lines that start with `@@`).
- Whether the Log should take a file from the Changes view (the file's history).

## 8. Status

- First draft on 2026-10-18, released in 2.12.0 the same day: AG-1 to AG-8 built; heard under NVDA by the user:
  the start from axClaude and axDown, the three views, a diff, staging and unstaging, the notices, Ctrl+E into
  axDown, and the way back through the Axit menu; `docs/axgit/nvda-test-plan.md` sections 3.5 to 4.5 (commit,
  discard, branches, fetch, pull, push) are still to run. The window moved onto the bundle's window base the same
  day (AX-6.5).
- 2026-10-22: released in 3.3.1 (3.3.0 tagged, not published). The whole NVDA test plan marked passed at the user's word, no step reported failing; the record by step is `docs/axit/nvda-test-record.md`.
