# Axit.Core, the shared pure code

Code every app may use and no window needs (`docs/axit/SPEC.md` AX-6.1). One bullet per piece; where a piece came from
another project on its second use, the bullet says so.

## The bundle

- `Dispatch` (AX-1): which app a command line starts; `Apps`, each app's name, verb, project, `AppGroup`, what it
  `Opens` and whether Show in offers it, in the order every list of apps shows (AX-9.2), the one source of the Axit
  menu, Open in Axit, the Show in submenus and the inventory test; `Of` (an app by its verb), `PathFor` (the path an
  app is started on from a file or folder in hand), `EditorOf`.
- `BundleInfo`: the version, read from the executable.
- `AppPaths` (AX-5.1): settings, logs, updates and the recently closed list under the `Axit` folders.
- `SettingsFile` (AX-5.2, AX-5.3): how every app's settings file is read and written, an unreadable one copied to
  `.bad` once, and the take-over of an old file.
- `WindowSettings` (AX-6.5): the base of every app's settings class, the font, the update check and the window's
  place with their sanitising; `WindowPlacement` is the window's place and size.
- `ConsoleSettings` (AX-6.5, 2026-10-23): on `WindowSettings`, the base of axClaude's and axTerminal's classes: the
  console size, the recent folders (`RememberRecentFolder`), the sounds and the signals, the transcript options, with
  `SanitizeConsole`; the JSON names are the ones the two files had.
- `Log`: one file per app; each app calls `Log.UseApp` first thing in its `Run`.
- `Updates/UpdateCheck` (the latest GitHub release: JSON parsing, version comparison; the repository name lives here)
  and `Updates/Updater` (the download and the hand-over to `install.ps1`, AX-3).
- `Audio/WaveTone`: the chimes, the ticks and the working music as WAV bytes, generated in code; no media files ship.
- `BundleAbout` (AX-9.9): the About sections of the bundle, each a notice's title and text: Version (with the window's
  latest-release state), Licence, Attribution, and the Axit guide, `docs/axit/README.md` embedded by
  `Axit.Core.csproj` as `AxitGuide.md`, read through `EmbeddedText`, which every app's `HelpText.UserGuide` uses for
  its own guide.

## Paths, windows and lists

- `LocalPaths` (AX-9.4, AX-9.5; 2026-10-23): whether a path is on a drive of this computer, where asking whether it
  exists answers at once, and `ExistsQuickly`, which asks of a local path only, since a share can take the network's
  timeout, which no menu may wait for.
- `PathName` (AX-9.5): `Of`, the name of a path for a title, a label or an announcement, the last part or the drive's
  root itself (four copies until 2026-10-23).
- `RecentPaths` (AX-9.5): what every settings class does to its recent-path list, `Remember` (the newest first, once
  whatever the case, ten at most) and `Sanitize` after loading; the list property stays in each class so that its file
  keeps its shape.
- `RecentlyClosed` (AX-9.4): the bundle's list of the last ten files and folders the windows held when they closed or
  swapped them, in `%APPDATA%\Axit\recently-closed.json` through `SettingsFile`: `Remember` from a window, `Load` for
  the menu (entries whose path still exists), `Title` as "notes.md (C:\notes\notes.md) - axDown".
- `AxitWindows` (AX-9): the window lists of the Axit menu from the windows `Axit.Forms.AxitMenu` reads off the system
  in Z order, which is recency: the app from a title's ending (" - axDown"), Open windows without the current window,
  each app's windows with it, and the item labels with the number as the mnemonic and a full stop as a pause
  ("&1. notes.md - axDown"; the apps' recent lists use the same `Label`).
- `MenuText` (AX-6.4, AX-10.2): `Plain`, a menu item's or a button's text as spoken or searched, the mnemonic marker
  and the trailing dots gone; the palette's names, the notices' key line and the Speech menu choices read it.
- `Palette` and `PaletteEntry` (AX-10): the command palette's lines: `Text` (a command's name with its menu path and
  key), `Find` (the matches for a typed text through `TextMatch`), `Spoken` ("Save as, File, Ctrl+Shift+S, 1 of 3")
  and `NoMatch`.
