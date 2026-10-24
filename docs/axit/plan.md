# How axClaude became Axit, and what is still to come

The record of the transition of 2026-10-02, kept short: the shape that was chosen, the decisions, what was built,
and the list that remains. The specifications describe the result (`docs/axit/SPEC.md`, `docs/axclaude/SPEC.md`,
`docs/axdown/SPEC.md`); the commit history holds the steps.

## The shape

One executable, `Axit.exe`, with a verb per app (`claude`, `down`); a path alone picks the app. Each app is a
WinForms class library with one entry point, with its pure code in a `.Core` project and its documents in
`docs/<app>/`. Shared code lives in `Axit.Core` (pure) and `Axit.Forms` (WinForms) and moves there on its second
use, unchanged. One installer, one updater, one version, one changelog, one release workflow. A new app follows
`docs/axit/adding-an-app.md`.

## Decisions taken

1. The GitHub repository was renamed `KyleKeane/Axit` with 2.0.0; the old address redirects.
2. `axit` with no argument starts axClaude on the current folder.
3. axClaude 1.7.0 was released first as a bridge: its updater accepts a download whose program file is not
   `AxClaude.exe`, so installed copies could update into Axit.
4. axDown registers under Open with for `.md`, `.markdown` and `.txt` and appears on every file's right-click menu;
   Windows decides defaults.
5. axDown's navigation keys are NVDA's browse-mode letters with Ctrl (next) and Ctrl+Shift (previous), headings
   first (axDown SPEC AD-D7).
6. The program file is `Axit.exe`; the Start menu entries are `Axit axClaude` and `Axit axDown`.
7. Keys live in tables, bundle-wide (`BundleKeys`) and per window (`AxClaudeKeys`, `AxDownKeys`), with a collision
   test (bundle SPEC AX-8); the notices and the question area are shared, not copied (AX-6.4).
8. Plain code only (B-10), zero third-party dependencies, and the working app never broken: the transition ran on
   a branch, one step per commit, every commit building and passing the tests, merged after NVDA checks.

## What was built, in order (all on 2026-10-02)

The bundle spec and the document split; the 1.7.0 bridge; `Axit.exe` with axClaude inside and the tested dispatcher;
the shared pure code in `Axit.Core`; the installer, publish and release scripts for the bundle; the axDown
specification; `AxDown.Core`; the axDown window and the shared window pieces in `Axit.Forms`; axDown's installer
entries and icon; the user's NVDA checks and the 2.0.0 release; axClaude's key table (2.1.0); heading navigation in
axDown (2.1.0); the adding-an-app checklist. After 2.1.0: axDown's window spoken on opening (2.1.1), the update notice
with its four buttons and the progress bar (2.2.0), the in-place install with one bar for download and install and
the restart by the app itself (2.3.0).

## Still to come

- axClaude's open items are in `docs/axclaude/SPEC.md` §12 To do, the one list for them.
- The text editor window is shared since axJSON (2026-10-18: `Axit.Forms/TextEditorForm` with `EditorApp`,
  `EditorKeys`, `EditorHost`; axDown and axJSON are records over it). The window base `Axit.Forms/AxitForm` (AX-6.5)
  holds the notice flow, the fonts, the window's place, find and the update flow since the same day; the editor
  shell, axGIT's window and axClaude's `MainForm` all derive from it; the recent lists (`RecentMenu`), the Options
  and Help menus and the File Explorer opener followed the same day. Left: `ControlArea`'s use in axClaude only; the
  "Show current … in" submenus stay in each window, three lines each. axClaude's window is partial files, one per
  section, since the same day.
- axJSON, an editor for JSON files (`docs/axjson/SPEC.md`): raw mode built 2026-10-18 as the editor shell plus JSON's
  navigation keys and Format (AJ-D4, B-14: the shell shared, one app per file type, one file per window, the Axit menu
  for the open windows). Still to come: structured mode, a form of real input fields from templates (AJ-3), and the
  caret's path in the status bar (AJ-2.4).
