# axMedia, the window

The WinForms side of axMedia: one list with the queue, a video area shown only while a video plays, a status bar, the bundle's notices. The pure code (the player seam, the queue, a folder's media files, the settings) is `src/AxMedia.Core`, with its own `CLAUDE.md`. The spec is `docs/axmedia/SPEC.md` ("SPEC.md" below); the guide `docs/axmedia/user-guide.md` is embedded by `AxMedia.csproj` for Help → User guide; the manual test plan is `docs/axmedia/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`. The window grew out of axFile's player and queue pane on 2026-10-22 (`docs/axit/axfile-split.md`).

## Commands

```
.\run-axmedia.ps1 C:\Music\Dune           # build Axit and start axMedia on the folder's media files (run.ps1 -App media)
.\run-axmedia.ps1 C:\Music\Dune\03.mp3    # the file, with the folder's other media files after it
.\run-axmedia.ps1                         # an empty queue
dotnet build Axit.sln
dotnet test tests/AxMedia.Tests/AxMedia.Tests.csproj
```

## Where things are

- `AxMediaApp`: the entry point `Run` that Axit's `Program` calls (bundle AX-1.7): the paths, `--add` and `--new` read off the command line, then, with paths and an open axMedia window, one `AppMessage.Send` and no window of this process (SPEC.md AM-1.2); otherwise one call into `Axit.Forms/AppHost.Run` with the settings and the window on a `HandOver`; the log (`axmedia.log`), the crash handling, `--help` and `--version` are the host's.
- `MediaForm`, in two partial files since 2026-10-24 (`MediaForm.Player.cs` is playing, AM-4: the engine, the transport, the position, the engine's events): the controls (the list, the video area, the status bar), the menus (AM-5.1), the hooks the window base asks for (`TakeHandOver`: a hand-over message, answered by the base and queued through `Apply`, the queue drained in order by `DrainAsync`), `ProcessWindowKey` (the one key switch, from `BundleKeys` and `AxMediaKeys`; Enter, Delete and Space only while the list has the focus); the base's `Do` runs every task (an escaped exception becomes a notice); the queue: `ApplyAsync` (a play replaces the queue through `MediaFolder.Queue` and plays with the playing file selected, an add appends through `MediaFolder.Expand` and says nothing, since axFile says it, AM-D3), `FillList` (the lines changed in place while the count is the same, so that the selection stays and only the changed line moves under NVDA; the list named with its count), `AddPaths` (Open files, Open folder, a recent item: "Added n"), `FillRecent`, `PlaySelected`, `RemoveSelected`, `ClearQueue`, `SayInformation`, `CurrentFile` (the playing file, or the selected one) for `ShowInExplorer` and `OpenInAxFile`; the player: the engine made on first use with the video area's handle, `PlayCurrent`, the events (`OnStarted`, `OnEnded`, `OnFailed`, `MoveOn`, `StopPlayback`), the transport keys, the speed, Go to time, Ctrl+Q, the clock, `UpdateTitle`, `UpdateStatusBar`, `FitVideo`.
- `AxMediaKeys`: the window's keys (SPEC.md AM-5.2; bundle AX-8), the one place a key is changed; `All` feeds the collision test and the F1 text names the same keys.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's.

## Rules

1. Every control gets an `AccessibleName`; the list is named with its count ("Queue, 12 items"); nothing speaks on a plain arrow key; a command's result is one announcement (AM-6), on the list, or on the notice while one shows. "Playing <name>" when the queue moves on by itself is the one unprompted announcement, and a setting.
2. The player is driven from the UI thread and its events come back on it through the `BeginInvoke` the window hands `MediaPlayer` at creation; the folder reads of a hand-over or an add run on a task.
3. No second window (bundle AX-7.2): questions and errors are notices; the file and folder pickers and File Explorer are the system's own windows; the video area is a panel inside the window.
4. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan.

## Gotchas already learned

- Enter in a `ListBox` reaches `ProcessCmdKey` as `Keys.Return`; handled there with a `_list.Focused` check, and returning true keeps the list from dinging. Delete and Space are the same.
- The video area's handle must exist before the engine is made (`_video.Handle` creates it, hidden or not); the engine needs it at creation and draws nothing in it for a sound file.
- The queue holds paths, not entries, and the window never indexes a list of its own: `PlayQueue.Playing` is the one index.
- A hand-over arrives through `SendMessage` from the starting process, which waits for the answer: the base's `WndProc` sets `m.Result` to 1 and posts the action `TakeHandOver` returns, so that nothing, not even the settings file, is touched before the answer; the folder is read on a task afterwards, and two hand-overs apply in the order they came (`_handOvers`, one queue since 2026-10-23). The starting process grants this one the right to come to the front (`AllowSetForegroundWindow` in `AppMessage.Send`) and the window activates itself once the queue is set, so that NVDA reads the new title and the playing file, not what was there before.
- The selection follows a play the user or a hand-over asked for (the playing file is selected), never the queue moving on by itself: the user may be arrowing through the list. "The file in hand" (`FileInHand`, for Ctrl+W, axFile, the restart and Recently closed) is the one playing or paused, else the one under the selection: after End of queue the queue's index still names the last file played, which is not in hand.
