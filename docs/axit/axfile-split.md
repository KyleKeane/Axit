# The split of AxFile: AxMedia, AxPreview and the Actions list

The inventory of the change decided on 2026-10-22 (the user: "make AxMedia a separate app, have any file opening or
previewing handled by AxPreview, and have AxFile focus on file operations and management only; this should keep these
apps simple to use and well scoped"). Three things happen:

1. **AxMedia**, a player with one queue, becomes an app of its own; AxFile's player (`docs/axfile/SPEC.md` AF-5, AF-9.2)
   moves there whole. Specification `docs/axmedia/SPEC.md`.
2. **AxPreview**, a read-only viewer for one file, becomes an app of its own; AxFile's preview pane (AF-9.4) moves there
   and grows into a window. Specification `docs/axpreview/SPEC.md`.
3. **AxFile** keeps the folder list and the operations, hands files to the two apps (Enter, Play folder, Add to
   queue, Preview), and its pane becomes the **Actions** list, where every operation is a line and Ctrl+Z on a line
   takes that one back. Its planned shape is `docs/axfile/SPEC.md` section 9.

This file lists every place that changes, grouped by what happens to it, and the order of the commits, so that the
split can be made step by step with the tests green after each one and nothing in the running apps broken between
steps (root `CLAUDE.md`, rule 7). Each new app follows `docs/axit/adding-an-app.md`. The user decided the same day
what Enter does in AxFile: a file opens in the program the user has made its default in Windows, as in File
Explorer, and the Axit apps register as programs for their kinds so that they can be chosen there; AxPreview has
Shift+Enter in AxFile as its own key (`docs/axpreview/SPEC.md` AP-D2, `docs/axfile/SPEC.md` AF-9.1).

## 1. Moves from AxFile to AxMedia, unchanged where possible (AX-6.3)

| Today | Tomorrow | Note |
| --- | --- | --- |
| `src/AxFile.Core/MediaPlayer.cs` | `src/AxMedia.Core/MediaPlayer.cs` | The MFPlay seam, unchanged; its gotchas move from `src/AxFile.Core/CLAUDE.md` to `src/AxMedia.Core/CLAUDE.md`. |
| `src/AxFile.Core/PlayQueue.cs` | `src/AxMedia.Core/PlayQueue.cs` | Without the status lines (`Report`, `Status`, the token), which become AxFile's Actions list; the lines are the files alone. |
| `src/AxFile/FileForm.Player.cs` | `src/AxMedia/MediaForm.cs` | The engine, `PlayCurrent`, the events, the transport, the speed, Go to time, Ctrl+Q, the clock, `FitVideo`, `StopPlayback`. |
| The queue part of `src/AxFile/FileForm.Pane.cs` | `src/AxMedia/MediaForm.cs` | `FillPane` becomes the list's fill; `PlayPaneItem`, `RemovePaneItem`, `SayPaneInformation`, `ClearQueue`, `MediaOf`. |
| The Player menu and its keys in `src/AxFile/FileForm.cs`, `AxFileKeys.cs`, `HelpText.cs` | AxMedia's Play menu, `AxMediaKeys`, `HelpText` | Ctrl+P, Ctrl+Left, Ctrl+Right, Ctrl+Shift+Left, Ctrl+Shift+Right, Ctrl+T, Ctrl+Shift+Period, Ctrl+Shift+Comma, Ctrl+Q; Space joins as pause (AM-D7). |
| The video area `_video` and the clock in `src/AxFile/FileForm.cs` | `MediaForm` | |
| `speed`, `seekSeconds`, `sayEachFile` in `src/AxFile.Core/FileSettings.cs` | `src/AxMedia.Core/MediaSettings.cs` | Dropped from `FileSettings`; an old `axfile.json` still loads, unknown keys ignored. |
| `tests/AxFile.Tests/PlayQueueTests.cs` | `tests/AxMedia.Tests/PlayQueueTests.cs` | Minus the status-line tests, which go to `ActionLogTests`. |
| The reason test in `tests/AxFile.Tests/FileOperationsTests.cs` | `tests/AxMedia.Tests/MediaPlayerTests.cs` | `MediaPlayer.Reason`. |
| `docs/axfile/SPEC.md` AF-5, AF-9.2, AF-D2, AF-D3, AF-D4, AF-D6 | `docs/axmedia/SPEC.md` AM-3, AM-4, AM-D1 to AM-D7 | Written; the AxFile sections become one pointer each on the build. |
| `docs/axfile/user-guide.md` The player, The queue | `docs/axmedia/user-guide.md` | AxFile's guide keeps one short "Sound and video" section. |
| `docs/axfile/nvda-test-plan.md` §4 and §6 steps 1 to 4 | `docs/axmedia/nvda-test-plan.md` | AxFile's §4 becomes the hand-over checks. |

## 2. Moves from AxFile to AxPreview

| Today | Tomorrow | Note |
| --- | --- | --- |
| `src/AxFile.Core/Preview.cs` `IsText` | `src/AxPreview.Core/TextSniff.cs` | The rule over the first bytes; `Read` with its cut and `WithoutCutSequence` are not needed (the viewer reads whole files through `TextDocument.Read`). |
| `tests/AxFile.Tests/PreviewTests.cs` | `tests/AxPreview.Tests/TextSniffTests.cs` | The text and binary cases; the cut cases go. |
| The preview part of `src/AxFile/FileForm.Pane.cs` (`_preview`, `ShowPreview`, `OnPreviewKeyDown`, `PreviewLine`, `ShowQueueList`) | `src/AxPreview/PreviewForm.cs` | Grown into a window: the text, `l`, the jumps, find, Go to line, reload. |
| `AxFileKeys.NextHeading` … `PreviousList`, `LineNumber`, `PreviewJumps` | `AxPreviewKeys` | AxDown's full set of letters there (AP-2.3); in AxFile they go. |
| `docs/axfile/SPEC.md` AF-9.4 | `docs/axpreview/SPEC.md` AP-2 | Written. |
| `docs/axfile/user-guide.md` the Preview paragraph; `docs/axfile/nvda-test-plan.md` §6 step 8 | `docs/axpreview/user-guide.md`, `nvda-test-plan.md` | |

## 3. Shared on the second use, moved to the bundle unchanged (AX-6.3, one piece per commit)

| Today | Tomorrow | Why |
| --- | --- | --- |
| `src/AxFile.Core/MediaFiles.cs` | `src/Axit.Core/MediaFiles.cs` | AxFile keeps asking `IsMedia` (Enter, Add to queue, Open in Axit); AxMedia needs all of it. `Queue(entries)` over AxFile's `Entry` leaves it (Axit.Core cannot reference AxFile.Core) and becomes `MediaFolder.Files(folder)` in AxMedia.Core. |
| `src/AxFile.Core/NaturalOrder.cs` | `src/Axit.Core/NaturalOrder.cs` | AxMedia orders a folder's files as AxFile does. |
| `tests/AxFile.Tests/MediaFilesTests.cs` | `tests/Axit.Tests/MediaFilesTests.cs` | With the file; the `Queue(entries)` test becomes `MediaFolderTests`. |
| `Axit.Forms/TextEditorForm`'s line and column arithmetic, reload-on-activation check, Next and Previous submenus | `Axit.Forms` pieces, or a read-only mode of the shell | Decided at the build (AP-D4); nothing copied. |

## 4. New

| Place | Content |
| --- | --- |
| `src/Axit.Forms/AppMessage.cs` | `Send(appName, verb, paths)`: the newest window of that app through the same `EnumWindows` as `AxitMenu` (`Axit.Core/AxitWindows` tells the app from the title), `WM_COPYDATA` with the text, `SetForegroundWindow` for the verbs that take the focus; true when a window took it, so that the starting process can end. `Receive(ref Message)`: the text back, for a window's `WndProc`. The text form (`verb`, then one path per line) is pure and tested in `Axit.Tests` (`AppMessageTests`). Verbs: `play`, `play-folder`, `add` (AxMedia), `open` (AxPreview). Used by `AxMediaApp.Run` and `AxPreviewApp.Run` before they make a window: with paths and an open window of their own, they send and return; `--new` skips it. AxFile and File Explorer only start the app. |
| `src/AxMedia.Core/` | `AxMedia.Core.csproj`, `MediaPlayer.cs` (moved), `PlayQueue.cs` (moved), `MediaFolder.cs`, `MediaSettings.cs`, `CLAUDE.md`. |
| `src/AxMedia/` | `AxMedia.csproj` (embedding the guide), `AxMediaApp.cs`, `MediaForm.cs`, `AxMediaKeys.cs`, `HelpText.cs`, `CLAUDE.md`. |
| `tests/AxMedia.Tests/` | The project, `PlayQueueTests.cs` (moved), `MediaFolderTests.cs`, `MediaSettingsTests.cs`, `MediaPlayerTests.cs`. |
| `docs/axmedia/` | `SPEC.md` (written), `user-guide.md`, `nvda-test-plan.md`. |
| `src/AxPreview.Core/` | `AxPreview.Core.csproj`, `TextSniff.cs` (moved), `PreviewSettings.cs`, `CLAUDE.md`. |
| `src/AxPreview/` | `AxPreview.csproj` (embedding the guide), `AxPreviewApp.cs`, `PreviewForm.cs`, `AxPreviewKeys.cs`, `HelpText.cs`, `CLAUDE.md`. |
| `tests/AxPreview.Tests/` | The project, `TextSniffTests.cs` (moved), `PreviewSettingsTests.cs`. |
| `docs/axpreview/` | `SPEC.md` (written), `user-guide.md`, `nvda-test-plan.md`. |
| `src/AxFile.Core/ActionLog.cs`, `tests/AxFile.Tests/ActionLogTests.cs` | Section 6. |
| `tools/make-icon.ps1` | Two lines, AxMedia's and AxPreview's icons; `publish.ps1` copies them. |
| `run-axmedia.ps1`, `run-axpreview.ps1` | `run.ps1 -App media @args`, `-App preview @args`. |

## 5. Stays in AxFile and changes

| Place | Change |
| --- | --- |
| `src/AxFile.Core/FolderListing.cs` `OpenWay`, `WayOf` | `OpenWay` keeps `Folder` and `Windows` only: Enter on any file is Windows' default program (AF-9.1); `EditorOf(path)` tells Ctrl+E which editor (`.json` AxJSON, else AxDown); `IsMedia` (now `Axit.Core.MediaFiles`) serves Add to queue and Play folder. |
| `src/AxFile/FileForm.Browse.cs` `OpenSelected` | A folder here; a file through `OpenWithWindows` (ShellExecute, the Open with dialog when Windows has no program), as the "anything else" branch does today. `Shift+Enter`: a file to AxPreview (`AxitMenu.Start(Dispatch.AxPreview, path)`), a folder in a new AxFile window. |
| `src/AxFile/FileForm.Pane.cs` | Becomes the Actions list (section 6): `_pane` named "Actions", filled from `ActionLog`; `PlayFolder` and `AddToQueue` start AxMedia on the folder or the marked entries (`--add`), "Added n to AxMedia's queue"; `ShowPreview` starts AxPreview on the entry; `Report` becomes `Begin`/`End`. |
| `src/AxFile/FileForm.cs` | The Player menu goes; under Item: Open (Enter), Open with ▸, Open in Axit ▸ (AxMedia and AxPreview appear by themselves from `Dispatch.Apps`; `OpenInAxit` for AxMedia on a folder plays the folder), Preview Shift+Enter, Edit Ctrl+E (the editor), Play folder in AxMedia Ctrl+Enter, Add to AxMedia's queue Ctrl+Shift+Q, then the operations. The pane keys: Enter re-reads the line, Ctrl+Z on a line undoes it, Ctrl+I its number and time, Escape back; Tab, Ctrl+Tab and F6 as now. `HideForNotice`, `ShowAfterNotice`, `FocusToKeep`, `ApplyFont`, `SizeChanged` lose the preview and the video. |
| `src/AxFile/FileForm.Operations.cs` | `_history` becomes the `ActionLog`; every operation records its line and its `Operation` in one place; Ctrl+Z anywhere undoes the newest undoable action, Ctrl+Z on a line of the Actions list undoes that one. |
| `src/AxFile.Core/OperationHistory.cs` | Replaced by `ActionLog`; `Operation` stays. |
| `src/AxFile.Core/FileOperations.cs`, `Archives.cs` | Unchanged (the progress callback feeds the Actions line). |
| `src/AxFile/AxFileKeys.cs`, `HelpText.cs` | The player's and the preview's keys go; `Preview` becomes Shift+Enter (Ctrl+Shift+P goes); Ctrl+Enter and Ctrl+Shift+Q stay as the AxMedia starts; the F1 sections "The player", "The queue", "The preview" become "Sound, video and viewing" (four lines) and "Actions". |
| `src/AxFile/CLAUDE.md`, `src/AxFile.Core/CLAUDE.md` | The player, the queue and the preview paragraphs go; the Actions list and the hand-overs come. |
| `docs/axfile/SPEC.md` | Section 1 (summary), 2 (G3, G5, non-goals), 3 (scenarios 1, 2), AF-1.3, AF-2 (the video area), AF-3.2 (Enter), AF-4.14 (undo on a line), AF-5 (a pointer to AxMedia), AF-6.1, AF-6.2, AF-7, AF-8.1, AF-9 (from section 9), the architecture block, AF-D2, AF-D3, AF-D4, AF-D6, AF-D9's last sentence, §7, §8. |
| `docs/axfile/user-guide.md`, `docs/axfile/nvda-test-plan.md` | As above; §6 becomes the Actions pane. |

## 6. The Actions list (AxFile's pane after the split; `docs/axfile/SPEC.md` section 9)

- One list under the folder list, "Actions", always there ("No actions yet" until the first operation, since 2026-10-22 late); Tab, Ctrl+Tab and F6
  move between the two, Escape goes back; the newest line is selected when a line is added while the focus is in the
  folder list, so that Tab lands on it.
- Every file operation is one line, in time order, newest last, the last fifty kept, gone with the window: "Renamed
  notes.md to todo.md", "Created temp", "Created New file.txt", "Moved 3 items into bundle", "Copied 3 items", "Moved
  2 items", "Compressed to docs.zip", "Extracted to docs - 2", "Deleted 2 items". A running operation reads what it
  is doing, "Copying 3 items", "Compressing, 40 of 120 files", and the line is replaced when it ends; a failure,
  "Failed: <reason>". A line that was undone reads "…, undone"; one that could not be undone when asked, "…, not
  undone: <reason>".
- Ctrl+Z anywhere in the window undoes the newest line that can be undone (AF-4.14 as built); Ctrl+Z on a line of the
  Actions list undoes that line; Enter re-reads the line; Ctrl+I says its number and time, "3 of 12, 21:04".
- `AxFile.Core/ActionLog` (pure, tested): entries of text, state (Running, Done, Failed, Undone, NotUndone), the
  `Operation` when there is one, a token for a running line, the time; `Begin(text)` → token, `End(token, text,
  operation?)`, `Fail(token, reason)`, `Add(text, operation)` for an operation that ran at once, `Lines()`, `Newest()`
  undoable, `Undone(entry)`, `NotUndone(entry, reason)`; the limit of fifty lines and of twenty undoable operations.
  It replaces `OperationHistory` and the status lines of `PlayQueue`.

## 7. Wiring (adding-an-app.md sections 5 and 6), for each of the two apps

| Place | Change |
| --- | --- |
| `src/Axit.Core/Dispatch.cs` | `AxMedia = "media"`, `AxPreview = "preview"`, the `Decide` branches, `Apps` (after AxFile), `Usage`; AX-1.2: a media file alone goes to AxMedia; `DispatchTests`. |
| `src/Axit/Program.cs` | `case Dispatch.AxMedia: SetAppId("Axit.AxMedia"); AxMediaApp.Run(…)`; the same for AxPreview. |
| `src/Axit/Axit.csproj`, `Axit.sln` | The references and the six projects. |
| `tests/Axit.Tests/KeyTests.cs`, `LayoutTests` | The two key tables and F1 texts; the reference direction holds (neither app references AxFile). |
| `run.ps1` | `media` and `preview` in the `ValidateSet` and their branches. |
| `install.ps1` | Two `$apps` blocks: AxMedia (Verb media, "AxMedia, a player for sound and video files", Opens files, Types the media extensions, OpenWith the same with TypeName "Media file (AxMedia)"); AxPreview (Verb preview, "AxPreview, a read-only viewer for files", Opens files, OpenWith `.txt`, `.md`, `.markdown`, `.log`, `.json` with TypeName "Text file (AxPreview)"). AxFile's description loses "with a player". The registration makes each app a program the user can choose as the default for a kind in Windows' settings; Windows lets no installer set the default itself. |
| `docs/axit/README.md`, the apps' guides | The two apps, their Start entries, commands and Explorer entries; AxFile's line; one paragraph on making an Axit app the default program for a kind (Settings → Apps → Default apps, or Open with → Always), since Enter in AxFile follows it. |
| `docs/axit/SPEC.md` | Section 3 (the two apps; AxFile's entry), AX-1.2, AX-1.6 (the app ids), AX-2.3's table, AX-8.3 (the key tables), the architecture listing. |
| Root `CLAUDE.md` | The map lines for AxMedia and AxPreview; AxFile's line loses "with a player". |
| `docs/axit/plan.md` | The AxMedia paragraph: the reversal of AF-D2 and the date (done). |
| `CHANGELOG.md` | `AxMedia:`, `AxPreview:` and `AxFile:` bullets; the release is a minor version, two new apps. |

## 8. Order of the commits

Each commit builds and passes every test; the apps keep working between commits. The build goes to a scratch folder
while Axit runs (root `CLAUDE.md`). Branch `split`, merged when the NVDA plans have passed.

Built 2026-10-22 in this order, with these departures: the find in a list moved to the window base before AxMedia
(its second use); AxPreview was built before the hand-over of AxFile's player (steps 7 before 6), so that AxFile could
hand over to both apps in one step; `ActionLog` came before that step (step 9 first), and the two hand-overs and the
Actions list were one commit of AxFile; `AppMessage.Send` grants the window the right to come to the front
(`AllowSetForegroundWindow`) and the window activates itself once the paths are applied; AxPreview is the editor
shell's read-only mode (AP-D4) and uses `EditorSettings`, so there is no `PreviewSettings`; Enter on a line of the
Actions list goes where the action left things (the user's ask of 2026-10-22), Ctrl+I reads the number and time.
Step 10's NVDA passes are the user's.

1. The documents: the two specs, this inventory, `docs/axfile/SPEC.md` section 9 and its status line,
   `docs/axit/plan.md`'s note. (Done 2026-10-22.)
2. `MediaFiles` to `Axit.Core` without `Queue(entries)` (AxFile's callers take `IsMedia` over the entries instead),
   with its tests; `NaturalOrder` to `Axit.Core`. One commit each.
3. `Axit.Forms/AppMessage` with the text form's tests.
4. `AxMedia.Core`: the project, `MediaPlayer` and `PlayQueue` copied in (the deletion from `AxFile.Core` comes in
   step 6, so that both apps build throughout), `MediaFolder`, `MediaSettings`, the tests, `CLAUDE.md`.
5. `AxMedia`: the window, the keys, the texts, the guide, the test plan, the wiring, `CLAUDE.md`, the single-window
   start through `AppMessage`. AxMedia is usable alone and from File Explorer after this commit; AxFile still plays
   in its own window.
6. AxFile hands over to AxMedia: Enter on a file becomes Windows' default program for every kind, Play folder and
   Add to queue start AxMedia; the player, the video area, the Player menu, its keys and settings leave AxFile;
   `FileForm.Player.cs`, the copies of `MediaPlayer` and `PlayQueue` in `AxFile.Core` and their tests are deleted;
   the spec, guide, test plan and notes follow.
7. `AxPreview.Core` and `AxPreview`: the projects, `TextSniff` copied in, the window on the shared pieces (AP-D4
   decided here, with any shell piece moved to `Axit.Forms` in its own commit first), the keys, the texts, the guide,
   the test plan, the wiring, `CLAUDE.md`, the single-window start.
8. AxFile hands over to AxPreview: Shift+Enter and Item → Preview start it; the preview leaves AxFile (code, keys,
   texts, docs); `Preview.cs` and its tests are deleted.
9. `ActionLog` in `AxFile.Core` with tests; the pane becomes the Actions list; Ctrl+Z on a line; `OperationHistory`
   and the queue's status lines go; AF-9 rewritten from section 9; the test plan's section 6.
10. Verification: every test project, the `accessibility-review` skill on the three windows, the probes off screen
    only with the user away from the keyboard, `publish.ps1 -NoInstall` and a look into the zip, then the user's
    NVDA passes of `docs/axmedia/nvda-test-plan.md`, `docs/axpreview/nvda-test-plan.md` and AxFile's sections 4 and
    6; the release as a minor version.
