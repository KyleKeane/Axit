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
- **G5 Nothing breaks on the way.** AxClaude works as released; the transition keeps it working at every commit.

Non-goals: a plugin system or loading apps from separate files (apps are compiled in); more than one window per
process; an installer that needs administrator rights; setting Windows default programs silently (Windows does not
allow it, see B-9).

## 3. The apps

- **AxClaude** (built): `Axit.exe claude [<folder>] [options]`, Start entry `Axit AxClaude`, `Open in AxClaude` on
  folders, command `axclaude`. Specification `docs/axclaude/SPEC.md`.
- **AxDown** (specified at step 6, built at steps 7 to 9): `Axit.exe down [<file>]`, Start entry `Axit AxDown`,
  `Open in AxDown` on files and Open with for `.md`, `.markdown`, `.txt`, command `axdown`. Specification
  `docs/axdown/SPEC.md`, guide `docs/axdown/user-guide.md`, test plan `docs/axdown/nvda-test-plan.md`.

## 4. Functional requirements

### AX-1 Launch and dispatch

- AX-1.1 The first argument may be a verb naming an app: `claude`, `down`. The rest of the arguments go to that app
  unchanged, so `Axit.exe claude C:\src --record x.vt` is what `AxClaude.exe C:\src --record x.vt` was.
- AX-1.2 Without a verb, a first argument that is an existing folder goes to AxClaude and an existing file to AxDown,
  with the remaining arguments. A path that does not exist is reported with the usage text (a message box, since no
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
- AX-2.2 Start menu: one shortcut per app, named `Axit <App>` (`Axit AxClaude`, `Axit AxDown`), each starting
  `Axit.exe <verb>` with the app's icon. Windows search matches the start of each word, so `axc` and `axd` find them
  and `axit` lists them; this is checked by hand at step 5 and the names change to `<App> (Axit)` if it does not hold.
- AX-2.3 File Explorer: `Open in AxClaude` on folders and on a folder's background (`Axit.exe claude "%1"`, `%V` for
  the background); `Open in AxDown` on every file (`Axit.exe down "%1"`); AxDown registered as a program for `.md`,
  `.markdown` and `.txt`, so it appears under Open with and can be chosen as the default in Settings, Default apps
  (B-9).
- AX-2.4 Console commands: `axit.cmd`, `axclaude.cmd` and `axdown.cmd` in `%USERPROFILE%\.local\bin`, one line each,
  passing their arguments to `Axit.exe`, `Axit.exe claude` and `Axit.exe down`. They work from cmd and from PowerShell.
- AX-2.5 Settings, Apps lists one entry, `Axit`, with the version, the publisher and an Uninstall that runs
  `install.cmd -Uninstall`.
- AX-2.6 Installing over an existing AxClaude installation (any version) removes it completely first: its folder,
  Start entry, `axclaude.cmd`, registry keys and Apps entry. The installer says what it found. The AxClaude settings
  file is left where it is; the app copies it on its first start (AX-5.2).
- AX-2.7 The hand-over parameters stay: `install.ps1 -WaitForProcess <pid> -Start <folder> [-ContinueConversation]
  -LogFile <file>` installs after that process ends and starts AxClaude on the folder, as the 1.7.0 updater expects.
  `-StartFile <file>` (step 9) starts AxDown on the file the same way; nothing is renamed. Copies up to 2.2.0 update
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
  (Escape). Each app's Help menu has the same items.
- AX-3.2 Either update runs behind one notice with one progress bar (`Notice.Progress`; NVDA reports a progress bar
  by its own settings, so nothing is spoken about the progress itself): the zip is downloaded into
  `%LOCALAPPDATA%\Axit\updates\<version>` (the bar to 80 percent; Cancel keeps the installed version), then the
  downloaded version's `install.ps1 -InPlace` runs while the app keeps running (AX-2.8; "Downloaded. Installing";
  the installer's lines move the bar to 100; Cancel no longer applies and says so), then the window closes
  ("Installed. Restarting" or "Installed. Closing") and, after Update and restart, starts the installed program file
  itself with what it had open (AxClaude: the folder, and `-- --continue` when a message was sent; AxDown: the file,
  or nothing). A failed installer leaves the running version in place and shows its output in a notice. A download
  counts as complete when it holds `install.ps1` and a program file.
