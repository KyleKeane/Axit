# Axit — Bundle Specification

Describes Axit as of 2.0.0 (2026-10-02), the bundle the AxClaude repository became (plan: `docs/axit/plan.md`). Every
requirement is built unless its heading says otherwise; this document is kept in step with the code (a behaviour
change updates it in the same commit). It covers only what crosses apps. Each app has its own specification:
`docs/axclaude/SPEC.md`, `docs/axdown/SPEC.md`. Requirements here are numbered `AX-n.m` and decisions `B-n`, so that
they never clash with an app's `FR-n.m` and `D-n`.

## 1. Summary

Axit is one installed Windows program, `Axit.exe`, that holds several accessible apps for a blind developer who uses
the NVDA screen reader. Each app is a separate window that starts on its own, has its own Start menu entry, its own
right-click entry in File Explorer and its own console command, and all of them share one installer, one updater, one
version number and one release workflow. The first apps are **AxClaude**, the front end for the Claude Code CLI that
exists today, and **AxDown**, a barebones editor for Markdown and text files. Later apps are planned (a collaborative
editor, an AI-assisted editor) and follow the same pattern without changes to the bundle.

## 2. Goals and non-goals

- **G1 One download, one install, one update.** The bundle is a single self-contained executable with the .NET
  runtime inside it, so the zip stays one file of about 70 MB whatever the number of apps.
- **G2 Apps that start apart.** Windows+`axc` and Windows+`axd` each find one app; a folder opens in AxClaude, a file
  in AxDown; each app has its own command word. Nothing about the bundle is in the user's way.
- **G3 Modular code and documents.** A change to one app needs only that app's folders, spec and notes in context.
  This is what makes the repository workable for smaller AI models and cheap to extend.
- **G4 Plain code.** Zero third-party runtime dependencies, as AxClaude always had, and no coding paradigm that a
  smaller model would struggle to edit: plain classes, static methods, one obvious way to do each thing.
- **G5 Nothing breaks.** AxClaude behaves as released; every commit builds and passes the tests, and a change to a window is checked with NVDA before it is released.

Non-goals: a plugin system or loading apps from separate files (apps are compiled in); more than one window per
process; an installer that needs administrator rights; setting Windows default programs silently (Windows does not
allow it, see B-9).

## 3. The apps

- **AxClaude** (built): `Axit.exe claude [<folder>] [options]`, Start entry `Axit AxClaude`, `Open in AxClaude` on
  folders, command `axclaude`. Specification `docs/axclaude/SPEC.md`.
- **AxDown**: `Axit.exe down [<file>]`, Start entry `Axit AxDown`,
  `Open in AxDown` on files and Open with for `.md`, `.markdown`, `.txt`, command `axdown`. Specification
  `docs/axdown/SPEC.md`, guide `docs/axdown/user-guide.md`, test plan `docs/axdown/nvda-test-plan.md`.
- **AxJSON**: `Axit.exe json [<file>]`, Start entry `Axit AxJSON`, Open with for `.json`, command `axjson`. The
  text editor shell AxDown is, with JSON's navigation keys and a Format command. Specification `docs/axjson/SPEC.md`,
  guide `docs/axjson/user-guide.md`, test plan `docs/axjson/nvda-test-plan.md`.

## 4. Functional requirements

### AX-1 Launch and dispatch

- AX-1.1 The first argument may be a verb naming an app: `claude`, `down`, `json`. The rest of the arguments go to that app
  unchanged, so `Axit.exe claude C:\src --record x.vt` is what `AxClaude.exe C:\src --record x.vt` was.
- AX-1.2 Without a verb, a first argument that is an existing folder goes to AxClaude and an existing file to AxDown,
  or to AxJSON when its extension is `.json`, with the remaining arguments. A path that does not exist is reported with the usage text (a message box, since no
  window exists yet, as `--help` does today).
- AX-1.3 Without any argument, `Axit.exe` starts AxClaude on the current folder, which is what `axclaude` does today
  (B-8). AxClaude keeps its own rule for a current folder that is the program's own folder.
- AX-1.4 `--help` and `--version` answer for the bundle and list the verbs; `Axit.exe claude --help` is the app's own
  usage text.
