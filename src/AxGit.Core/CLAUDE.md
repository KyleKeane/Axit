# AxGit.Core, the pure code

Everything of AxGit that needs no window: git as a process and the parsers of its output. Unit tested in `tests/AxGit.Tests` from recorded git output, with no git process but a fake git.cmd the runner tests write. The spec is `docs/axgit/SPEC.md` ("SPEC.md" below); the window is `src/AxGit`, with its own `CLAUDE.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
dotnet test tests/AxGit.Tests/AxGit.Tests.csproj
git -C <repo> status --porcelain=v2 --branch --untracked-files=all   # what GitStatus.Parse reads
git -C <repo> log -n200 --date=short --format=%h%x09%ad%x09%an%x09%s   # what GitLog.Parse reads
git -C <repo> branch --list --format=%(refname:short)%09%(HEAD)%09%(upstream:short)%09%(upstream:track)
```

## Where things are

- `GitRunner`: `RunAsync` (one process per command, `-C <repo> -c core.quotepath=false -c color.ui=never` in front of the arguments, `GIT_TERMINAL_PROMPT=0`, both streams read in UTF-8 on tasks, a timeout that kills the process; `GitNotFoundException` when git is not on the PATH; `Program`, the executable, git unless the tests point it at a fake), `RootAsync` (the repository that holds a folder or file), `Command` (the argument list, pure, tested). `GitResult` carries the exit code and both texts, `FirstLine` for an announcement and `AllText` for a notice.
- `GitStatus` and `ChangedFile`, `RepositoryStatus` (SPEC.md AG-3.1): porcelain v2 to records; the item's `Label` ("Modified src/Foo.cs, unstaged"), the list's name, the status bar text, and the argument lists for Space (`StageArguments`), Enter (`DiffArguments`) and Delete (`DiscardArguments`), so that the window knows no git syntax. `Unquote` undoes git's C-style quoting of odd paths.
- `GitLog` and `Commit` (AG-4): the tab-separated log to records, `Label`, `ShowArguments`.
- `GitBranches` and `Branch` (AG-5): the branch list to records with the upstream state (a detached HEAD, which git lists in parentheses, left out), `Label`, `SwitchArguments`, `CreateArguments`.
- `GitSettings` (AG-8.1): the settings file, as `EditorSettings` is for the editors, with the recent repositories.

## Gotchas already learned

- `git status --porcelain=v2` without `-z` quotes a path only when it holds control characters, quotes or a backslash once `core.quotepath` is off; `Unquote` handles the C escapes and octal bytes. Renames come as one line with the new path, a tab, the old path.
- The log and branch formats use `%x09` and `%09` for tabs, so a subject with spaces or colons parses cleanly; a subject never holds a tab.
- `LC_ALL=C.UTF-8` keeps git's messages in English (the announcements quote them) and its output in UTF-8; `GIT_PAGER=cat` is belt and braces, git does not page without a terminal anyway.