- AX-3.3 The 1.7.0 bridge (B-6): AxClaude 1.7.0 accepts a download whose program file is not `AxClaude.exe`, so an
  installed AxClaude updates into Axit through Help, Update now. The first Axit release is 2.0.0.
- AX-3.4 The update check and the version comparison are pure code in `Axit.Core/Updates/UpdateCheck`, unit tested;
  the download and the hand-over are `Axit.Core/Updates/Updater`.

### AX-4 Releases

- AX-4.1 `release.ps1 <version>` checks that `CHANGELOG.md` has a `## <version>` section, runs the tests, sets
  `<Version>`, commits `Release <version>`, tags `v<version>` and pushes. `.github/workflows/release.yml` checks the
  tag against the version, tests, runs `publish.ps1 -NoInstall` and creates the GitHub release `Axit <version>` with
  `Axit-<version>-win-x64.zip` and `install.cmd` attached and the CHANGELOG section as notes.
- AX-4.2 `publish.ps1` publishes `src/Axit` self-contained, single file, into `publish\win-x64` with `install.cmd`,
  `install.ps1`, `LICENSE` and `docs/axit/README.md` as `README.md`, and zips it.
- AX-4.3 One `CHANGELOG.md`; every bullet starts with the app it concerns (`AxClaude:`, `AxDown:`, `Axit:` for the
  bundle itself).
- AX-4.4 `build.yml` runs the tests of the whole solution on every push and pull request.

### AX-5 Settings, logs and paths

- AX-5.1 Each app has its own settings file, `%APPDATA%\Axit\<app>.json` (`axclaude.json`, `axdown.json`), and its own
  log, `%LOCALAPPDATA%\Axit\logs\<app>.log`. The updater's log is `%LOCALAPPDATA%\Axit\logs\update.log`.
- AX-5.2 On its first start under Axit, AxClaude copies `%APPDATA%\AxClaude\settings.json` to `axclaude.json` when the
  new file does not exist; the old file is left alone.
- AX-5.3 The load-and-save pattern of a settings file (JSON, a missing or unreadable file falls back to the defaults
  and the error is reported once) is one piece of shared code, `Axit.Core/SettingsFile`; each app's settings class is
  its own plain record of properties.

### AX-6 Shared pieces

- AX-6.1 `Axit.Core` (pure, unit tested; built at step 4): `Dispatch`, `BundleInfo`, `AppPaths`, `SettingsFile`,
  `Log` (per-app file name), `Updates/` (`UpdateCheck`, the release check, and `Updater`, the download and the
  hand-over to `install.ps1`; neither needs a window), `Audio/WaveTone` (the sounds as WAV bytes).
- AX-6.2 `Axit.Forms` (WinForms, no tests, checked with NVDA; built at step 8): `OverlayPanel` (the notices in the
  window, with the `Notice` record callers pass), `ControlArea` and `ListKeys` (a question's answers as a list or a
  field in the input field's place, with the number and page keys of every such list), `Sounds` (one wave-out device
  for the life of the process), `EditPaging` (Page Up and Page Down in an edit control), `StatusLayout`, the font
  handling (text size and the font picker), `BundleKeys` (AX-8). The crash report (twenty lines in each app's
  `Run`) stays per app until a second copy shows what is common.
- AX-6.3 A piece moves into a shared project only when a second app needs it, in a commit of its own, with the
  accessibility review; it moves unchanged where possible (B-3).