- AX-1.5 The dispatch is pure code in `Axit.Core` (`Dispatch`): arguments in, app name and arguments out. It is unit
  tested and has no knowledge of the apps beyond their names.
- AX-1.6 Each process sets a Windows application id before its window opens (`Axit.AxClaude`, `Axit.AxDown`), so the
  taskbar shows the apps apart and Win+number keys reach the right one.
- AX-1.7 An app is a class library with one entry point, `static void Run(string[] args)` on a class named
  `<App>App`, plus a `Usage` text. `Axit`'s `Program` owns the crash handling, the version and the log start, and calls
  that entry point; the app owns everything after.

### AX-2 Installation

- AX-2.1 `install.ps1` installs for the current user into `%LOCALAPPDATA%\Programs\Axit` with no administrator
  rights, from the extracted zip or from `publish\win-x64`; `install.cmd` runs it with the execution policy bypassed
  and, downloaded alone, fetches the latest release first. `-Uninstall` removes everything of Axit and keeps the
  settings and the logs. This is AxClaude's installer today (`docs/axclaude/SPEC.md` FR-8.7, D18) with the names changed.
- AX-2.2 Start menu: one shortcut per app, named `Axit <App>` (`Axit AxClaude`, `Axit AxDown`, `Axit AxJSON`), each
  starting `Axit.exe <verb>` with the app's icon. Windows search matches the start of each word, so `axc`, `axd` and
  `axj` find them and `axit` lists them (checked).
- AX-2.3 File Explorer: `Open in AxClaude` on folders and on a folder's background (`Axit.exe claude "%1"`, `%V` for
  the background); `Open in AxDown` on every file (`Axit.exe down "%1"`); AxDown registered as a program for `.md`,
  `.markdown` and `.txt`, and AxJSON for `.json`, so they appear under Open with and can be chosen as the default in
  Settings, Default apps (B-9).
- AX-2.4 Console commands: `axit.cmd`, `axclaude.cmd`, `axdown.cmd` and `axjson.cmd` in `%USERPROFILE%\.local\bin`, one
  line each, passing their arguments to `Axit.exe`, `Axit.exe claude`, `Axit.exe down` and `Axit.exe json`. They work
  from cmd and from PowerShell.
- AX-2.5 Settings, Apps lists one entry, `Axit`, with the version, the publisher and an Uninstall that runs
  `install.cmd -Uninstall`.
- AX-2.6 Installing over an existing AxClaude installation (any version) removes it completely first: its folder,
  Start entry, `axclaude.cmd`, registry keys and Apps entry. The installer says what it found. The AxClaude settings
  file is left where it is; the app copies it on its first start (AX-5.2).
- AX-2.7 The hand-over parameters stay: `install.ps1 -WaitForProcess <pid> -Start <folder> [-ContinueConversation]
  -LogFile <file>` installs after that process ends and starts AxClaude on the folder, as the 1.7.0 updater expects.
  `-StartFile <file>` starts AxDown on the file the same way; nothing is renamed. Copies up to 2.2.0 update
  this way; from 2.3.0 the apps use AX-2.8.
- AX-2.8 In place: `install.ps1 -InPlace -LogFile <file>` installs while the app keeps running. Nothing is removed;
  the new files are written over the old ones; the running `Axit.exe` is first renamed `Axit.old.exe` (a running
  program file can be renamed but not overwritten or deleted), and `Axit.exe` deletes any `Axit.old*.exe` next to it
  when it starts. The shortcuts, menus, shims and the Apps entry are written again, so the version shown in Settings,
  Apps is the new one. The app, not the installer, starts the new version afterwards (AX-3.2).

### AX-3 Updates

- AX-3.1 One version for the bundle, `<Version>` in `src/Axit/Axit.csproj`. A newer GitHub release is offered as
  AxClaude does (`docs/axclaude/SPEC.md` FR-1.10, D25): asked once at startup when the setting is on, a notice with
  the release notes and four buttons: Update and restart (Enter), Update and close, Open release page, Later
  (Escape). Every window's Axit menu ends with Check for updates (Axit <version>), always, naming the version in use
  and asking GitHub again every time, Update to Axit <version> below it while a newer release is known, and About, the
  bundle's About in sections (AX-9.9, `Axit.Core/BundleAbout`), the same in every window. Every text of the update
  flow names Axit, never an app: the version is the bundle's.
