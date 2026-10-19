# AxFile, the window

The WinForms side of AxFile: one list with a folder's entries, a video area shown only while a video plays, a status bar, the bundle's notices. The pure code (the folder's entries, the media rules, the player seam, the operations, the settings) is `src/AxFile.Core`, with its own `CLAUDE.md`. The spec is `docs/axfile/SPEC.md` ("SPEC.md" below); the guide `docs/axfile/user-guide.md` is embedded by `AxFile.csproj` for Help → User guide; the manual test plan is `docs/axfile/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\run-axfile.ps1 C:\Music\Dune    # build Axit and start AxFile on the folder (run.ps1 -App file); a file selects itself in its folder
.\run-axfile.ps1                  # the last folder opened
dotnet build Axit.sln
dotnet test tests/AxFile.Tests/AxFile.Tests.csproj
```

## Where things are

- `AxFileApp`: the entry point `Run` that Axit's `Program` calls (bundle AX-1.7), one call into `Axit.Forms/AppHost.Run` with the one folder or file of the command line, the settings and the window; the log (`axfile.log`), the crash handling, `--help` and `--version` are the host's.
- `FileForm`, in four partial files. `FileForm.cs`: the controls, the menus (AF-6.1), the hooks the window base asks for, `ProcessWindowKey` (the one key switch, from `BundleKeys` and `AxFileKeys`; Enter, Backspace, Space, F2 and Delete only while the list has the focus), `Do` (a task with an escaped exception turned into a notice), `Failed` (one line as an announcement, more as a notice). `FileForm.Browse.cs`: `StartAsync`, `ShowFolderAsync` (the read on a task, the list refilled with the selection by name, the title, the recent list; a folder gone gives the notice and the nearest parent that exists), `FillList`, `OpenSelected` (Enter by `FolderListing.WayOf`), `GoUp`, the folder picker, the recent submenu, Go to folder, refresh (F5, and silently on activation when the folder's write time moved), find in the list, information. `FileForm.Operations.cs`: new folder, rename, delete, copy path, the File Explorer and the other apps' windows, open in editor, open with Windows. `FileForm.Player.cs`: the engine made on first use with the video area's handle, the queue from the folder at the moment of Enter (AF-D6), `PlayCurrent`, the events (`OnStarted`, `OnEnded`, `OnFailed`, `MoveOn`), the transport keys, the speed, Go to time, Ctrl+Q, the status bar's clock, `FitVideo`.
- `AxFileKeys`: the window's keys (SPEC.md AF-6.2; bundle AX-8), the one place a key is changed; `All` feeds the collision test and the F1 text names the same keys.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's.

## Rules

1. Every control gets an `AccessibleName`; the list is named with the folder and its count; nothing speaks on a plain arrow key; a command's result is one announcement (AF-7), on the list, or on the notice while one shows. The player's "Playing <name>" when it moves on by itself is the one unprompted announcement, and a setting.
2. The folder reads and the operations never run on the UI thread: `Task.Run` for the reads, the rename and the new folder, a thread of its own for the shell's delete; the player's events come back on the UI thread through the `BeginInvoke` the window hands `MediaPlayer` at creation.
3. No second window (bundle AX-7.2): questions and errors are notices; the folder picker and File Explorer are the system's own windows; the video area is a panel inside the window.
4. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan.

## Gotchas already learned

- Enter in a `ListBox` reaches `ProcessCmdKey` as `Keys.Return`; handled there with a `_list.Focused` check, and returning true keeps the list from dinging. Backspace, Space, F2 and Delete are the same.
- A read that comes back after the user has moved on is dropped (`_reads`): a slow folder would otherwise overwrite a faster one chosen later.
- The video area's handle must exist before the engine is made (`_video.Handle` creates it, hidden or not); the engine needs it at creation and draws nothing in it for a sound file.
- The queue holds paths, not entries: the list may show another folder by the time a file ends.