- AX-6.4 The notices and the question area are the bundle's one way of asking the user something, so that every
  window asks the same way: a notice has a title, a text, named buttons that Enter and Escape reach, the key line and
  the chime; a question in the input field's place is a list or a field named with the question, Enter answers,
  Escape cancels. An app that needs to ask uses these and does not draw its own dialog.

### AX-8 Keys

- AX-8.1 Every key an app handles is one of two kinds: **bundle-wide**, meaning the same in every window of Axit, or
  **window-specific**, meaning something in one window only. Each app's keyboard table in its spec marks every key
  with its kind.
- AX-8.2 Bundle-wide keys are defined once, in `Axit.Forms/BundleKeys`, a plain static class of constants, and every
  window uses those constants. The list, settled with the AxDown spec (step 6): F1 the keyboard shortcuts; Tab,
  Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab and F6 move between the window's controls (and between a notice's text, field
  and buttons); Ctrl+F find, F3 and Shift+F3 next and previous; Ctrl+S save (what the window holds: a conversation,
  a file); Ctrl+Plus and Ctrl+Minus text size; Enter and Escape on a notice; Page Up and Page Down one screen in an
  edit control; Alt+F4 exit; Alt and F10 the menu. Everything else is window-specific.
- AX-8.3 Window-specific keys are defined once per window, in one table (`AxClaudeKeys`, `AxDownKeys`), as constants
  with names that say what the key does. Changing a key is a change to that one line: the menu item and the key
  handler read the table, and the F1 text names the same keys and is changed with it. Single-letter quick keys
  (AxClaude's `h`, `i`, `o`) are in the table too.
- AX-8.4 A unit test in `tests/Axit.Tests` fails when a window-specific key equals a bundle-wide key, and lists the
  keys that two windows use for different actions, so that a collision between apps is seen before it is felt.
- AX-8.5 A key is never handled anywhere but through its table, and no key is intercepted that NVDA uses (AX-7.4).

### AX-7 Accessibility rules every app follows

These are the rules of `docs/axclaude/SPEC.md` that are not about Claude; app specifications refer to them by number.

- AX-7.1 Every control has an `AccessibleName`; every action works from the keyboard; menu items have mnemonics and
  shortcuts. Native Win32 controls (EDIT, menus, status bar, `ListBox`) are used because NVDA reads them reliably;
  no WPF, no web view, no owner-drawn controls.
- AX-7.2 No second window of the app's own: questions, errors and help texts are notices drawn inside the window
  (`OverlayPanel`), never a `Form` or a `MessageBox` once a window exists. The Windows folder, file and font pickers
  are the only separate windows.
- AX-7.3 The caret never moves except as the direct result of the user's own command. The result of a navigation
  command is announced through a UI Automation notification, joined into one notification when several things arrive
  at once. Nothing speaks on a plain arrow key; information is given on demand.
- AX-7.4 Enter and Escape reach a notice's buttons from every control (`AcceptButton`, `CancelButton`); handled keys
  set `SuppressKeyPress`; NVDA's own key combinations are never intercepted and no global keyboard hook is installed.
- AX-7.5 The UI thread never blocks on I/O; background work is marshalled to the UI thread and batched.
- AX-7.6 Any change to focus handling, key handling, announcements or an edit control is checked with NVDA against
  the app's test plan before a release.

## 5. Architecture

### 5.1 Projects and references

```
src/Axit/            executable (net10.0-windows): Program (crash handling, version, log start, dispatch), icons
src/Axit.Core/       class library (net10.0), no WinForms, unit tested
src/Axit.Forms/      class library (net10.0-windows), WinForms pieces both apps use
src/AxClaude/        class library (net10.0-windows): AxClaudeApp, MainForm, ... (AxClaude's window)
src/AxClaude.Core/   class library (net10.0): Vt, Transcript, Pty, Privacy, AppSettings (AxClaude's pure code)
src/AxDown/          class library (net10.0-windows): AxDownApp, EditorForm, ... (AxDown's window)
src/AxDown.Core/     class library (net10.0): TextDocument, EditorSettings, MarkdownOutline
tests/Axit.Tests, tests/AxClaude.Tests, tests/AxDown.Tests
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
  keeps AxClaude's behaviour identical through the transition (G5).
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
- **B-11 The transition runs on the branch `axit`.** `main` stays AxClaude 1.7.0 and releasable until 2.0.0 is
  checked with NVDA; every commit on the branch builds and passes the tests.
- **B-12 Keys are tables, in two kinds.** Several windows will want the same letters, and a key that means one thing
  in one window and another in the next is what a screen reader user feels first. Every key is either bundle-wide or
  window-specific (AX-8); the tables make a collision visible in a test and a change a one-line edit, which is what a
  smaller model needs when the keys have to move.
- **B-13 The notices and the question area are shared, not copied.** They work and they are what the user knows;
  every window asks the same way (AX-6.4). They move to `Axit.Forms` unchanged when AxDown needs them (step 8).

## 7. Status

- 2026-10-02: plan written (`docs/axit/plan.md`); AxClaude's documents moved to `docs/axclaude/`; the notes split per
  project; this specification written. 1.7.0 (the bridge, B-6) released from `main`.
- 2026-10-02, step 3: `Axit.exe` exists (`src/Axit`), `src/AxClaude` is a class library with `AxClaudeApp.Run`,
  `Axit.Core/Dispatch` decides the app and is tested in `tests/Axit.Tests`; `Axit.sln`, `run.ps1` and `build.yml`
  follow. `publish.ps1`, `install.ps1` and the release pipeline still carry AxClaude's names and are not usable on
  the branch until step 5.
- 2026-10-02, step 4: `Axit.Core` holds `AppPaths`, `SettingsFile`, `Log`, `Updates/` and `Audio/WaveTone`;
  AxClaude's settings file is `%APPDATA%\Axit\axclaude.json` (the 1.x file copied on the first start) and its log
  `%LOCALAPPDATA%\Axit\logs\axclaude.log`; `tests/Axit.Tests` has `SettingsFileTests` and `LayoutTests`.
- 2026-10-02, step 5: `install.ps1`, `install.cmd`, `publish.ps1`, `release.ps1` and `release.yml` are the bundle's
  (`Programs\Axit`, `Axit AxClaude`, `axit` and `axclaude`, the Apps entry `Axit`, `Axit-<version>-win-x64.zip`,
  `docs/axit/README.md` in the zip); a 1.x installation is removed first. Kyle's checks of the Start menu search,
  the folder menu and the console commands are pending (AX-2.2).
- 2026-10-02, steps 6 to 8: AxDown specified (`docs/axdown/`), its pure code and window built; `Axit.Forms` holds
  the shared window pieces, moved from AxClaude unchanged (AX-6.2), plus `BundleKeys` and `WindowAccessibleObject`;
  `tests/Axit.Tests/KeyTests` is the collision test (AX-8.4), with AxDown's table in it and AxClaude's to come at
  step 11. `Axit.exe down <file>` works.
- 2026-10-02, step 9: the installer adds AxDown's Start entry, "Open in AxDown" on files, the `Axit.AxDown` ProgID
  under Open with for `.md`, `.markdown` and `.txt`, `axdown.cmd`, and `-StartFile` for the updater; `AxDown.ico` is
  drawn by `tools/make-icon.ps1` and shipped next to the executable. Kyle's checks (AX-2.2) pending for both apps.
- 2026-10-02, step 10: Kyle's NVDA checks passed for both apps; the repository was renamed to `KyleKeane/Axit`
  (B-7); Axit 2.0.0 released from `main`.
- 2026-10-02, step 11: `AxClaudeKeys` holds AxClaude's keys; the window, the menus and the conversation view read
  it; the collision test covers both windows (AX-8.4). No key changed.