- AX-3.2 Either update runs behind one notice with one progress bar (`Notice.Progress`; NVDA reports a progress bar
  by its own settings, so nothing is spoken about the progress itself): the zip is downloaded into
  `%LOCALAPPDATA%\Axit\updates\<version>` (the bar to 80 percent; Cancel keeps the installed version), then the
  downloaded version's `install.ps1 -InPlace` runs while the app keeps running (AX-2.8; "Downloaded. Installing";
  the installer's lines move the bar to 100; Cancel no longer applies and says so), then the window closes
  ("Installed. Restarting" or "Installed. Closing") and, after Update and restart, starts the installed program file
  itself with what it had open (AxClaude: the folder, and `-- --continue` when a message was sent; AxDown: the file,
  or nothing). A failed installer leaves the running version in place and shows its output in a notice. A download
  counts as complete when it holds `install.ps1` and a program file. Cancel counts until the download is unpacked, a
  download that is not a zip is a failed update like any other, and an installed program file that cannot be
  started as the window closes is logged (`StartInstalled`), the window closing all the same. Each app's close
  question after Update and restart (AxClaude's Keep working, AxDown's Cancel in the save question) keeps the window
  on the old program file and says "Update installed. It starts with the next AxClaude" (or AxDown).
- AX-3.3 The 1.7.0 bridge (B-6): AxClaude 1.7.0 accepts a download whose program file is not `AxClaude.exe`, so an
  installed AxClaude updates into Axit through Help, Update now. The first Axit release is 2.0.0.
- AX-3.4 The update check and the version comparison are pure code in `Axit.Core/Updates/UpdateCheck`, unit tested;
  the download and the hand-over are `Axit.Core/Updates/Updater`.

### AX-4 Releases

- AX-4.1 `release.ps1 <version>` checks that `CHANGELOG.md` has a `## <version>` section, fetches origin and stops
  when `origin/main` has commits that `main` does not (nothing is released over work pushed from elsewhere; skipped
  with `-NoPush`, which pushes nothing and may run offline), runs
  the tests, sets `<Version>`, commits `Release <version>`, tags `v<version>` and pushes. `.github/workflows/release.yml` checks the
  tag against the version, tests, runs `publish.ps1 -NoInstall` and creates the GitHub release `Axit <version>` with
  `Axit-<version>-win-x64.zip` and `install.cmd` attached and the CHANGELOG section as notes.
- AX-4.2 `publish.ps1` publishes `src/Axit` self-contained, single file, into `publish\win-x64` with `install.cmd`,
  `install.ps1`, `LICENSE` and `docs/axit/README.md` as `README.md`, and zips it.
- AX-4.3 One `CHANGELOG.md`; every bullet starts with the app it concerns (`AxClaude:`, `AxDown:`, `Axit:` for the
  bundle itself).
- AX-4.4 `build.yml` runs the tests of the whole solution on every push and pull request.

### AX-5 Settings, logs and paths

- AX-5.1 Each app has its own settings file, `%APPDATA%\Axit\<app>.json` (`axclaude.json`, `axdown.json`), and its own
  log, `%LOCALAPPDATA%\Axit\logs\<app>.log`. The updater's log is `%LOCALAPPDATA%\Axit\logs\update.log`. The bundle
  has one file of its own, `%APPDATA%\Axit\recently-closed.json`, the Recently closed list of the Axit menu (AX-9.4).
- AX-5.2 On its first start under Axit, AxClaude copies `%APPDATA%\AxClaude\settings.json` to `axclaude.json` when the
  new file does not exist; the old file is left alone.
- AX-5.3 The load-and-save pattern of a settings file (JSON, a missing or unreadable file falls back to the defaults
  and the error is reported once) is one piece of shared code, `Axit.Core/SettingsFile`; each app's settings class is
  its own plain record of properties. A file that is not valid JSON (a typo after Options → Developer → Open settings file) is
  copied to `<app>.json.bad` before the defaults are used, and the error names the copy: the app saves its settings
  soon after loading, which would otherwise write the defaults over every other setting in the file.

### AX-6 Shared pieces