- `TextMatch` (AX-10.3): whether a name holds every word of a typed text, case and accents folded away (`Fold`,
  `Words`), and its `Rank` by the first word (the name starts with it, a word does, anywhere), null when a word is
  missing; from AxStart.Core's `ProgramMatch`, the command palette (2026-10-22).
- `TypeAhead` (docs/axfile/SPEC.md AF-3.6; from AxFile.Core, axProcess, 2026-10-24): where letters typed in a list
  jump, File Explorer's way: the prefix from the item in hand round the end, a run of one letter cycling from the next
  item.
- `NaturalOrder`: `StrCmpLogicalW` of shlwapi as an `IComparer<string>`, File Explorer's order of names (2 before 10);
  from AxFile.Core, axMedia (2026-10-22).
- `Accents` (2026-10-23): the accented letters of Latin-1 and Latin Extended-A taken apart (`Split`), put together
  (`Compose`) and stripped (`Strip`) from tables of their own, because the executable runs with invariant
  globalization, where `string.Normalize` returns its input; `TextMatch.Fold` and axFile's lookalike names use it. The
  test projects run in invariant mode too (`tests/Directory.Build.props`), which is what found both.

## Safety (docs/axit/security-audit-2026-10-24.md)

- `SystemPrograms` (S-1): Windows' own programs by full path, and `OnPath`, the PATH searched in its full folders only; every process an app starts is named through it or by a full path it found itself.
- `AutomationEnvironment` (S-11): a test run's secret and folder, read once in `Main` and taken out of the environment; only a new Axit window of the run gets them (`PassTo`).
- `Redaction` (S-20; from AxClaude.Core on its second use, Copy diagnostics): e-mail addresses, the account's paths, pipe names, ids and the account name replaced by placeholders, for text to be shown or shared.

## Background apps

- `SignInStart` (docs/axstart/SPEC.md AS-1.3; from axStart, axClip, 2026-10-23): a background app's value under the
  user's Run key, "Axit <app>" with `"…\Axit.exe" <verb> --hidden`, written only by the installed program. Named so
  since axProcess has a `StartupEntry` of its own, an entry of the startup list.
- `Hotkey` (docs/axstart/SPEC.md AS-4; from AxStart.Core, axClip, 2026-10-23): a system-wide key as `RegisterHotKey`
  takes it, its text both ways ("Ctrl+Shift+Space", any order and case on reading), `Refusal` (Ctrl, Alt or the
  Windows key, and a key of `Names`), axStart's `Default`; each app names its own default.

## Text and the editors

- `Paragraphs`: where Ctrl+Up and Ctrl+Down go in a text with `\r\n` line ends: the starts of its paragraphs, the
  first line or a line with text after a blank line, for `Axit.Forms.EditParagraphs`.
- `TextLines` (docs/axdown/SPEC.md AD-6.4, AD-3.6; 2026-10-23): the caret's line and column in an edit control's text,
  and the index of a line and column put back, clamped to the line and never between a CR and its LF.
- `MarkdownOutline` (docs/axdown/SPEC.md §9; from AxDown.Core): the elements of a Markdown text for navigation keys,
  outside fenced code blocks: headings with a level filter, links, list items and lists, block quotes and tables,
  each with `Next…` and `Previous…` from a caret position returning an `Element` with `Start` and `Spoken`; block
  kinds by line, links by position.
- `TextDocument` (docs/axdown/SPEC.md AD-3; from AxDown.Core, axJSON): a text file as an editor window holds it, with
  the encoding, the byte order mark and the line ending it came with, so that `ToBytes` writes it back as it was,
  every line ending of the text the file's (a lone LF pasted from a Unix clipboard included); `FromBytes` and
  `ToBytes` are the pure core, `Read` and `Write` add the file and the 32 MB limit.
- `EditorSettings` (AD-8; from AxDown.Core, axJSON): what an editor window remembers between runs (font, word wrap,
  recent files, window, the update check), read and written through `SettingsFile` into the app's own file.