- axDown and axJSON, a file changed outside the editor (asked and built 2026-10-18, AD-3.6): compared when the window
  becomes active again; reloaded with the caret kept when nothing is unsaved, said once otherwise, and Save offers
  Reload and lose mine. No watcher thread, no merging. A watcher for live reload while the window stays active is
  the second step, only if the activation check turns out not enough.

- **AI help as one shared, optional service; local first** (the user's direction, 2026-10-18; bundle G6, B-15).
  AI-based editing and feedback tools are not axDown's own: they are one service every app can call from inside its
  window to get things done there, written once and optional, so that each app has its full utility without it and
  offline. The bundle is to have a shared database as well, which the service and the apps share. Its specification,
  not yet written, has to settle what the database holds and where it lives, how the service reaches Claude (Claude
  Code as axClaude runs it, or the API), what the service may change in a window and how the user sees and undoes
  it, and the one switch that turns every network use off.
- **A command palette in every window** (the user's ask, 2026-10-21; specified 2026-10-22 as bundle SPEC AX-10,
  Ctrl+Shift+P at the user's choice, built on the branch `palette`): one key opens a field with a list under it;
  typing narrows the list to the window's menu commands by name, each with its key named as a reminder, and in axFile
  to the folder's entries as well; Enter runs the command or opens the entry. It is the window base's, fed by each
  window's menus, with axStart's matching shared; next to the AI service above, the second thing every app gets for
  free.
- **A gutter for the transcript views** (the user's idea, 2026-10-19, agreed as a later bundle feature): two narrow
  columns to the left of a transcript, the line number and the jump letter of the line's kind (`i`, `o`, `h2`), for
  sighted helpers, drawn next to the view and never spoken, since in an edit control what is in the text is what NVDA
  reads; and, as two options off by default, the same two as text in front of each line, which NVDA then reads on
  every arrow. What is spoken stays on demand: `l` for the line number, the jump keys with their announcements, and
  Options → Text → Markers in the text (axClaude FR-4.6a, built 2026-10-19) for the `#` markers. The spoken-but-
  invisible variant would need a rich edit control and is not planned.
- **User-defined shells and programs, one shape** (2026-10-21). axFile's Open with a custom program ▸ (axFile
  AF-4.11) is the user's own list of programs and scripts: a list in the app's settings with a name and a path each,
  Add through the Windows file picker and a notice for the name, Remove from a submenu, the entries handed to the
  program as its arguments. axTerminal's user-defined shells, still to come, take the same shape in its own settings:
  a name and a command line each, next to the one custom command line it has now.
- **Custom keys to scripts** (the user's thought, 2026-10-21, kept for later rather than built): a key the user binds
  to one of their own programs or scripts, so that one shortcut runs it on the entry or the folder in hand, with the
  key table and the collision test knowing the binding. The custom programs list above is what such keys would point
  at; what is open is where a user's bindings live (the settings file, a submenu) and how they show in F1.

- **The review of 2026-10-23 and the years ahead.** `review-2026-10-23.md` is the critical review of the five days
  from 2026-10-18 (the verdict, the inventory, what was fixed that day, what is verified and open, the rewrites in
  order: one console window for axClaude and axTerminal, axClaude's question state as one record, the notice core of
  the window base, axFile's operation runner and marks, axTerminal's prompt rows by line identity, then the smaller
  ones). `longevity.md` is the plan for keeping the repository a stable resource for decades: what already holds,
  what was adopted that day, the proposals with their cost, what breaks when, and the calendar (every release, every
  month, every October, every LTS of .NET). The same day, in an autonomous turn: every rewrite, fix and test the
  review asked for (`review-2026-10-23.md` section 10, each item marked) and every longevity item that needed no
  account (`longevity.md` section 3, each marked), in twenty commits on `palette`. Open: the NVDA checks of all of it
  (review section 9), the `ApplyChrome` and mirror restructure (review 5.6), and the longevity items for the
  maintainer (3.2 the organization, 3.13 the archive branch, 3.14 the yearly pass).

- **Axit's own folder and file picker** (asked 2026-10-23; kept for later with the later apps, its priority to be chosen; spec first): in place of the Windows Open, Save as and folder pickers, an owned modal window in front of the window it serves (as the Windows picker stays), with a menu bar for the extras (favourites, sort, hidden files, "Use the Windows picker instead"). The view is axFile's folder list with its keys (arrows, Enter, Backspace, Left and Right, type-ahead; Home and End stay the first and last entry); Ctrl+Left goes to This PC, in axFile too; Ctrl+G opens the Go to field with the path completion, which also completes favourites and the known folders by name (places by name without crowding the list). The Save version adds a name field; every version has buttons for its action and Cancel. It takes axFile's list, `FolderListing` and the path completion into the shared pieces. The user weighed the Windows dialogs' familiarity against Axit's consistency; the choice was Axit's own picker with a way back to Windows'.

## Later apps (recorded 2026-10-18)

Each would be one more app of the bundle, a window per thing worked on, added the way `adding-an-app.md` says, with no
library dependency (B-10: `git.exe` and the file system through the APIs already used, as Claude is `claude.exe`).

- **axGIT**: built the same day (`docs/axgit/SPEC.md`), released in 2.12.0 and passed under NVDA on 2026-10-22; the
  notes below were its plan. A git manager for a repository. One window per repository, named after it, opened from the other windows'
  "Show current folder in ▸" and "Show current file in ▸" submenus (the slot the specs already keep for an Axit app
  next to File Explorer) and from the Axit menu, so that it stays tied to the folder or file the user came from and is
  found again in Open windows (AX-9) rather than lost. Inside the window the bundle's own pieces: lists (status as
  the changed files, log, branches), read-only text with the jump keys (a diff, a commit), one field (the commit
  message), notices for questions. A streamlined set to start with: status, diff of a file, stage and unstage,
  commit, log, branch and switch, fetch, pull and push; conflicts and stashes shown and left to the terminal or to
  Claude at first. Everything through `git` commands with their text parsed, nothing reimplemented. In the same
  window as axClaude or the editors it would be a Git menu three times over and a second set of lists in a window
  built around one thing; and in axClaude much of it can already be asked of Claude, so axGIT's worth is the quick,
  deterministic read of a repository's state without a turn.
- **axTerminal** (asked 2026-10-18; specified and built the same day, `docs/axterminal/SPEC.md`, released in 3.0.0
  and passed under NVDA on 2026-10-22): axClaude's window around a
  shell. PowerShell (or Windows PowerShell, or cmd) in a ConPTY; every row a line of the transcript; a command typed
  in the field gets axClaude's exchange block; the shell's prompt rows hidden and shown in the status bar; the shell's
  integration marks (OSC 133, the Windows Terminal convention, injected by the app for the session) say when a command
  is done and with what code, spoken with the chimes. Not a terminal emulator: programs that take the screen over are
  out of scope. It is also the infrastructure step the user asked for first: the ConPTY host, the parser, the screen,
  the mirror and the view move from axClaude into the shared projects on this second use, one piece per commit, with
  one seam (`LineStore`) between the screen and Claude's session model.
- **axFile** (the plan's file manager, named 2026-10-18; specified the same day, `docs/axfile/SPEC.md`): a list of
  one folder's entries, folders first, Enter the one verb (a folder into the list, a text or Markdown file into axDown,
  JSON into axJSON, a media file played, anything else to Windows' program), Backspace up, the four operations (new
  folder, rename, delete to the Recycle Bin, copy the path) and File Explorer for the rest. **axMedia**, asked the
  same day as "a simple file browser that plays everything in a folder", was this window's player, not an app
  (AF-D2), until 2026-10-22, when the user split it out as an app of its own with one queue, together with
  **axPreview**, a read-only viewer for one file, so that axFile is file operations and management only; the
  specifications are `docs/axmedia/SPEC.md` and `docs/axpreview/SPEC.md`, the inventory of the split
  `docs/axit/axfile-split.md`, and axFile's pane becomes its Actions list (`docs/axfile/SPEC.md` AF-9); all three
  built the same day on the branch `split`, released in 3.3.1 and passed under NVDA the same day. As first built, the player was:
  Enter on a sound or video file plays it and the folder's media files follow in order; pause, seek, next, previous,
  speed, go to time, what is playing on Ctrl+Q; Windows' own decoders through one P/Invoke seam (MCI or MFPlay, chosen
  by the first build), the keyboard's own volume keys. The worry on record stands: the scope stays small; copy and
  paste wait for the ask.
- **axStart** (asked, specified and built 2026-10-22, `docs/axstart/SPEC.md`; released in 3.4.0 before its NVDA pass,
  which is still owed): a program launcher behind a global hotkey, the one app of the bundle with no document: a
  hidden process from sign-in whose hotkey brings up one search field over the Start menu's programs, Down and Up
  through the matches with the caret kept in the field, Enter to start.
- **axPass** (asked 2026-10-18 as AxKeePassium, after the KeePass app on the phone; renamed axPass by the user on
  2026-10-23, which keeps clear of anyone's product name; no spec yet; the format and the command-line tool, read from
  their sources, are in `docs/axpass/kdbx-notes.md`): a KeePass client for one database (`.kdbx`) per window: the
  entries as a list, by group and by search, Enter for an entry's fields as read-only text with the password withheld,
  keys that copy the user name and the password to the clipboard with a timed clear, add and edit through notices, a
  generator, lock. The engine, by the bundle's precedent (AG-D2, B-10): KeePassXC's `keepassxc-cli` as a process per
  command (`ls`, `show`, `clip`, `add`, `edit`, `rm`, `mv`, `generate`, `db-create`), the master password handed to it on
  standard input and held only in the window's memory until lock; the format, Argon2 and the rest stay KeePassXC's and
  are never reimplemented. To verify before the spec: that the Windows build reads the password from a pipe (its
  manual does not say; see the notes), its output formats. Local first (B-15): the database is a file, synced by whatever syncs the user's
  files.
- **AxCryptomator** (asked 2026-10-18; no spec yet; Cryptomator is a product name too, AxVault the alternative): a
  window to unlock and lock Cryptomator vaults, since the desktop app's JavaFX window reads poorly with NVDA: a list
  of known vaults, unlock with the password asked in a notice, the unlocked vault as a drive or folder with "Open in
  axFile" on it, lock. The engine: the project's own `cryptomator-cli` as a process per vault, kept running while the
  vault is unlocked and ended to lock it; it takes the password on standard input or from a file and mounts the vault
  (WinFsp, which the desktop installer brings) or serves it on a local address. The vault format (scrypt, AES-SIV, the
  rest) is never reimplemented. To verify before the spec: the current release's Windows support and mount options,
  and how it lives beside a running desktop app. Local first (B-15): the vault's cloud copy is the user's sync client's
  business.
- **AxList and AxTask** (asked 2026-10-23; placeholder names, no spec; the user may or may not pursue them). AxList: a
  list manager for things to remember, with bullet, numbered and checkbox lists; later, links between items for
  mind maps, curriculum planning, recall in plain language and structured storage. AxTask: the checkbox lists as a
  task manager, with reminders, automations and smart lists (saved filters). Perhaps one app at first, split when the
  task side grows. Other names considered: AxNotes, AxTodo. The rule the user set for both: plain text files on disk
  (Markdown lists, one file per list) with a clear, documented format, so that an AI system or a script can read and
  change them as easily as the app does; no database the user cannot open in axDown.
- **An anywhere, anytime helper** (asked 2026-10-23, high priority for the user; placeholder name AxAsk): one key
  from anywhere, as axStart's, opens a field for a question about the user's own files: an index of the folders the
  user allows, kept as plain files, and answers drawn from it (retrieval over the index, RAG) through the shared AI
  service. The foundation comes first: the list of allowed folders, the index format, and the service's one switch
  for network use (B-15).
- **Screen capture and description** (asked 2026-10-23; placeholder AxSee): a key that takes a screenshot and sends it
  to a vision API for a description and a chat about it, through the shared AI service. Later, on that foundation:
  a parser that segments the screen into objects, and actions on them (move the mouse to an object, click it), an
  agent the user steers by speech and keys. The user's thought: these may be add-ons run from the launcher (AxRun,
  the user's word for axStart running scripts as well as programs) rather than apps, one simple place where all of
  them are on hand, each a script with a clear interface.
- **Automated quality checks** (asked 2026-10-23): runs that drive the apps the way a user does and check the
  result, so that a robustness question (several axClaude sessions at once, a model change, a message each) is a
  command rather than a manual pass. The first is `tools/QaRun`, which runs Claude sessions through the app's own
  console pieces without windows; driving real windows waits for a way that never takes the foreground.
- **Other everyday utilities, on record only** (2026-10-23): **AxWeb**, a simple browser for reading the web as a
  transcript of headings, links and text (the hard part is the engine: WebView2 is a web view, which B-10 rules out,
  so a reader over fetched HTML is the likely shape); **AxMail**, a mail reader and writer over IMAP and SMTP;
  **AxCalendar**, dates and reminders (shared with AxTask's reminders); **AxCalc**, a calculator as a field with a
  history; **AxClip**, a clipboard history; **AxSettings**, a plain list of the Windows settings a user changes most.
  Each would keep the same rule: local first (B-15), plain files, clear formats an AI can work with.
- **Apps suggested on 2026-10-23, recorded at the user's ask** (no specs; ranked by daily use and fit with the
  bundle's rules). The two picked to start with, if any: axClip, then axCalc or axHTTP as a small app that follows
  `adding-an-app.md`.
  1. **axClip** (above), in detail: axStart's hotkey and search list over what was copied; typing filters, Enter
     pastes, a key reads an item in full. The Windows clipboard history (Windows+V) reads poorly with NVDA.
  2. **axOCR**, text from a screenshot, an image file or the window in front, through Windows' offline OCR engine
     (`Windows.Media.Ocr`), the text opened in axPreview: images, scanned PDFs, dialogs that read badly, screenshots
     people send. To agree first: the engine needs the Windows SDK target in the project file (a Microsoft reference
     pack, not a NuGet package) against the no-dependencies rule. The local counterpart of AxSee above, which
     describes a screen through the AI service.
  3. **axHTTP**, a REST client: requests with a history, the reply as a transcript, a JSON body opened in axJSON.
     Postman and the like are web views. .NET's own HTTP client; the second app with network use after axClaude, only
     on the user's command.
  4. **axSQL**, a SQLite browser: the tables, a query field, the result as a list read row by row with a key for the
     whole row. Windows ships SQLite (`winsqlite3.dll`), so no library; the shared database planned above could use it.
  5. **axProcess** (built 2026-10-23 on the branch `axprocess`, `docs/axprocess/SPEC.md`, NVDA pass owed), processes, services and startup items (named so, since AxTask is the to-do manager): a filtered
     list with memory and CPU, keys to end, restart and show the program file in axFile. Task Manager's lists read
     only in part. Windows APIs only.
  6. **axCalc** (above), in detail: a field for an expression, each answer a line of a transcript to arrow through
     and copy, hex, binary and units for development. The smallest of these.
  7. **axMail** (above): the largest daily need and the largest build (IMAP, SMTP, OAuth sign-in for Gmail and
     Outlook), so it waits until the small apps have proved their patterns.
  Not planned: a full browser (a web view, B-10; AxWeb above is a reader) and a PDF reader (Windows has no built-in
  way to pull text from a PDF; axOCR covers scanned pages).
- The order the user set: the spec, then the infrastructure, then the build; axTerminal first, since its
  infrastructure is the moves; axFile next; the two engines verified before their specs are written.

- Path completion (2026-10-23): axFile's Go to folder and Move to folder share one completion (`AxFile.Core.PathCompletion` and the field's key hook in `FileForm.Browse`). The user asked for it as a shared resource for every path field of the bundle; it moves to `Axit.Core` and `Axit.Forms` with the first other app that has a path field (M3), untangled from `FolderListing` then.