- AX-6.1 `Axit.Core` (pure, unit tested): `Dispatch` (with `Apps`, the name and verb of each app), `BundleInfo`, `AppPaths`, `SettingsFile`,
  `Log` (per-app file name), `Updates/` (`UpdateCheck`, the release check, and `Updater`, the download and the
  hand-over to `install.ps1`; neither needs a window), `Audio/WaveTone` (the sounds as WAV bytes), `MarkdownOutline` (the headings, links, lists, block quotes and tables of a Markdown text, with next and previous from a caret position; AxDown SPEC §9, shared since its second use), `AxitWindows` (the window lists of the Axit menu from the windows the system names, AX-9), `RecentlyClosed` (the bundle's list of closed files and folders, AX-9.4), `BundleAbout` (the About sections and the embedded Axit guide, AX-9.9).
- AX-6.2 `Axit.Forms` (WinForms, no tests, checked with NVDA): `OverlayPanel` (the notices in the
  window, with the `Notice` record callers pass), `ControlArea` and `ListKeys` (a question's answers as a list or a
  field in the message field's place, with the number and page keys of every such list), `Sounds` (wave-out devices kept
  open for the life of the process), `EditPaging` (Page Up and Page Down in an edit control), `EditParagraphs`
  (Ctrl+Up and Ctrl+Down by paragraph, with Shift selecting), `EditKeys` (the KeyDown handler that gives an edit
  control both), `Announcer` (the UI Automation notification, AX-7.1), `TextJump` and `TextJumps` (a window's key pairs for the kinds of element in a text and the jump itself on an edit control, used by every notice text and by AxDown's editor, AX-7.2), `StatusLayout`, the font
  handling (text size and the font picker), `BundleKeys` (AX-8), `AxitMenu` (the Axit menu every window has, AX-9),
  and the text editor shell: `TextEditorForm` (the window every text editor app is: the file, the menus, find, go to
  line, the status bar, the notices, the fonts, the update flow; AxDown's `EditorForm` until AxJSON, the second
  editor), `EditorApp` (what differs between the editor apps, as data: name, verb, settings file, file types, F1 text,
  guide, the `TextJump` pairs of its outline, its resources pages and its commands on the text), `EditorKeys` (the
  shell's keys, the same in every editor window) and `EditorHost` (the entry point every editor app's `Run` is one
  call into: log, crash handling, command line, settings, the window). AxClaude's `MainForm` keeps its own notice and
  update flow; a window base for both kinds is the remaining duplication (plan).
- AX-6.3 A piece moves into a shared project only when a second app needs it, in a commit of its own, with the
  accessibility review; it moves unchanged where possible (B-3).
- AX-6.4 The notices and the question area are the bundle's one way of asking the user something, so that every
  window asks the same way: a notice has a title, a text, named buttons that Enter and Escape reach, the key line and
  the chime; a question in the message field's place is a list or a field named with the question, Enter answers,
  Escape cancels. An app that needs to ask uses these and does not draw its own dialog.

### AX-8 Keys

- AX-8.1 Every key an app handles is one of two kinds: **bundle-wide**, meaning the same in every window of Axit, or
  **window-specific**, meaning something in one window only. Each app's keyboard table in its spec marks every key
  with its kind.
- AX-8.2 Bundle-wide keys are defined once, in `Axit.Forms/BundleKeys`, a plain static class of constants, and every
  window uses those constants. The list: F1 the keyboard shortcuts; Tab,
  Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab and F6 move between the window's controls (and between a notice's text, field
  and buttons); Ctrl+F find, F3 and Shift+F3 next and previous; Ctrl+S save (what the window holds: a conversation,
  a file); Ctrl+Plus and Ctrl+Minus text size; Enter and Escape on a notice; Page Up and Page Down one screen in an
  edit control; Ctrl+Up and Ctrl+Down the start of the previous and next paragraph in an edit control, a paragraph
  starting at the first line or at a line with text directly after a blank line, and Ctrl+Shift+Up and
  Ctrl+Shift+Down the selection grown to there, as in Word (`EditParagraphs`; the EDIT control's own keys scroll a
  row and leave the caret where it is, as Notepad's do, which a screen reader, reading at the caret after the key,
  reports as the same text again); Alt+F4 exit; Alt and F10 the menu. Everything else is window-specific.