- `EditHistory` and `EditStep` (AD-2.5, 2026-10-21): the editor shell's undo and redo history: `Diff` finds the one
  step between the text before and after a change by the common prefix and suffix, the caret settling an ambiguous
  place ("a" typed next to an "a"); `Record` merges a change into the open run when its key was typed at `RunCaret`
  (more typing, a Backspace at the run's end, a Delete at its caret) and makes any other change (`typedAt` -1: a
  paste, a cut, a command, a reload) a step of its own that closes the run; `Break` is the window's second of silence;
  `Undo` and `Redo` hand the step back for the window to apply through the selection; four million characters of
  deltas at most, the oldest dropped.
- `ItemDocument`, `ItemField`, `FieldTypes` and `Parsed` (docs/axit/item-apps.md IA-4; from AxTask.Core, axPass,
  2026-10-24): the item apps' format, an item as a list of fields of a title, a type and a value kept whole as JSON,
  the types read out and typed (dates as tomorrow or +3).
- `MediaFiles` (docs/axmedia/SPEC.md AM-4; from AxFile.Core, axMedia, 2026-10-22): the rules of sound and video files
  that need no engine, the extension sets with `IsMedia`, the time format and the typed time, the speed steps.

## The console and the transcript

From AxClaude.Core on their second use, axTerminal (2026-10-18; docs/axterminal/SPEC.md §5, AT-D6); the gotchas are in
`src/AxClaude.Core/CLAUDE.md`.

- `Pty/PtyHost`: ConPTY through P/Invoke, the one implementation; `tools/PtyCapture` records through it.
  `Pty/StreamRecorder`: the raw stream to a `.vt` file plus chunk index.
- `Vt/VtParser`: byte stream to print, control, CSI and OSC, written into the `Screen` it is given. `Vt/Wcwidth`.
- `Vt/Screen`: cells, cursor, scroll region; rows carry their transcript `Line`, found again by identity with `RowOf`
  and made ahead of text with `EnsureLine`; `PullRowText` at a frame's end; `DetachRows` commits every row's line and
  blanks the rows; events for the frame end, the bell, the title, the cursor homed, the screen erased or reset.
- `Transcript/Line` and `LineKind`: one logical line with its kind, heading level, join flag and hidden flags; the
  Claude-specific kinds and the echo, replay and repaint flags stay here because the class moved whole.
- `Transcript/LineStore`: the transcript of a console: the lines and what a screen does to them, create, commit,
  remove, the text of a rewritten row; since 2026-10-23 also the UTF-8 decoder with `Feed`, the parser and the screen
  made by `NewScreen`, to which each model attaches its own handlers, `EndFrame` over the abstract `CompleteFrame`,
  `ToggleBookmark`, `VisibleBefore`, `Trim`, `ExchangeCount`, the `Changed` and `Bell` events with `RaiseChanged`;
  each app's model derives from it: axClaude's `SessionModel`, axTerminal's `TerminalModel`.
- `Transcript/TranscriptMirror`: visible lines to edit-control edits, the caret's place afterwards, display line
  numbers, the find search, the reading breaks; `ShowMarkers` off shows heading lines as `Line.BareText`, without the
  `#`, the export unchanged.
- `Transcript/Utterance`: how spoken lines join into one notification.

## Tests

Every piece is tested in `tests/Axit.Tests`, one `<Piece>Tests.cs` file per piece (`VtScreenTests`,
`VtScreenOperationTests` and `ScreenMarkTests` for the screen); the transcript pieces are tested further in
`tests/AxClaude.Tests`, where the recorded fixtures drive them. The same project holds the bundle's own checks of the
sources: `LayoutTests` (the project references point one way, M2), `InventoryTests` (every app in every place it must
be), `KeyTests` (no window key equals a bundle key, AX-8.4), `ListTests` and `MenuTests`.

## Gotchas

- `TextDocument`: the Windows code pages come from `CodePagesEncodingProvider`, registered in its static constructor;
  the system's ANSI page is asked with `GetACP`, since the executable runs with invariant globalization and the
  culture cannot tell; an answer of 65001 falls back to 1252, since the bytes were just found not to be UTF-8; the
  encodings are constructed with their marks so that `GetPreamble` knows them; `GetBytes` never writes a mark,
  `ToBytes` adds it when the file had one.

## Rules

Nothing here references an app (M2); a piece arrives here only when a second app needs it, unchanged where possible
(M3); plain static classes and records, no frameworks (B-10). An app's settings class stays in the app; only the file
handling is shared.