- AX-8.3 Window-specific keys are defined once per window, in one table (`AxClaudeKeys`, `AxDownKeys`, `AxJSONKeys`), as constants
  with names that say what the key does. Changing a key is a change to that one line: the menu item and the key
  handler read the table, and the F1 text names the same keys and is changed with it. Single-letter quick keys
  (AxClaude's `h`, `i`, `o`) are in the table too. The text editor shell's keys (`Axit.Forms/EditorKeys`: Ctrl+N new,
  Ctrl+O open, Ctrl+Shift+S save as, Ctrl+G go to line, Ctrl+Shift+W word wrap, Ctrl+W show in File Explorer) are
  defined once for every editor window, and each editor app's table includes them, so that one table per window
  still holds every key and the collision test sees them.
- AX-8.4 A unit test in `tests/Axit.Tests` fails when a window-specific key equals a bundle-wide key, and lists the
  keys that two windows use for different actions, so that a collision between apps is seen before it is felt.
- AX-8.5 A key is never handled anywhere but through its table, and no key is intercepted that NVDA uses (AX-7.4).
  The text a menu item shows for its key comes from the tables too (`BundleKeys.Text`: "Ctrl+Shift+S", "Shift+Escape"),
  so that a key changed in its table changes in the menus by itself; only Windows' own keys (Alt+F4, the edit
  control's Ctrl+Z, Ctrl+X, Ctrl+C, Ctrl+V, Ctrl+A) and descriptions ("Enter in the message field") are written out.
  A key of the number row ends in a full stop ("Ctrl+1."): NVDA speaks the shortcut directly before the position of
  the item in the menu ("1 of 9"), and the synthesizer runs "Ctrl+1  1 of 9" together into "Ctrl+11 of 9"; the full
  stop is a pause between the two.

### AX-9 The Axit menu

- AX-9.1 Every window has an **Axit** menu (mnemonic A) between its own menus and Help, the same in every window: one
  class, `Axit.Forms/AxitMenu`, which the window constructs with its two update items, its latest-release state and
  its notice method. In order: New window ▸ and Recently closed ▸; a separator; Open windows ▸, Open windows by type ▸
  and Close all windows; a separator; Check for updates, Update to (while a newer release is known) and About ▸
  (AX-9.9). The lists are filled each time the menu opens, so that they show the windows and the files of that moment.
- AX-9.2 **New window ▸** (N): one item per app of the bundle (`Dispatch.Apps`), "AxClaude", "AxDown", with the letter
  after Ax as the mnemonic. It starts `Axit.exe <verb>` with no arguments: a new process and a new window (B-2), the
  app starting as it does from its Start menu entry.
- AX-9.3 **Open windows ▸** (O): every open window of the bundle's apps except this one, the most recently used first.
  The order is the one Windows keeps its top-level windows in, which is Alt+Tab's, so nothing is stored and Alt+A, O,
  1 is always the window the user was in before this one. **Open windows by type ▸** (B) holds one submenu per app,
  "AxClaude (2)", "AxDown (1)", the number being its open windows: that app's windows in the same order, this window
  included and checked, so that the list also says where the user is.
- AX-9.4 **Recently closed ▸** (R): the last ten files and folders a window of any app held when it closed or replaced
  them (AxDown's file, AxClaude's project folder), the newest first, each app and path once, those that still exist;
  Enter starts that app on it in a new window. The list is one file for the bundle, `%APPDATA%\Axit\recently-closed.json`
  (AX-5.1), written whole by the window that closes or swaps (`Axit.Core/RecentlyClosed`, through `SettingsFile`; two
  windows closing in the same instant leave the last writer's list). Nothing else about windows is stored.
- AX-9.5 A window's or a file's item is its number and a title, "1 notes.md - AxDown", the number as the mnemonic (1
  to 9, then a to z), so that Alt+A, O, 1 or Alt+A, R, 1 acts without an arrow key. Enter brings a window to the
  front, restored when it was minimized. A list with nothing in it holds one item that says so and does nothing, enabled
  ("No other open windows", "No open AxDown windows", "Nothing recently closed"; the apps' own lists likewise, "No
  recent folders", "No recent files"): WinForms skips disabled items when the keyboard moves through a menu, so a
  submenu of only a disabled item opens with nothing selected and leaves the screen reader with nothing to read.
- AX-9.6 The windows are the visible, unowned, titled top-level windows of every process named as this one, told
  apart by the end of their title (" - AxClaude", " - AxDown"; AD-10, AxClaude's `<folder> (<path>) - AxClaude`); a window of
  none of the apps (the bundle's own message box, AX-1.4) is listed nowhere. The lists are pure code in
  `Axit.Core/AxitWindows` and `Axit.Core/RecentlyClosed`, unit tested; only the listing and the switch (user32
  `EnumWindows`, `SetForegroundWindow`) and the menu itself are in `AxitMenu`.
- AX-9.7 Only Axit's windows are listed; Alt+Tab stays the way to everything else. Alt+A is a menu mnemonic, not a key
  of the tables (AX-8); the F1 texts name it. A separator carries no text and NVDA passes over it; the groups are
  told apart by their names, not by headers, which would be one more stop on every pass through the menu.
- AX-9.8 **Close all windows** (C) posts the close command Alt+F4 sends (`WM_SYSCOMMAND` `SC_CLOSE`) to every other
  open Axit window and then to this one; each window closes as it does on Alt+F4 and asks there what it would ask
  then (AxDown's Save changes?, AxClaude's Close AxClaude? while Claude works), so nothing is lost without a question.
- AX-9.9 **Check for updates (Axit n)…** (U) and **Update to Axit n…** (T, while a newer release is known) are the
  window's own update items (AX-3.1), placed here by the window. **About ▸** (A) is the bundle's About in sections,
  the same in every window (`Axit.Core/BundleAbout`), each shown as a notice of the window: **Version** (V; the
  version, the apps, the latest release as the window knows it, the releases page, the settings and logs folders),
  **Licence** (L; MIT, and the warranty and liability disclaimer in full words; the LICENSE file is the binding text),
  **Attribution** (A; the maker, the Anthropic note, what Axit is built with), **Axit guide** (G; `docs/axit/README.md`,
  the zip's guide, embedded by `Axit.Core`: install, start, update, remove; its headings take the window's heading
  keys, AX-7.2), and **Axit on GitHub** (H; the repository page, `UpdateCheck.RepositoryPage`, in the browser).

### AX-7 Accessibility rules every app follows

These are the rules of `docs/axclaude/SPEC.md` that are not about Claude; app specifications refer to them by number.

- AX-7.1 Every control has an `AccessibleName`; every action works from the keyboard; menu items have mnemonics and
  shortcuts. Native Win32 controls (EDIT, menus, status bar, `ListBox`) are used because NVDA reads them reliably;
  no WPF, no web view, no owner-drawn controls.
- AX-7.2 No second window of the app's own: questions, errors and help texts are notices drawn inside the window
  (`OverlayPanel`), never a `Form` or a `MessageBox` once a window exists. A notice's button may open the next notice
  (the update's progress, an error, AxDown's changed-on-disk question after Save), which then stays in view. The
  Windows folder, file and font pickers are the only separate windows.
  A notice's text takes the window's own keys for moving by Markdown structure, one `TextJump` pair per kind from the
  window's key table handed to `OverlayPanel.Jumps` and acted on by `TextJumps`, the code AxDown's editor uses for the same
  keys: AxClaude's heading letters (h, Shift+H, 1 to 6), AxDown's
  Ctrl+letters for every kind of its section 9. The result is announced on the notice; "No next heading" and the
  like when there is none, and the caret stays.
- AX-7.3 The caret never moves except as the direct result of the user's own command. The result of a navigation
  command is announced through a UI Automation notification, joined into one notification when several things arrive
  at once. Nothing speaks on a plain arrow key; information is given on demand.
- AX-7.4 Enter and Escape reach a notice's buttons from every control (`AcceptButton`, `CancelButton`); handled keys
  set `SuppressKeyPress`; NVDA's own key combinations are never intercepted and no global keyboard hook is installed.
- AX-7.5 The UI thread never blocks on I/O; background work is marshalled to the UI thread and batched.
- AX-7.6 Any change to focus handling, key handling, announcements or an edit control is checked with NVDA against
  the app's test plan before a release.
- AX-7.7 Colours are the system colours and follow the Windows app theme: the entry point calls
  `Application.SetColorMode(SystemColorMode.System)` before any window exists, so every window is light or dark as
  Windows is when the app starts, and a high contrast theme applies as it always has. No app sets a colour of its own.

## 5. Architecture

### 5.1 Projects and references

```
src/Axit/            executable (net10.0-windows): Program (crash handling, version, log start, dispatch), icons
src/Axit.Core/       class library (net10.0), no WinForms, unit tested
src/Axit.Forms/      class library (net10.0-windows), WinForms pieces the apps use, among them the text editor shell
                     (TextEditorForm, EditorApp, EditorKeys, EditorHost)
src/AxClaude/        class library (net10.0-windows): AxClaudeApp, MainForm, ... (AxClaude's window)
src/AxClaude.Core/   class library (net10.0): Vt, Transcript, Pty, Privacy, AppSettings (AxClaude's pure code)
src/AxDown/          class library (net10.0-windows): AxDownApp (its EditorApp), AxDownKeys, HelpText; the window is
                     the shell's, and its pure code (TextDocument, EditorSettings, MarkdownOutline) is Axit.Core's
src/AxJSON/          class library (net10.0-windows): AxJSONApp (its EditorApp), AxJSONKeys, HelpText (AxJSON's app side)
src/AxJSON.Core/     class library (net10.0): JsonScanner, JsonParser, JsonOutline, JsonFormat (AxJSON's pure code)
tests/Axit.Tests, tests/AxClaude.Tests, tests/AxJSON.Tests
```

References point one way: `Axit` references every app; an app's window project references its own `.Core`,
`Axit.Core` and `Axit.Forms`; an app's `.Core` references `Axit.Core`; the shared projects reference nothing of the
apps. `tests/Axit.Tests/LayoutTests` reads the project files and fails when this is broken.

### 5.2 Documents

```
CLAUDE.md               the map of the bundle and its rules (short); each project has its own CLAUDE.md with its notes
docs/axit/              SPEC.md (this), README.md (the zip's guide: install, the apps, uninstall), adding-an-app.md, plan.md
docs/axclaude/          SPEC.md, user-guide.md (embedded by src/AxClaude), claude-screens.md, nvda-test-plan.md
docs/axdown/            SPEC.md, user-guide.md (embedded by src/AxDown), nvda-test-plan.md
```

### 5.3 Modularity rules

- M1 One app, one set of folders: `src/<App>`, `src/<App>.Core`, `docs/<app>/`, `tests/<App>.Tests`. Nothing of the
  app lives elsewhere.
- M2 References point one way (5.1), enforced by a test.
- M3 Shared code moves on the second use, unchanged where possible, one piece per commit.
- M4 Notes next to the code: `src/<Project>/CLAUDE.md` holds that project's gotchas and points to its spec. Claude
  Code reads a nested `CLAUDE.md` when it works on files in that folder. The root `CLAUDE.md` is the map and the
  bundle rules, about 60 lines.
- M5 One spec per app, complete on its own; the bundle spec covers only what crosses apps and the accessibility
  rules (AX-7), which app specs cite by number. In AxClaude's code and documents, "SPEC.md" means
  `docs/axclaude/SPEC.md`.
- M6 One version, one changelog with app prefixes, one release.
- M7 An app's texts belong to the app: its user guide is embedded by its own project, its F1 text lives in its
  `HelpText`, its usage text in its `StartupOptions`.
- M8 A new app follows `docs/axit/adding-an-app.md`.
- M9 Keys live in tables (AX-8): one bundle-wide table in `Axit.Forms`, one table per window; a key is changed in its
  table and nowhere else.

## 6. Design decisions

- **B-1 One executable with a verb per app.** A self-contained .NET executable carries the runtime, about 70 MB; one
  per app would multiply the download, and two executables sharing a runtime folder would need a multi-file publish
  or an installed .NET, which the project has always avoided. One executable keeps the zip, the installer, the Apps
  entry and the updater as they are. A later app is another verb.
- **B-2 Apps are class libraries with one entry point.** `Axit`'s `Program` stays the only `Main`; each app is
  `<App>App.Run(args)`. One process shows one window; that keeps the apps apart for the taskbar, Alt+Tab and NVDA.
- **B-3 Shared code moves on the second use, unchanged.** Nothing is generalised on a guess, and moving code as it is
  kept AxClaude's behaviour identical when it became one app of the bundle (G5).
- **B-4 Names.** The bundle is Axit; the apps keep their names; Start entries are `Axit <App>` so that the bundle
  name groups them and the app name is still a word that search matches.
- **B-5 Settings and logs per app, under `Axit`.** One folder for the bundle, one file per app, so an app's settings
  code knows only its own record. The old AxClaude settings file is copied, not moved, so a return to 1.7.0 still
  finds it.
- **B-6 A bridge release before the rename.** The 1.6.0 updater finds a release's zip by its `-win-x64.zip` ending
  but refuses a download without `AxClaude.exe`. 1.7.0 relaxes that check, so every installed copy can take the 2.0.0
  update instead of failing with an error.
- **B-7 The GitHub repository is renamed to `Axit` with the 2.0.0 release.** GitHub redirects the old web and API
  addresses, so installed 1.7.0 copies still find the releases; `UpdateCheck.Repository`, the installer and the
  guides then name the new address.
- **B-8 `axit` without an argument starts AxClaude on the current folder.** That is what `axclaude` does today, and a
  console inside a project is where the command is typed. The usage text is one `--help` away.
- **B-9 No silent default program.** Windows protects the user's choice of default program; an installer cannot set
  it without a prompt. AxDown is registered as a program for its file types and offered in the right-click menu; the
  guide says how to make it the default in Settings, Default apps.
- **B-10 Plain code only.** Plain classes, static methods, records for data, one way to do each thing. No dependency
  injection, no interfaces with one implementation, no reflection beyond the two reads WinForms needs, no source
  generators, no NuGet packages. This is what lets a smaller model make a change without reading the whole code.
- **B-11 Releases go from `main`; larger work runs on a branch.** `main` is always releasable; a branch merges when
  its NVDA checks have passed. The transition to the bundle ran this way on the branch `axit`.
- **B-12 Keys are tables, in two kinds.** Several windows will want the same letters, and a key that means one thing
  in one window and another in the next is what a screen reader user feels first. Every key is either bundle-wide or
  window-specific (AX-8); the tables make a collision visible in a test and a change a one-line edit, which is what a
  smaller model needs when the keys have to move.
- **B-13 The notices and the question area are shared, not copied.** They work and they are what the user knows;
  every window asks the same way (AX-6.4). They moved to `Axit.Forms` unchanged when AxDown needed them (2.0.0).
- **B-14 An Axit menu instead of tabs.** One window holds one file or one conversation (B-2), and AxJSON will keep it
  so. Tabs in one window were considered for an editor of several files (2026-10-18) and dropped: a tab needs a
  modified state of its own, a close of its own against the window's, a title and a status bar that follow it, and
  keys whose meaning changes with the tab under the user's fingers, the thing a screen reader user feels first (B-12).
  The Axit menu (AX-9) gives the one thing tabs give, a list of what is open in one place, by recency and by app;
  Windows' own order of its windows is the recency, so nothing is stored or kept in step, and Alt+Tab is unchanged.

## 7. Status

- 2.0.0 (2026-10-02): the bundle. `Axit.exe` with AxClaude unchanged inside it and AxDown new; `Axit.Core` and
  `Axit.Forms`; the installer, publish and release scripts for the bundle; the repository renamed `KyleKeane/Axit`.
  Preceded by AxClaude 1.7.0, the bridge (B-6). Both apps checked with NVDA by the user before the release.
- 2.1.0: the key tables (`AxClaudeKeys`, `AxDownKeys`, `BundleKeys`) with the collision test (AX-8); heading
  navigation in AxDown. 2.1.1: AxDown's window spoken on opening (`AccessibleRole.Window`).
- 2.2.0: the update notice with Update and restart, Update and close, and a progress bar for the download.
- 2.3.0: the in-place install with one bar for download and install and the restart by the app itself (AX-2.8,
  AX-3.2); the user ran the full path from an older build with NVDA. 2.3.1: the documents pruned after the transition.
- 2.9.0 (2026-10-18), developed on the branch `axit` and checked with NVDA by the user: `MarkdownOutline` and
  `TextJumps` shared for AxDown's letters and every notice text (AX-7.2); the Axit menu in every window (AX-9, B-14)
  with About in sections and the embedded Axit guide; AxClaude's Session menu and AxDown's menus in the same shape;
  an enabled item in every empty list (AX-9.5).
