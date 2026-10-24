# Axit — Bundle Specification

Describes Axit, the bundle the axClaude repository became on 2026-10-02 (plan: `docs/axit/plan.md`). Every
requirement is built unless its text says otherwise; this document is kept in step with the code (a behaviour change
updates it in the same commit), and what each release brought is section 7. It covers only what crosses apps. Each
app has its own specification, `docs/<app>/SPEC.md` (section 3). Requirements here are numbered `AX-n.m` and
decisions `B-n`, so that they never clash with an app's `FR-n.m` and `D-n`.

## 1. Summary

Axit is one installed Windows program, `Axit.exe`, that holds several accessible apps for a blind developer who uses
the NVDA screen reader. Each app is a separate window that starts on its own, has its own Start menu entry, its place in
File Explorer's Open with Axit submenu and its own console command, and all of them share one installer, one updater, one
version number and one release workflow. The first apps were **axClaude**, the front end for the Claude Code CLI, and
**axDown**, a barebones editor for Markdown and text files; **axJSON**, **axGIT**, **axTerminal**, **axFile**,
**axMedia**, **axPreview**, **axStart**, **axProcess**, **axTask**, **axPass**, **axCalc** and **axClip** followed (section 3). Later apps are planned (a password manager, a vault
manager: `docs/axit/plan.md`) and follow the same pattern without changes to the bundle.

## 2. Goals and non-goals

- **G1 One download, one install, one update.** The bundle is a single self-contained executable with the .NET
  runtime inside it, so the zip stays one file of about 70 MB whatever the number of apps.
- **G2 Apps that start apart.** Windows+`axc` and Windows+`axd` each find one app; a folder opens in axClaude, a file
  in axDown; each app has its own command word. Nothing about the bundle is in the user's way.
- **G3 Modular code and documents.** A change to one app needs only that app's folders, spec and notes in context.
  This is what makes the repository workable for smaller AI models and cheap to extend.
- **G4 Plain code.** Zero third-party runtime dependencies, as axClaude always had, and no coding paradigm that a
  smaller model would struggle to edit: plain classes, static methods, one obvious way to do each thing.
- **G5 Nothing breaks.** axClaude behaves as released; every commit builds and passes the tests, and a change to a window is checked with NVDA before it is released.
- **G6 Local first.** Every app has its full utility with the network off; axClaude is the one exception, since
  Claude Code is a service. The update check (AX-3) is the only call a window makes on its own, a setting that can be
  off, and a network failure is never more than a missing update. Any AI help in the other apps is one shared,
  optional service (B-15), never a dependency of a feature that works without it.

Non-goals: a plugin system or loading apps from separate files (apps are compiled in); more than one window per
process; an installer that needs administrator rights; setting Windows default programs silently (Windows does not
allow it, see B-9).

## 3. The apps

The apps are named ax and a word, the ax in lower case and the word as it is written: axClaude, axDown, axJSON, axGIT, axTerminal, axFile, axMedia, axPreview, axStart, axProcess, axTask, axPass, axCalc, axClip (2026-10-23, the user's rule; until then AxClaude and the like). A screen reader then says the name as ax and a word it knows, and an acronym keeps its capitals. Nothing depends on the case: the Start menu, the command palette, axStart and the console commands (xclaude) find a name typed in any case, and the code that tells the apps apart by name (a window's title, the Recently closed list, the hand-over) ignores case, so an entry saved under the old spelling still opens. The code keeps the C# spelling of its projects, folders and classes (`src/AxClaude`, `AxClaudeApp`), which no screen reader speaks; `Dispatch.App` holds both, `Name` and `Project`.

- **axClaude** (built): `Axit.exe claude [<folder>] [options]`, Start entry `Axit axClaude`, Open with Axit on
  folders, command `axclaude`. Specification `docs/axclaude/SPEC.md`.
- **axDown**: `Axit.exe down [<file>]`, Start entry `Axit axDown`,
  Open with Axit on files and Open with for `.md`, `.markdown`, `.txt`, command `axdown`. Specification
  `docs/axdown/SPEC.md`, guide `docs/axdown/user-guide.md`, test plan `docs/axdown/nvda-test-plan.md`.
- **axJSON**: `Axit.exe json [<file>]`, Start entry `Axit axJSON`, Open with Axit on `.json` files and Open with
  for `.json`, command `axjson`. The text editor shell axDown is, with JSON's navigation keys and a Format command.
  Specification `docs/axjson/SPEC.md`, guide `docs/axjson/user-guide.md`, test plan `docs/axjson/nvda-test-plan.md`.
- **axGIT** (first draft, 2026-10-18): `Axit.exe git [<folder or file>]`, Start entry `Axit axGIT`, Open with Axit
  on folders, command `axgit`. A git manager for one repository, a window per repository, opened from the other
  windows' "Show current … in ▸" submenus on the repository of the folder or file in hand. Specification
  `docs/axgit/SPEC.md`, guide `docs/axgit/user-guide.md`, test plan `docs/axgit/nvda-test-plan.md`.
- **axTerminal** (first draft, 2026-10-18): `Axit.exe terminal [<folder>]`, Start entry `Axit axTerminal`, Open with
  Axit on folders, command `axterminal`. A shell (PowerShell, or cmd) in axClaude's window: the output as a
  transcript with exchange blocks, the shell's integration marks for Done and Failed. Specification
  `docs/axterminal/SPEC.md`, guide `docs/axterminal/user-guide.md`, test plan `docs/axterminal/nvda-test-plan.md`.
- **axFile** (first draft, 2026-10-19): `Axit.exe file [<folder or file>]`, Start entry `Axit axFile`, Open with Axit
  on folders and on files, command `axfile`. A file manager for one folder as one list, Enter the verb (a folder
  here, a file in the user's default program, as in File Explorer), the file operations with Undo and an Actions
  list of every operation; its player and preview became axMedia and axPreview on 2026-10-22
  (`docs/axit/axfile-split.md`). Specification `docs/axfile/SPEC.md`, guide `docs/axfile/user-guide.md`, test plan
  `docs/axfile/nvda-test-plan.md`.
- **axMedia** (2026-10-22): `Axit.exe media [<file or folder>...] [--add] [--new]`, Start entry `Axit axMedia`, Open
  with Axit on sound and video files and Open with for their kinds, command `axmedia`. A player with one queue, the
  player axFile carried until the split (`docs/axit/axfile-split.md`); a start with paths hands them to the open
  axMedia window (`AppMessage`). Specification `docs/axmedia/SPEC.md`, guide `docs/axmedia/user-guide.md`, test plan
  `docs/axmedia/nvda-test-plan.md`.
- **axPreview** (2026-10-22): `Axit.exe preview [<file>] [--new]`, Start entry `Axit axPreview`, Open with Axit on
  every file and Open with for `.txt`, `.md`, `.markdown`, `.log`, `.json`, command `axpreview`. A read-only viewer for
  one text file: the text editor shell in its read-only mode, with axDown's navigation keys; a start with a file
  hands it to the open axPreview window. Specification `docs/axpreview/SPEC.md`, guide `docs/axpreview/user-guide.md`,
  test plan `docs/axpreview/nvda-test-plan.md`.
- **axStart** (2026-10-22, 3.4.0): `Axit.exe start [--hidden]`, Start entry `Axit axStart`, command
  `axstart`, no File Explorer entry. A program launcher: a background process from sign-in (the Run key) with a
  global hotkey, Ctrl+Shift+Space unless the user chose another, that shows one search field over the Start menu's
  programs, Down and Up through the matches with the caret kept in the field. Specification `docs/axstart/SPEC.md`,
  guide `docs/axstart/user-guide.md`, test plan `docs/axstart/nvda-test-plan.md`.
- **axProcess** (2026-10-23, branch `axprocess`): `Axit.exe process`, Start entry `Axit axProcess`, command `axprocess`,
  no File Explorer entry. What runs on the computer as one list with three views (processes, services, startup
  programs), read on F5 and never on a timer; a process ended after a question, a service started or stopped.
  Specification `docs/axprocess/SPEC.md`, guide `docs/axprocess/user-guide.md`, test plan
  `docs/axprocess/nvda-test-plan.md`.
- **axTask** (2026-10-24, branch `axtask`): `Axit.exe task [<folder>]`, Start entry `Axit axTask`, command `axtask`,
  folders in File Explorer's Open with Axit submenu. Tasks and lists as a folder of JSON files, one per item, in a
  navigator like axFile's with a detail pane of the item's typed fields; the item apps' shared shape is
  `docs/axit/item-apps.md`. Specification `docs/axtask/SPEC.md`, guide `docs/axtask/user-guide.md`, test plan
  `docs/axtask/nvda-test-plan.md`.
- **axPass** (2026-10-24, branch `axtask`): `Axit.exe pass [<database>]`, Start entry `Axit axPass`, command `axpass`,
  `.kdbx` files in File Explorer's Open with Axit submenu and Open with. A KeePass database in axTask's two panes,
  through KeePassXC's command-line tool, which must be installed. Specification `docs/axpass/SPEC.md`, guide
  `docs/axpass/user-guide.md`, test plan `docs/axpass/nvda-test-plan.md`.
- **axCalc** (2026-10-23, branch `axcalc`): `Axit.exe calc`, Start entry `Axit axCalc`, command `axcalc`, no File
  Explorer entry. A calculator as one expression field and a history list: Enter says the answer, the history keeps
  every answer to read back, copy and recall, and Ctrl+I says an answer in hexadecimal, binary and octal.
  Specification `docs/axcalc/SPEC.md`, guide `docs/axcalc/user-guide.md`, test plan `docs/axcalc/nvda-test-plan.md`.
- **axClip** (2026-10-23, branch `axclip`): `Axit.exe clip [--hidden]`, Start entry `Axit axClip`, command `axclip`, no File
  Explorer entry. A clipboard history behind a hotkey (Windows+Alt+V unless the user chose another): a background
  process from sign-in, as axStart, on the bundle's `HotkeyForm`, with one search field over the text of every copy;
  Enter pastes into the program that was in front. Specification `docs/axclip/SPEC.md`, guide
  `docs/axclip/user-guide.md`, test plan `docs/axclip/nvda-test-plan.md`.

## 4. Functional requirements

### AX-1 Launch and dispatch

- AX-1.1 The first argument may be a verb naming an app: `claude`, `down`, `json`, `git`, `terminal`, `file`, `media`, `preview`, `start`, `process`, `task`, `pass`, `calc`, `clip` (the one list is `Dispatch.Apps`). The rest of the arguments go to that app
  unchanged, so `Axit.exe claude C:\src --record x.vt` is what `AxClaude.exe C:\src --record x.vt` was.
- AX-1.2 Without a verb, a first argument that is an existing folder goes to axClaude and an existing file to axDown,
  or to axJSON when its extension is `.json`, to axPass when it is `.kdbx`, or to axMedia when it is a sound or video
  file (`MediaFiles.IsMedia`),
  with the remaining arguments. A path that does not exist is reported with the usage text (a message box, since no
  window exists yet, as `--help` does today).
- AX-1.3 Without any argument, `Axit.exe` starts axClaude on the current folder, which is what `axclaude` does today
  (B-8). axClaude keeps its own rule for a current folder that is the program's own folder.
- AX-1.4 `--help` and `--version` answer for the bundle and list the verbs; `Axit.exe claude --help` is the app's own
  usage text. One verb is not an app: `mcp`, the AI tools' broker an MCP client starts, with no window
  (`docs/axit/mcp.md` MC-1.1).
- AX-1.5 The dispatch is pure code in `Axit.Core` (`Dispatch`): arguments in, app name and arguments out. It is unit
  tested and has no knowledge of the apps beyond their names.
- AX-1.6 Each process sets a Windows application id before its window opens (`Axit.AxClaude`, `Axit.AxDown`, `Axit.AxJSON`, `Axit.AxGit`, `Axit.AxTerminal`, `Axit.AxFile`, `Axit.AxMedia`, `Axit.AxPreview`, `Axit.AxStart`, `Axit.AxProcess`, `Axit.AxTask`, `Axit.AxPass`, `Axit.AxCalc`, `Axit.AxClip`), so the
  taskbar shows the apps apart and Win+number keys reach the right one.
- AX-1.7 An app is a class library with one entry point, `static void Run(string[] args)` on a class named
  `<App>App`, plus a `Usage` text. `Axit`'s `Program` only dispatches; the entry point is one call into
  `Axit.Forms/AppHost.Run`, which owns the log, the crash handling (an exception in a UI handler is logged and shown
  in the window as a notice, or in a message box when no window exists yet), the start and exit lines in the log,
  `--help` and `--version`, and runs the window the app makes from the rest of its arguments (`AppHost.OnePath`
  for an app that takes one file or folder, `AppHost.BadCommandLine` for the rest).

### AX-2 Installation

- AX-2.1 `install.ps1` installs for the current user into `%LOCALAPPDATA%\Programs\Axit` with no administrator
  rights, from the extracted zip or from `publish\win-x64`; `install.cmd` runs it with the execution policy bypassed
  and, downloaded alone, fetches the latest release first. `-Uninstall` removes everything of Axit and keeps the
  settings and the logs. This is axClaude's installer today (`docs/axclaude/SPEC.md` FR-8.7, D18) with the names changed.
- AX-2.2 Start menu: one shortcut per app, named `Axit <App>`, each starting `Axit.exe <verb>` with the app's icon.
  Windows search matches the start of each word, so `axc`, `axd` and `axj` find them and `axit` lists them (checked).
- AX-2.3 File Explorer: one submenu, **Open with Axit**, in the right-click menu of folders, of a folder's background
  and of files, listing the apps that open what was right-clicked, by name, in the order of their names. It is a
  static cascading menu (`HKCU\Software\Classes\Directory\shell\Axit`, `Directory\Background\shell\Axit` and
  `*\shell\Axit`, each with `MUIVerb`, an empty `SubCommands` and the apps as verbs under its `shell` subkey, running
  `Axit.exe <verb> "%1"`, `%V` on the background). An app that opens only some file types says so with `AppliesTo`
  (`System.FileExtension:=".json"`), so the registry shows an app only where it belongs and no app checks the path
  it is given. The table is held once, in `install.ps1`'s `$apps`, one block per app; an app that is also a program
  for its file types, under Windows' own Open with and in Settings, Default apps (where the user, never the
  installer, makes it the default, B-9), says so in the same block:

  | App | Folders, and a folder's background | Files | Program for (Open with, Default apps) |
  | --- | --- | --- | --- |
  | axClaude | yes | | |
  | axDown | | every file | `.md`, `.markdown`, `.txt` (`Axit.AxDown`) |
  | axJSON | | `.json` | `.json` (`Axit.AxJSON`) |
  | axGIT | yes | | |
  | axTerminal | yes | | |
  | axFile | yes | every file (its folder, the file selected) | |
  | axMedia | | the sound and video kinds (`MediaFiles.Extensions`, listed again in the block) | the same kinds (`Axit.AxMedia`) |
  | axPreview | | every file | `.txt`, `.md`, `.markdown`, `.log`, `.json` (`Axit.AxPreview`) |
  | axStart | | | |
  | axProcess | | | |
  | axTask | folders | | |
  | axPass | | `.kdbx` | `.kdbx` (`Axit.AxPass`) |
  | axCalc | | | |
  | axClip | | | |

  On Windows 11 the submenu is in the full menu (Shift+F10, or Show more options), as every program's own entries
  are. Up to 2.13.0 each app had an entry of its own (`Open in axClaude` and the like); the installer removes them.
- AX-2.4 Console commands: `axit.cmd` and one `<app>.cmd` per app (`axclaude.cmd`, `axdown.cmd`, `axjson.cmd`,
  `axgit.cmd`, `axterminal.cmd`, `axfile.cmd`, `axmedia.cmd`, `axpreview.cmd`, `axstart.cmd`, `axprocess.cmd`, `axtask.cmd`, `axpass.cmd`, `axcalc.cmd`, `axclip.cmd`) in `%USERPROFILE%\.local\bin`, one line each, passing their arguments to `Axit.exe`
  and `Axit.exe <verb>`. They work from cmd and from PowerShell.
- AX-2.5 Settings, Apps lists one entry, `Axit`, with the version, the publisher and an Uninstall that runs
  `install.cmd -Uninstall`.
- AX-2.6 Installing over an existing axClaude installation (any version) removes it completely first: its folder,
  Start entry, `axclaude.cmd`, registry keys and Apps entry. The installer says what it found. The axClaude settings
  file is left where it is; the app copies it on its first start (AX-5.2).
- AX-2.7 The hand-over parameters stay: `install.ps1 -WaitForProcess <pid> -Start <folder> [-ContinueConversation]
  -LogFile <file>` installs after that process ends and starts axClaude on the folder, as the 1.7.0 updater expects.
  `-StartFile <file>` starts axDown on the file the same way; nothing is renamed. Copies up to 2.2.0 update
  this way; from 2.3.0 the apps use AX-2.8.
- AX-2.8 In place: `install.ps1 -InPlace -LogFile <file>` installs while the app keeps running. The new files are
  written over the old ones, and what earlier versions left goes, as on a fresh install: their File Explorer
  entries, axClaude 1.x's folder, Apps entry and Start menu entry, and files in the folder this version does not ship
  (from 3.0.0; before, nothing was removed in place); the running `Axit.exe` is first renamed `Axit.old.exe` (a running
  program file can be renamed but not overwritten or deleted), and `Axit.exe` deletes any `Axit.old*.exe` next to it
  when it starts. The shortcuts, menus, shims and the Apps entry are written again, so the version shown in Settings,
  Apps is the new one. The app, not the installer, starts the new version afterwards (AX-3.2).
- AX-2.9 **The programs an app needs and Windows lacks** (the user's ask, 2026-10-24): Git (axGIT, and Git Bash in
  axTerminal), KeePassXC (axPass), Claude Code (axClaude) and PowerShell 7 (axTerminal). Axit's installer installs
  none of them, so that it stays a per-user install without an administrator prompt and installs nothing nobody asked
  for. Each app checks for its program and, when it is missing, its notice says so and offers **Install it now**
  (`Axit.Core.ProgramInstall`, `AxitForm.InstallProgram`): the maker's own package through winget by its exact id
  (`Git.Git`, `KeePassXCTeam.KeePassXC`, `Microsoft.PowerShell`), or the maker's own installer (Claude Code's, for the
  user only), never a copy of Axit's; every question answered, silent, no window. The notice says beforehand whether
  Windows will ask for an administrator's yes. When it ends the PATH is read again from Windows and the app checks
  for the program itself: found, "Git is installed" and the app goes on (axGIT opens the repository, axPass asks for
  the master password, axClaude starts Claude, axTerminal switches to the shell); not found, a notice with the
  installer's exit code and last line, Try again and the download page. Without winget (an older Windows 10) the
  download page opens instead. Only on the user's command, never on a timer or at start (B-15); each install is
  logged. Removing such a program is Settings, Apps, as for any program. A shell that is not installed is not offered
  in axTerminal (AT-1.2a). winget is the one Windows puts in the user's WindowsApps folder, never one on the PATH,
  and installs from its own source (`--source winget`, security audit S-10); Claude Code's installer runs in
  Windows PowerShell by its full path. One install at a time in a window: a second Install it now, while one runs or
  after the window stopped waiting for one (it is never killed), says so. Install it now is never the notice's
  default button: Enter is Close, as the key line says (the first default of a notice is its Enter, OverlayPanel).
  The end closes only its own progress notice; a failure while the user is in another notice is announced, not shown
  over it. The installer's output is read under the same deadline, so a helper left running cannot hold it.

### AX-3 Updates

- AX-3.1 One version for the bundle, `<Version>` in `src/Axit/Axit.csproj`. A newer GitHub release is offered as
  axClaude does (`docs/axclaude/SPEC.md` FR-1.10, D25): asked once at startup when the setting is on, a notice with
  the release notes and four buttons: Update and restart (Enter), Update and close, Open release page, Later
  (Escape). A development build (Debug, as `run.ps1` makes it) does not ask at startup, since its version is whatever
  the working tree said when it was last built; Check for updates still asks. Every window's Axit menu ends with Check for updates (Axit <version>), always, naming the version in use
  and asking GitHub again every time, Update to Axit <version> below it while a newer release is known, and About, the
  bundle's About in sections (AX-9.9, `Axit.Core/BundleAbout`), the same in every window. Every text of the update
  flow names Axit, never an app: the version is the bundle's.
- AX-3.2 Either update runs behind one notice with one progress bar (`Notice.Progress`; NVDA reports a progress bar
  by its own settings, so nothing is spoken about the progress itself): the zip is downloaded into
  `%LOCALAPPDATA%\Axit\updates\<version>` (the bar to 80 percent; Cancel keeps the installed version), then the
  downloaded version's `install.ps1 -InPlace` runs while the app keeps running (AX-2.8; "Downloaded. Installing";
  the installer's lines move the bar to 100; Cancel no longer applies and says so), then the window closes
  ("Installed. Restarting" or "Installed. Closing") and, after Update and restart, starts the installed program file
  itself with what it had open (axClaude: the folder, and `-- --continue` when a message was sent; axDown: the file,
  or nothing). A failed installer leaves the running version in place and shows its output in a notice. A download
  counts as complete when it holds `install.ps1` and a program file. Cancel counts until the download is unpacked, a
  download that is not a zip is a failed update like any other, and an installed program file that cannot be
  started as the window closes is logged (`StartInstalled`), the window closing all the same; an installer that
  cannot be started (PowerShell blocked by a policy) or an update that passes ten minutes is a failed update like
  any other, a notice and the running version kept (2026-10-23: both escaped as a crash notice over the progress).
  The window's close for an installed update is a real close in every window, the one that otherwise hides on
  closing included (axStart, `ClosingForUpdate`). Each app's close
  question after Update and restart (axClaude's Keep working, axDown's Cancel in the save question) keeps the window
  on the old program file and says "Update installed. It starts with the next axClaude" (or axDown).
- AX-3.3 The 1.7.0 bridge (B-6): axClaude 1.7.0 accepts a download whose program file is not `AxClaude.exe`, so an
  installed axClaude updates into Axit through Help, Update now. The first Axit release is 2.0.0.
- AX-3.4 The update check and the version comparison are pure code in `Axit.Core/Updates/UpdateCheck`, unit tested;
  the download and the hand-over are `Axit.Core/Updates/Updater`. **Signed releases** (2026-10-24, the user's
  decision; security audit S-27 to S-31): every release's zip is signed with the release key, an ECDSA P-256 key that
  lives only on the maintainer's machine, encrypted for its Windows account (`tools/release-key.ps1`), never on
  GitHub. The release workflow leaves a draft (a build job that can only read, then a job that uploads and attests
  the build's provenance); `release.ps1` then runs `tools/sign-release.ps1`, which checks the provenance, signs the
  zip's SHA-256, uploads `<zip>.sig` and publishes. The updater takes assets only from this repository's releases
  address, downloads afresh every time, and extracts nothing unless the zip matches GitHub's recorded checksum and its
  signature checks against the public half in `Axit.Core/Updates/ReleaseSigning`; `install.cmd` alone does the same
  check in Windows PowerShell, into a folder of a random name. A release without a signature is never installed by
  the apps. Losing the key means one release installed by hand with a new key's public half in it.

### AX-4 Releases

- AX-4.1 `release.ps1 <version>` checks that `CHANGELOG.md` has a `## <version>` section, fetches origin and stops
  when `origin/main` has commits that `main` does not (nothing is released over work pushed from elsewhere; skipped
  with `-NoPush`, which pushes nothing and may run offline), runs
  every test project in Release through `test.ps1` (3.3.0 was tagged and never published because a test passed in
  Debug and failed in Release), sets `<Version>`, commits `Release <version>`, tags `v<version>` and pushes. `.github/workflows/release.yml` checks the
  tag against the version, tests, runs `publish.ps1 -NoInstall` and creates the GitHub release `Axit <version>` with
  `Axit-<version>-win-x64.zip` and `install.cmd` attached and the CHANGELOG section as notes, as a draft that is
  published once the files are up (since 2026-10-21: the update check reads the latest published release, and one
  made while the zip was still uploading had a window download a half-written zip, "The installer failed").
- AX-4.2 `publish.ps1` publishes `src/Axit` self-contained, single file, into `publish\win-x64` with `install.cmd`,
  `install.ps1`, `LICENSE` and `docs/axit/README.md` as `README.md`, and zips it.
- AX-4.3 One `CHANGELOG.md`; every bullet starts with the app it concerns (`axClaude:`, `axDown:`, `Axit:` for the
  bundle itself, `For developers:` for the repository, the scripts and the tests); every released section's heading
  carries its date.
- AX-4.4 `build.yml` runs the tests of the whole solution in Release on every push to any branch and on every pull
  request, so that a branch fails before it is merged rather than at its release (since 2026-10-22; it ran on `main`
  alone). `test.ps1` runs the same tests locally (every `tests/*.Tests.csproj`, Release, one by one so that a running
  Axit's `bin\Debug` is never written); `release.ps1` and `run.ps1 -Test` call it.
- AX-4.5 The repository's own pages (2026-10-22): `README.md` (what Axit is, the apps, install, build, test, release,
  where things are), `CONTRIBUTING.md` (how a change is made: spec first, one app's folders, tests, the NVDA plan and
  its record, the changelog), `SECURITY.md` (private reports through GitHub's security advisories), issue forms in
  `.github/ISSUE_TEMPLATE` (a bug asks for the app, the versions and the test plan step), a pull request checklist,
  and Dependabot (`.github/dependabot.yml`) keeping the workflow actions and the test packages current, monthly; the
  apps themselves have no packages (G4, B-10).

### AX-5 Settings, logs and paths

- AX-5.1 Each app has its own settings file, `%APPDATA%\Axit\<app>.json` (`axclaude.json`, `axdown.json`), and its own
  log, `%LOCALAPPDATA%\Axit\logs\<app>.log`, each line with the time, the level and the process id in brackets, so
  that two windows of one app writing one log can be told apart (2026-10-23). The updater's log is `%LOCALAPPDATA%\Axit\logs\update.log`. The bundle
  has one file of its own, `%APPDATA%\Axit\recently-closed.json`, the Recently closed list of the Axit menu (AX-9.4).
- AX-5.2 On its first start under Axit, axClaude copies `%APPDATA%\AxClaude\settings.json` to `axclaude.json` when the
  new file does not exist; the old file is left alone.
- AX-5.3 The load-and-save pattern of a settings file (JSON, a missing or unreadable file falls back to the defaults
  and the error is reported once) is one piece of shared code, `Axit.Core/SettingsFile`; each app's settings class is
  its own plain record of properties. A file that is not valid JSON (a typo after Options → Developer → Open settings file) is
  copied to `<app>.json.bad` before the defaults are used, and the error names the copy: the app saves its settings
  soon after loading, which would otherwise write the defaults over every other setting in the file.

### AX-6 Shared pieces

- AX-6.1 `Axit.Core` (pure, unit tested): `Dispatch` (with `Apps`, the name and verb of each app), `BundleInfo`, `AppPaths`, `SettingsFile`, `WindowSettings` (what every window's settings hold, AX-6.5), `ConsoleSettings` (what the two console windows' settings hold besides: the console size, the recent folders, the sounds and signals, the transcript options; axClaude's and axTerminal's classes derive from it under the JSON names they had), `TextMatch` (a typed text against a name, AX-10.3) and `Accents` (the European letters taken apart and stripped without the globalization data: the executable runs with invariant globalization, where `string.Normalize` does nothing, and the test projects run the same way so that such a dependence fails in a test and not in the program; 2026-10-23),
  `Log` (per-app file name), `Updates/` (`UpdateCheck`, the release check, and `Updater`, the download and the
  hand-over to `install.ps1`; neither needs a window), `Audio/WaveTone` (the sounds as WAV bytes), `Paragraphs` (the
  paragraph starts of a text, for Ctrl+Up and Ctrl+Down), `WindowPlacement`, `EmbeddedText` (the embedded guides),
  `NaturalOrder` and `MediaFiles` (File Explorer's order of names, and the sound and video kinds with the time and
  speed rules; from axFile on their second use, axMedia), `Palette` (the command palette's lines and search, AX-10), `MenuText` (a menu item's or a button's text as spoken: no mnemonic marker, no trailing dots; the palette, the notices' key line and the menu choices that announce themselves), `MarkdownOutline` (the headings, links, lists, block quotes and tables of a Markdown text, with next and previous from a caret position; axDown SPEC §9, shared since its second use), `AxitWindows` (the window lists of the Axit menu from the windows the system names, AX-9), `RecentlyClosed` (the bundle's list of closed files and folders, AX-9.4), `RecentPaths` (what every settings class does to its recent list: the newest first, once, ten at most, cleaned after loading; AX-9.5), `PathName` (the name of a path for a title or a label: the last part, or a drive's root itself), `BundleAbout` (the About sections and the embedded Axit guide, AX-9.9), and the console-to-transcript pieces that came from axClaude on their second use, axTerminal (2026-10-18, docs/axterminal/SPEC.md §5): `Pty/` (`PtyHost`, the ConPTY host, and `StreamRecorder`), `Vt/` (`VtParser`, `Screen`, `Wcwidth`) and `Transcript/` (`Line` and `LineKind`, `LineStore`, the base of each app's model: the lines the screen writes to, the decoder, the parser and the screen itself, the bookmarks and the cap, since 2026-10-23, `TranscriptMirror` and `Utterance`), tested in `tests/AxClaude.Tests` against its recordings.
- AX-6.2 `Axit.Forms` (WinForms, checked with NVDA; the pure parts, `AppMessage`'s text form, `BundleKeys.Text` and
  `TextJumps`, have unit tests in `tests/Axit.Tests`): `OverlayPanel` (the notices in the
  window, with the `Notice` record callers pass), `ControlArea` and `ListKeys` (a question's answers as a list or a
  field in the message field's place, with the number and page keys of every such list), `Sounds` (wave-out devices kept
  open for the life of the process; `Notice` for every window, `Edge`, one short low note, for a key that moves nothing), `EditPaging` (Page Up and Page Down in an edit control), `EditParagraphs`
  (Ctrl+Up and Ctrl+Down by paragraph, with Shift selecting), `EditKeys` (the KeyDown handler that gives an edit
  control both), `Announcer` (the UI Automation notification, AX-7.1), `TextJump` and `TextJumps` (a window's key pairs for the kinds of element in a text and the jump itself on an edit control, used by every notice text and by axDown's editor, AX-7.2), `StatusLayout`, the font
  handling (text size and the font picker), `BundleKeys` (AX-8), `CommandPalette` (the menu bar read into the
  palette's entries, AX-10), `WindowAccessibleObject` (the window's client area as its UI Automation rectangle, so
  that NVDA+End finds the status bar), `AxitMenu` (the Axit menu every window has, AX-9), `RecentMenu` (the "Open recent …" submenus, AX-9.5), `AppMessage` (the hand-over from a starting process to the open window of an app that keeps one window, one `WM_COPYDATA` message with a verb and paths, found through the Axit menu's window list; axMedia and axPreview, docs/axmedia/SPEC.md AM-1.3, 2026-10-22; `SendTo` a window the caller found, axStart's hidden one, docs/axstart/SPEC.md AS-1.2), `TranscriptView` with `TranscriptKeys` and `QuickKey` (the read-only transcript of a console with its single-key navigation, the keys handed in by the window from its table; from axClaude on its second use, axTerminal, docs/axterminal/SPEC.md §5),
  and the text editor shell: `TextEditorForm` (the window every text editor app is: the file, the menus, find, go to
  line, the status bar, the notices, the fonts, the update flow; axDown's `EditorForm` until axJSON, the second
  editor), `EditorApp` (what differs between the editor apps, as data: name, verb, settings file, file types, F1 text,
  guide, the `TextJump` pairs of its outline, its resources pages and its commands on the text; `ReadOnly` and
  `Refuse` make the shell a viewer, axPreview, docs/axpreview/SPEC.md AP-D4, 2026-10-22: no Edit menu, no Save and
  no history, Reload and Edit in the editor in the File menu, `l` for the line, the hand-over of a file received),
  `EditorKeys` (the shell's keys, the same in every editor window, and `Viewer`, the read-only set) and `EditorHost`
  (the entry point every editor app's `Run` is one call into); `AppHost` (every app's entry point, AX-1.7); `AxitForm`, the window base (AX-6.5); and `ConsoleForm` (2026-10-23), the window of a console app on it, which axClaude's and axTerminal's windows derive from: the transcript view, the field, the control area, the status bar pair, the pty bridge, the paced writes with one pump, the recording, the export, find, the signals and the music, with the hooks for what is the program's (docs/axterminal/SPEC.md AT-2).
- AX-6.3 A piece moves into a shared project only when a second app needs it, in a commit of its own, with the
  accessibility review; it moves unchanged where possible (B-3).
- AX-6.4 The notices and the question area are the bundle's one way of asking the user something, so that every
  window asks the same way: a notice has a title, a text, named buttons that Enter and Escape reach, the key line and
  the chime; a question in the message field's place is a list or a field named with the question, Enter answers,
  Escape cancels. An app that needs to ask uses these and does not draw its own dialog.
- AX-6.5 **The window base.** Every window of an app derives from `Axit.Forms/AxitForm`, which holds what the
  windows share and no app copies: the notice flow (`ShowNotice`, `CloseNotice`, `Fail`, `ShowError`; Alt, F10 and
  the window's shortcuts swallowed while a notice shows), `Announce`, the text font with its size keys and the
  Windows font picker (the choice said), the window's place and size, the settings saving and the notice for a
  settings file that could not be read, the Axit menu with the
  update check, the update notice and the install in place (AX-3), find in a text box, in a list, in a transcript
  and in a notice's text (one Find notice, one last text), Copy diagnostics (the account's name, its paths, e-mail addresses and ids replaced by `Redaction`, since it is meant for a public bug report, security audit S-20), `OpenUrl`, `OpenInExplorer`, the
  Windows folder picker (`ChooseFolder`), `Do` (a command's task with an escaped exception turned into a notice),
  the hand-over from a starting process of the app (`AppMessage`, answered at once and applied after), the submenus
  and the menu every window has (Options → Text, General and Developer; Help: `CreateTextMenu`,
  `CreateGeneralMenu`, `CreateDeveloperMenu`, `CreateHelpMenu`, each with the window's own items in their place;
  `Toggle` for a checked setting; `CreateExitItem` for the last item of the first menu), the
  window's pipe for the AI tools (`McpChannel`, MC-6.2), the window role for NVDA
  (AX-7.1) and the common part of the key switch (the text size keys everywhere, F1, the palette, Ctrl+S). A
  window adds its controls (the `Overlay` first) and fills in the hooks: the F1 text, what to hide for a notice and
  what to show after, the control the focus falls back to after one (`FocusTarget`), whether the chime follows a
  setting, what gets the font, how the new version starts it again (`RestartArguments`, `UpdateRestartText`), what
  to note of an update (`UpdateNoted`), its diagnostics lines, the control its announcements are raised on
  (`AnnounceSource`), its own palette entries (`PaletteEntries`, AX-10.5), what Ctrl+S does (`SaveCommand`;
  "Nothing to save here" by default), what it does with a hand-over (`TakeHandOver`), how it reports a settings file
  it could not read (`SettingsNotRead`; a notice by default, a system line in the console windows), and its own
  keys, as commands (docs/axit/app-kit.md AK-2; `ProcessWindowKey` for what must come before them). **The focus across a notice is one rule, the base's** (2026-10-23): when the first
  notice of a chain opens, the control that has the focus is remembered and `OnNoticeOpened` is called with it; a
  notice a notice's button opens, or one shown while one is open, continues the chain; when the last one closes,
  `OnNoticeClosed` is called and that control gets the focus back if it is still there and visible (the Actions
  list of axFile, the details of axGIT, a transcript), and `FocusTarget` otherwise (an answered question's list, the
  message field behind a question). No window decides where the focus goes after a notice. The
  settings each window needs are an abstract class in `Axit.Core`, `WindowSettings` (font, update check, place;
  `Save`), which every app's settings class derives from while its file keeps its own shape. The base is one class
  in partial files, one per section (the hooks and the menus, the notices, find, the fonts, the updates, the palette).
  It grew out of the editor shell on the third copy, axGIT (2026-10-18); axClaude's `MainForm` moved onto it the same
  day, its own extras (the sound setting for the chime, the reader's kept caret, system lines for the update's steps)
  as hooks; the notice and focus core was written again, and what every window still copied moved in, on 2026-10-23
  (`docs/axit/review-2026-10-23.md`, 5.3 and 6).

### AX-8 Keys

- AX-8.1 Every key an app handles is one of two kinds: **bundle-wide**, meaning the same in every window of Axit, or
  **window-specific**, meaning something in one window only. Each app's keyboard table in its spec marks every key
  with its kind.
- AX-8.2 Bundle-wide keys are defined once, in `Axit.Forms/BundleKeys`, a plain static class of constants, and every
  window uses those constants. The list: F1 the keyboard shortcuts; Tab,
  Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab and F6 move between the window's controls (and between a notice's text, field
  and buttons); Ctrl+F find, F3 and Shift+F3 next and previous, in a notice's text as well (AX-7.2); Ctrl+S save (what the window holds: the editors' file; axClaude and axTerminal, which have no
  document of their own, export their transcript to a text file with it, from the bottom of their first menu, so that
  Save and Save as keep their place in the editors' and the first menus share one order; a window with nothing to
  save says "Nothing to save here", axGIT adding the keys that do something, through the base's `SaveCommand`); Ctrl+Plus and Ctrl+Minus text size; Enter and Escape on a notice; Page Up and Page Down one screen in an
  edit control; Ctrl+Up and Ctrl+Down the start of the previous and next paragraph in an edit control, a paragraph
  starting at the first line or at a line with text directly after a blank line, and Ctrl+Shift+Up and
  Ctrl+Shift+Down the selection grown to there, as in Word (`EditParagraphs`; the EDIT control's own keys scroll a
  row and leave the caret where it is, as Notepad's do, which a screen reader, reading at the caret after the key,
  reports as the same text again); Ctrl+Shift+P the command palette (AX-10); Alt+F4 exit; Alt and F10 the menu. Everything else is window-specific.
- AX-8.3 Window-specific keys are defined once per window, in one table (`AxClaudeKeys`, `AxDownKeys`, `AxJSONKeys`, `AxGitKeys`, `AxTerminalKeys`, `AxFileKeys`, `AxMediaKeys`, `AxPreviewKeys`, `AxStartKeys`, `AxProcessKeys`, `AxTaskKeys`, `AxPassKeys`, `AxCalcKeys`, `AxClipKeys`), as constants
  with names that say what the key does. Changing a key is a change to that one line: the menu item and the key
  handler read the table, and the F1 text names the same keys and is changed with it. Single-letter quick keys
  (axClaude's `h`, `i`, `o`) are in the table too. The text editor shell's keys (`Axit.Forms/EditorKeys`: Ctrl+N new,
  Ctrl+O open, Ctrl+Shift+S save as, Ctrl+G go to line, Ctrl+Shift+W word wrap, Ctrl+W show in File Explorer) are
  defined once for every editor window, and each editor app's table includes them, so that one table per window
  still holds every key and the collision test sees them.
- AX-8.4 A unit test in `tests/Axit.Tests` fails when a window-specific key equals a bundle-wide key, and lists the
  keys that two windows use for different actions, so that a collision between apps is seen before it is felt. A
  window key may take a bundle key's place in one control only by the user's decision, listed by name in the test's
  `Overrides` with its spec line: axTerminal's command field, Ctrl+Up and Ctrl+Down for the history (AT-3.9, 2026-10-23).
- AX-8.5 A key is never handled anywhere but through its table, and no key is intercepted that NVDA uses (AX-7.4).
  The text a menu item shows for its key comes from the tables too (`BundleKeys.Text`: "Ctrl+Shift+S", "Shift+Escape"),
  so that a key changed in its table changes in the menus by itself; only Windows' own keys (Alt+F4, the edit
  control's Ctrl+Z, Ctrl+X, Ctrl+C, Ctrl+V, Ctrl+A) and descriptions ("Enter in the message field") are written out.
  A key of the number row ends in a full stop ("Ctrl+1."): NVDA speaks the shortcut directly before the position of
  the item in the menu ("1 of 9"), and the synthesizer runs "Ctrl+1  1 of 9" together into "Ctrl+11 of 9"; the full
  stop is a pause between the two.
- AX-8.6 Type-to-find in every list a user moves through by typing (2026-10-24; File Explorer's way, axFile's AF-3.6
  since 2026-10-22): letters typed within a second of each other build one prefix and select the first item that starts
  with it from the one in hand, round the end; the same letter again goes on to the next item starting with it; Shift
  with a letter searches the same way backwards (the user's ask; the case of the letters never matters, so a capital
  only says the direction); the edge earcon when no item starts with it; nothing is said but what NVDA reads of the
  selection. Up or Home on the first item and Down or End on the last play the edge earcon too, since the key moves
  nothing. Both are built into one control, `Axit.Forms/AxitList`, the list of every app (`Axit.Core/TypeAhead` the
  search, tested): a window makes an `AxitList` and has them, and gives it the names to search only when its items
  are not all names (`Names`). `ListTests` fails on a plain `ListBox` in an app; the lists that never take a typed
  letter are its named exceptions: axStart's and axClip's matches and the palette's, never focused, and a question's
  answers, whose letters are the screen's keys (axClaude D33).
- AX-8.7 F2 renames in place in every list whose items can be renamed (the user's rule, 2026-10-24; axFile's AF-4.2
  since 2026-10-20): a box named "New name" over the item, holding its name with the part that changes selected (a
  file's name before its extension); Enter takes it, and a name the window refuses is said and the box stays; Escape,
  or the focus leaving the box, keeps the old name; Ctrl+Z in the box undoes the typing. Built into `AxitList`
  (`BeginRename`, `EndRename`, `Renaming`), so that a window only says what renaming means for its items. In axFile's
  folder list, axTask's and axPass's navigators, and the item apps' field titles.

### AX-9 The Axit menu

- AX-9.1 Every window has an **Axit** menu (mnemonic A) between its own menus and Help, the same in every window: one
  class, `Axit.Forms/AxitMenu`, which the window constructs with its Command palette item, its two update items, its
  latest-release state and its notice method. In order: Command palette… (P, AX-10; the bundle's own command, so it
  sits in the bundle's menu, asked 2026-10-23); a separator; New window ▸ and Recently closed ▸; a separator; Open
  windows ▸, Open windows by type ▸ and Close all windows; a separator; AI tools ▸ (I, `mcp.md` MC-6.1: the bundle's
  own settings live here, an app's Options hold only the app's); Check for updates, Update to (while a newer
  release is known) and About ▸ (AX-9.9). The lists are filled each time the menu opens, so that they show the
  windows and the files of that moment.
- AX-9.2 **New window ▸** (N): one item per app of the bundle (`Dispatch.Apps`), "axClaude", "axDown", with the letter
  after ax as the mnemonic. It starts `Axit.exe <verb>` with no arguments: a new process and a new window (B-2), the
  app starting as it does from its Start menu entry. The apps stand in four groups, a separator between them
  (2026-10-24, the user's choice of a flat list over a submenu per group): Claude and code (axClaude, axTerminal,
  axGIT), files (axFile, axDown, axJSON, axPreview, axMedia), one's own lists (axTask, axPass), utilities (axStart,
  axClip, axCalc, axProcess). Every list of apps in the windows has this order and these separators: New window, Open
  windows by type, axFile's Open in Axit (`AxitMenu.AddApps`). A new app is one line in `Dispatch.Apps`, with its
  group, what it opens and whether Show in offers it; every list, Show in (AX-9.10) and Recently closed (AX-9.4)
  follow from that line.
- AX-9.3 **Open windows ▸** (O): every open window of the bundle's apps except this one, the most recently used first.
  The order is the one Windows keeps its top-level windows in, which is Alt+Tab's, so nothing is stored and Alt+A, O,
  1 is always the window the user was in before this one. **Open windows by type ▸** (B) holds one submenu per app,
  "axClaude (2)", "axDown (1)", the number being its open windows: that app's windows in the same order, this window
  included and checked, so that the list also says where the user is.
- AX-9.4 **Recently closed ▸** (R): the last ten files and folders a window of any app held when it closed or replaced
  them (axDown's file, axClaude's project folder), the newest first, each app and path once, those that still exist
  (checked as the menu opens for a path on a drive of this computer only, since a share or a mapped network drive
  can take the network's timeout to answer, which no menu may wait for; a path elsewhere is listed as it is and
  found missing when chosen; `Axit.Core/LocalPaths`, tested, 2026-10-23; the apps' recent lists follow the same
  rule, AX-9.5); Enter starts that app on it in a new window. An app that holds no file or folder (axProcess, axCalc,
  axClip, axStart) is listed by its name alone when its window closes, once, and Enter starts it (2026-10-24). The
  window base does it for every window as it closes (`AxitForm.PathInHand`: the file or folder, empty for an app that
  holds none, which is the default, so that a new app is listed without a line of its own; nothing for a window with
  nothing to open again, an untitled text or axFile at This PC); a window that swaps its file or folder remembers the
  old one itself. A background app's window (axStart, axClip) hides on Alt+F4 and is listed when Quit ends it. The list is one file for the bundle, `%APPDATA%\Axit\recently-closed.json`
  (AX-5.1), written whole by the window that closes or swaps (`Axit.Core/RecentlyClosed`, through `SettingsFile`; two
  windows closing in the same instant leave the last writer's list). Nothing else about windows is stored.
- AX-9.5 A window's or a file's item is its number, a full stop and a title, "1. notes.md (C:\notes\notes.md) - axDown":
  the number is the mnemonic (1 to 9, then a to z), so that Alt+A, O, 1 or Alt+A, R, 1 acts without an arrow key, and
  the full stop is a pause before the name (AX-8.5). A window's title is the window's own; a closed item's is the
  file's or folder's name, its full path in parentheses, and the app. The apps' own recent lists have the same shape,
  "1. myproject (C:\src\myproject)" (axClaude FR-8.5, axDown AD-4.1). Enter brings a window to the
  front, restored when it was minimized. A list with nothing in it holds one item that says so and does nothing, enabled
  ("No other open windows", "No open axDown windows", "Nothing recently closed"; the apps' own lists likewise, "No
  recent folders", "No recent files"): WinForms skips disabled items when the keyboard moves through a menu, so a
  submenu of only a disabled item opens with nothing selected and leaves the screen reader with nothing to read.
- AX-9.6 The windows are the visible, unowned, titled top-level windows of every process named as this one, told
  apart by the end of their title (" - axClaude", " - axDown"; AD-10, axClaude's `<folder> (<path>) - axClaude`); a window of
  none of the apps (the bundle's own message box, AX-1.4) is listed nowhere. The lists are pure code in
  `Axit.Core/AxitWindows` and `Axit.Core/RecentlyClosed`, unit tested; only the listing and the switch (user32
  `EnumWindows`, `SetForegroundWindow`) and the menu itself are in `AxitMenu`.
- AX-9.7 Only Axit's windows are listed; Alt+Tab stays the way to everything else. Alt+A is a menu mnemonic, not a key
  of the tables (AX-8); the F1 texts name it. A separator carries no text and NVDA passes over it; the groups are
  told apart by their names, not by headers, which would be one more stop on every pass through the menu.
- AX-9.8 **Close all windows** (C) posts the close command Alt+F4 sends (`WM_SYSCOMMAND` `SC_CLOSE`) to every other
  open Axit window and then to this one; each window closes as it does on Alt+F4 and asks there what it would ask
  then (axDown's Save changes?, axClaude's Close axClaude? while Claude works), so nothing is lost without a question.
- AX-9.9 **Check for updates (Axit n)…** (U) and **Update to Axit n…** (T, while a newer release is known) are the
  window's own update items (AX-3.1), placed here by the window. **About ▸** (A) is the bundle's About in sections,
  the same in every window (`Axit.Core/BundleAbout`), each shown as a notice of the window: **Version** (V; the
  version, the apps, the latest release as the window knows it, the releases page, the settings and logs folders),
  **Licence** (L; MIT, and the warranty and liability disclaimer in full words; the LICENSE file is the binding text),
  **Attribution** (A; the maker, the Anthropic note, what Axit is built with), **Axit guide** (G; `docs/axit/README.md`,
  the zip's guide, embedded by `Axit.Core`: install, start, update, remove; its headings take the window's heading
  keys, AX-7.2), and **Axit on GitHub** (H; the repository page, `UpdateCheck.RepositoryPage`, in the browser).
- AX-9.10 **Show current folder in ▸** (2026-10-24, one builder, `AxitForm.CreateShowInMenu`): the window's File
  Explorer item (Ctrl+W), then the apps that work in a folder (`App.ShowIn`: axClaude, axTerminal, axGIT, axFile, in
  that order) but the window's own, each a new window on the file or folder in hand: axClaude and axTerminal on a
  file's folder, axGIT on the repository that holds it, axFile on the folder or with the file selected
  (`Dispatch.PathFor`). The windows that hold a folder or a file have it: axClaude and axTerminal ("Show current folder
  in"), axGIT ("Show current repository in"), axFile ("Show current folder in"), the editors and axPreview ("Show
  current file in") and axMedia ("Show current file in"). With nothing in hand the window says why ("No project
  folder", "No repository", "No file yet", the empty queue).

- AX-6.6 **Menus are the standard ones.** A menu bar item's drop-down is the one WinForms makes for it; no window
  assigns a drop-down of its own (a `ContextMenuStrip` or any `ToolStripDropDown` set as `DropDown`). A hand-made
  drop-down has its owner item cleared when it closes, after which the bar no longer leaves auto-expansion on Escape,
  and Left and Right open the neighbours' menus instead of moving along the bar (axFile, 2026-10-21). `MenuTests` in
  `tests/Axit.Tests` scans the sources for the construct. A key that must work inside an open menu goes in a context
  menu subclass (axFile's `ItemMenu`), which has no owner item. In menu mode WinForms routes the keys to the menu
  bar, which hands the ones it does not use up to the window's `ProcessCmdKey`; the window base gives a key that
  comes from a `ToolStrip` to the menu alone, never to the window's keys, F1 and the palette included (axFile's
  Right opened the entry in the list instead of moving along the bar, 2026-10-22; F1 opened the shortcuts over an
  open menu, 2026-10-23).

### AX-10 The command palette

The user's ask of 2026-10-21, specified 2026-10-22: one key in every window finds any of the window's commands by
name, so that a command is reached without knowing its menu or its key, and its key is learnt on the way.

- AX-10.1 **Ctrl+Shift+P** (`BundleKeys.CommandPalette`, AX-8.2) in any window opens the palette as a notice of the
  window (AX-6.4, AX-7.2), titled "Command palette": one field named "Search commands" with the caret in it, and
  under it a list of the matches named "Commands, n" that never takes the focus, axStart's shape (docs/axstart/SPEC.md
  AS-3.1, AS-D2). Axit → Command palette… (the first item of the Axit menu, AX-9.1) opens it too, so that it is found
  from the menus. While another notice shows, or in menu mode (AX-6.6), the key does nothing.
- AX-10.2 **The entries** are the window's menu bar as it stands when the palette opens, the Axit menu and Help
  included: every enabled item that does something when chosen, in menu order. An entry reads its name, then its menu
  path, then its key when it has one, then "checked" for a checked item: "Save as, File, Ctrl+Shift+S", "Larger text,
  Options, Text, Ctrl+Plus", "Word wrap, Edit, Ctrl+Shift+W, checked". The name is the item's text without its
  mnemonic ampersand and its trailing dots. Left out: separators, items that only open a submenu, disabled items, and
  the lists a menu fills only as it opens (Open recent, Open windows, Recently closed), whose state the palette would
  have to guess; a window marks such a submenu where it attaches its filling (`CommandPalette.FilledOnOpening`), and
  the fixed items such a submenu also holds (axFile's Add or remove this folder under Favorites, This PC and Eject
  under Drives) are left out with it, their keys still working. A menu whose items change their text or check mark as
  it opens (axFile's Undo named after what it would undo, axTerminal's Switch shell with the shell in use checked)
  is marked where its refreshing is attached (`CommandPalette.RefreshedOnOpening`), and the palette runs the
  refresh before reading (until 2026-10-23 such an item read as it last stood). The palette reads
  the menus and keeps no list of its own: a command added to a menu is in the palette.
- AX-10.3 **Matching** is axStart's (AS-2.2), moved to `Axit.Core` on this second use: the typed text, trimmed, case
  and accents ignored, against the entry's whole line; an entry matches when its line holds every typed word (`text
  larger` finds "Larger text, Options, Text"). The order: a line that starts with the first word, then a line with a
  word that starts with it, then any other match; within each, menu order. An empty field lists every entry.
- AX-10.4 **The keys in the field** are axStart's (AS-3.2, AS-3.3): typing refills the list and selects the first
  match, and 300 ms after the last change one notification says it and the count, "Save as, File, Ctrl+Shift+S, 1 of
  3", or "No commands match xyz"; Down and Up, Page Down and Page Up, Ctrl+Home and Ctrl+End move the selection and
  say it; at an end, where nothing moves, the edge earcon and then the match again, "Save as, File, Ctrl+Shift+S, 1
  of 1", so that the last thing heard is the position and not the typed text (asked 2026-10-23; the earcon alone had
  left NVDA's reading of the field as the last thing heard), and with nothing matching the earcon and "No commands
  match …"; the field's editing keys stay its own. **Enter** closes the palette, gives the
  focus back to the control that had it, and then runs the selected entry as a click on its menu item would, so that
  the command acts where the user was; a checked setting run this way says its new state, "Word wrap, not checked",
  since no menu shows it. Enter with nothing matching says "No commands match …" and the palette stays. **Escape**
  closes the palette with the focus back and nothing run.
- AX-10.5 **A window's own entries.** A window may add entries after the commands through one hook of the window base,
  each a name and an action. axFile adds the folder's entries, "notes.md, file", "docs, folder", and Enter on one acts
  as Enter on it in the folder list (docs/axfile/SPEC.md). Other windows add none.
- AX-10.6 The palette is the window base's (`AxitForm`, AX-6.5) with the matching in `Axit.Core` (unit tested); no app
  writes code for it beyond AX-10.5. Every window's F1 text names Ctrl+Shift+P. axGIT's Push, which held
  Ctrl+Shift+P, moved to Ctrl+Shift+H (docs/axgit/SPEC.md AG-5.4).

### AX-7 Accessibility rules every app follows

These are the rules of `docs/axclaude/SPEC.md` that are not about Claude; app specifications refer to them by number.

- AX-7.1 Every control has an `AccessibleName`; every action works from the keyboard; menu items have mnemonics and
  shortcuts. Native Win32 controls (EDIT, menus, status bar, `ListBox`) are used because NVDA reads them reliably;
  no WPF, no web view, no owner-drawn controls.
- AX-7.2 No second window of the app's own: questions, errors and help texts are notices drawn inside the window
  (`OverlayPanel`), never a `Form` or a `MessageBox` once a window exists. A notice's button may open the next notice
  (the update's progress, an error, axDown's changed-on-disk question after Save), which then stays in view. The
  Windows folder, file and font pickers, and the shell's own dialogs and menu an app hands an entry to (axFile's Open
  with, Properties and Windows menu, docs/axfile/SPEC.md AF-4.7), are the only separate windows. The pickers are
  shown through the window base's `ShowSystemDialog`, which marks the window while one is up (`DialogShowing`),
  since a dialog's closing activates the window before the command that opened it has gone on (2026-10-23: the
  editors said "Reloaded" in the middle of Save as, and axFile read its folder again).
  A notice's text takes the heading letters in every window (`TextJump.HeadingLetters`, the default of
  `OverlayPanel.Jumps`; the letters of axClaude's transcript): h and Shift+H the next and the previous heading, 1 to 6
  and Shift with them a heading of that level, by the text's Markdown headings; an editor's notices take the editor's
  own Ctrl+letter pairs as well (every kind of axDown's section 9, JSON's kinds in axJSON), so that its convention
  holds in its notices too. The jump is `TextJumps`, the code the editors use on their own text; the result is
  announced on the notice, "No next heading" and the like when there is none, and the caret stays. A notice's text
  is searched with the bundle's find keys (AX-8.2): Ctrl+F puts the Find notice in the notice's place, and Find next
  brings the notice back with the match selected and the focus in its text (Cancel brings it back as it was); F3 and
  Shift+F3 find the next and the previous match there, "Not found" when there is none. The F1 texts are written under
  Markdown headings, one per group of keys, with a first line naming the text's own keys, so that the letters and the
  find walk them (asked 2026-10-23).
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
  Windows is when the app starts, and a high contrast theme applies as it always has. No app sets a colour of its own,
  with one exception: axMedia's video area is black (AM-2.4), the colour of a video surface everywhere, so that a
  picture's letterbox is not the window's colour (decided 2026-10-23).

## 5. Architecture

### 5.1 Projects and references

```
src/Axit/            executable (net10.0-windows): Program (crash handling, version, log start, dispatch), icons
src/Axit.Core/       class library (net10.0), no WinForms, unit tested
src/Axit.Forms/      class library (net10.0-windows), WinForms pieces the apps use, among them the text editor shell
                     (TextEditorForm, EditorApp, EditorKeys, EditorHost)
src/AxClaude/        class library (net10.0-windows): AxClaudeApp, MainForm, ... (axClaude's window)
src/AxClaude.Core/   class library (net10.0): Vt, Transcript, Pty, Privacy, AppSettings (axClaude's pure code)
src/AxDown/          class library (net10.0-windows): AxDownApp (its EditorApp), AxDownKeys, HelpText; the window is
                     the shell's, and its pure code (TextDocument, EditorSettings, MarkdownOutline) is Axit.Core's
src/AxJSON/          class library (net10.0-windows): AxJSONApp (its EditorApp), AxJSONKeys, HelpText (axJSON's app side)
src/AxJSON.Core/     class library (net10.0): JsonScanner, JsonParser, JsonOutline, JsonFormat (axJSON's pure code)
src/AxGit/           class library (net10.0-windows): AxGitApp, GitForm, AxGitKeys, HelpText (axGIT's window)
src/AxGit.Core/      class library (net10.0): GitRunner, GitStatus, GitLog, GitBranches, GitSettings (axGIT's pure code)
src/AxTerminal/      class library (net10.0-windows): AxTerminalApp, TerminalForm, AxTerminalKeys, HelpText (axTerminal's window)
src/AxTerminal.Core/ class library (net10.0): ShellLauncher, TerminalModel, TerminalSettings, … (axTerminal's pure code)
src/AxFile/          class library (net10.0-windows): AxFileApp, FileForm, AxFileKeys, HelpText (axFile's window)
src/AxFile.Core/     class library (net10.0): FolderListing, FileOperations, Archives, FileSettings, … (axFile's pure code)
src/AxMedia/         class library (net10.0-windows): AxMediaApp, MediaForm, AxMediaKeys, HelpText (axMedia's window)
src/AxMedia.Core/    class library (net10.0): MediaPlayer, PlayQueue, MediaFolder, MediaSettings (axMedia's pure code
                     and its one seam to Windows' media stack); MediaFiles and NaturalOrder are Axit.Core's
src/AxPreview/       class library (net10.0-windows): AxPreviewApp (its EditorApp, read-only), AxPreviewKeys, HelpText;
                     the window is the shell's
src/AxPreview.Core/  class library (net10.0): TextSniff (whether a file reads as text; axPreview's pure code)
src/AxStart/         class library (net10.0-windows): AxStartApp, StartForm, AppsFolder, AxStartKeys, HelpText
                     (axStart's window on HotkeyForm, and the shell's Applications folder)
src/AxStart.Core/    class library (net10.0): ProgramEntry, ProgramMatch, StartSettings (axStart's pure code)
src/AxProcess/       class library (net10.0-windows): AxProcessApp, ProcessForm, AxProcessKeys, HelpText (axProcess's window)
src/AxProcess.Core/  class library (net10.0): ProcessTable, ServiceManager, StartupList, SystemInfo, ProcessSettings (axProcess's pure code)
src/AxTask/          class library (net10.0-windows): AxTaskApp, TaskForm, AxTaskKeys, HelpText (axTask's window)
src/AxTask.Core/     class library (net10.0): ItemFolder, Changes, Links, TaskRules, SmartList, TaskSettings (axTask's pure code)
src/AxPass/          class library (net10.0-windows): AxPassApp, PassForm, AxPassKeys, HelpText (axPass's window)
src/AxPass.Core/     class library (net10.0): KeePassCli, PassTree, EntryFields, PassSettings (axPass's pure code)
src/AxCalc/          class library (net10.0-windows): AxCalcApp, CalcForm, AxCalcKeys, HelpText (axCalc's window)
src/AxCalc.Core/     class library (net10.0): Calculator, NumberText, CalcEntry, CalcSettings (axCalc's pure code)
src/AxClip/          class library (net10.0-windows): AxClipApp, ClipForm, AxClipKeys, HelpText (axClip's window, on HotkeyForm)
src/AxClip.Core/     class library (net10.0): ClipHistory, ClipEntry, ClipPolicy, ClipSettings (axClip's pure code)
tests/Axit.Tests, tests/AxClaude.Tests, tests/AxJSON.Tests, tests/AxGit.Tests, tests/AxTerminal.Tests, tests/AxFile.Tests, tests/AxMedia.Tests, tests/AxPreview.Tests, tests/AxStart.Tests, tests/AxProcess.Tests, tests/AxTask.Tests, tests/AxPass.Tests, tests/AxCalc.Tests, tests/AxClip.Tests
```

References point one way: `Axit` references every app; an app's window project references its own `.Core`,
`Axit.Core` and `Axit.Forms`; an app's `.Core` references `Axit.Core` and never `Axit.Forms`; the shared projects
reference nothing of the apps. `tests/Axit.Tests/LayoutTests` reads the project files and fails when this is broken,
and `InventoryTests` fails when an app lacks one of its places (5.3 M1, `adding-an-app.md`).

The .NET version is one property, `AxitDotNet` in `Directory.Build.props`, from which every project's
`TargetFramework` is built (`$(AxitDotNet)` for the pure projects, `$(AxitDotNet)-windows` for WinForms), and the SDK
band is `global.json`, which the workflows read too (2026-10-23). .NET 10 is an LTS release supported until
November 2028; the move to the next LTS is those two files and a run of the tests (`docs/axit/longevity.md`).

### 5.2 Documents

```
CLAUDE.md               the map of the bundle and its rules (short); each project has its own CLAUDE.md with its notes
README.md, CONTRIBUTING.md, SECURITY.md   the repository's own pages (AX-4.5)
docs/axit/              SPEC.md (this), README.md (the zip's guide: install, the apps, uninstall), adding-an-app.md, plan.md,
                        nvda-test-record.md (every NVDA pass, by step), axfile-split.md (the inventory of the split),
                        review-2026-10-23.md (the review of the code and documents), inventory-2026-10-24.md (the copies and drift, and what is open) and longevity.md (the practices for the years ahead)
docs/<app>/             SPEC.md, user-guide.md (embedded by the app's project), nvda-test-plan.md, for each app of section 3;
                        docs/axclaude also claude-screens.md, docs/axdown also tutorial.md
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
  rules (AX-7), which app specs cite by number. In axClaude's code and documents, "SPEC.md" means
  `docs/axclaude/SPEC.md`.
- M6 One version, one changelog with app prefixes, one release.
- M7 An app's texts belong to the app: its user guide is embedded by its own project, its F1 text lives in its
  `HelpText`, its usage text in its `StartupOptions`.
- M8 A new app follows `docs/axit/adding-an-app.md`.
- M9 Keys live in tables (AX-8): one bundle-wide table in `Axit.Forms`, one table per window; a key is changed in its
  table and nowhere else.

### 5.4 The Windows APIs depended on

Every call into Windows outside WinForms is behind one file, listed here with what it is for and what to do when
Windows retires it (`docs/axit/longevity.md` 3.6; the list is checked against Windows' deprecation notices once a
year, section 5 of that page). WinForms itself (the EDIT control, the menus, the status bar, the ListBox, the common
dialogs) is .NET's, shipped inside the executable, and every project is marked as Windows code for the analyzer
(`Directory.Build.props`).

- **ConPTY**, `kernel32` (`src/Axit.Core/Pty/PtyHost.cs`): `CreatePseudoConsole`, `ClosePseudoConsole`,
  `CreatePipe`, `CreateProcess` with a thread attribute list, `GetStdHandle`, `SetStdHandle`, `WaitForSingleObject`,
  `GetExitCodeProcess`, `TerminateProcess`, `CloseHandle`. The console axClaude and axTerminal run their programs
  in. Windows 10 1809 and later: the one API without a fallback, hence the Windows requirement (axClaude's FR-13.1
  names it when it is missing).
- **`GetACP`**, `kernel32` (`src/Axit.Core/TextDocument.cs`): the system code page, for a text file that is neither
  UTF-8 nor UTF-16 (axDown's AD-3.1). Stable; `Encoding.Default` is no replacement, being UTF-8 on .NET.
- **`StrCmpLogicalW`**, `shlwapi` (`src/Axit.Core/NaturalOrder.cs`): Explorer's order of names, numbers compared as
  numbers, for axFile's entries and axMedia's queue. Stable; the fallback is a comparer of our own under the same
  tests.
- **The EDIT control's messages**, `user32` (`SendMessage` and `GetCaretPos` in `src/Axit.Forms/EditPaging.cs`,
  `EditParagraphs.cs` and `TranscriptView.cs`): line and caret positions and the scrolling of the views. As old as
  Windows; WinForms' TextBox is the same control.
- **`FlashWindowEx`**, `user32` (`src/Axit.Forms/ConsoleForm.Signals.cs`): the taskbar flash when Claude or the
  shell wants attention while the window is in the background. Stable; without it the sounds remain.
- **The window list**, `user32` (`EnumWindows`, `GetWindow`, `GetWindowText`, `GetWindowTextLength`,
  `GetWindowThreadProcessId`, `IsIconic`, `IsWindowVisible`, `ShowWindow`, `SetForegroundWindow`, `PostMessage` in
  `src/Axit.Forms/AxitMenu.cs`): the Axit menu's list of the bundle's windows and the switch to one. Stable.
- **`WM_COPYDATA`**, `user32` (`SendMessageTimeoutW`, `AllowSetForegroundWindow`, `GetWindowThreadProcessId` in
  `src/Axit.Forms/AppMessage.cs`): the hand-over from a second start to the open window of axMedia or axPreview
  (AX-7.2). Stable.
- **`RegisterHotKey`**, `user32` (with `UnregisterHotKey`, `GetForegroundWindow`, `SetForegroundWindow`,
  `FindWindowExW`, `IsWindow`, `IsWindowVisible`, `GetKeyState`, `GetWindowThreadProcessId` in
  `src/AxStart/StartForm.cs`): axStart's system-wide key and the foreground given back (AS-4.1). Stable.
- **`waveOut`**, `winmm` (`waveOutOpen`, `waveOutPrepareHeader`, `waveOutWrite`, `waveOutReset`,
  `waveOutUnprepareHeader`, `waveOutClose` in `src/Axit.Forms/Sounds.cs`): the earcons, each a buffer queued behind
  the one playing. Legacy, kept by Windows for compatibility (`PlaySound` and .NET's `SoundPlayer` rest on it); the
  fallback is WASAPI through COM in the same file.
- **`SHFileOperationW`**, `shell32` (`src/AxFile.Core/FileOperations.cs`): copy, move, rename, delete to the Recycle
  Bin, with the shell's own progress, conflicts and undo (axFile's AF-4). Superseded by `IFileOperation` (Vista),
  still shipped; the fallback is `IFileOperation` through COM, in the same file.
- **The shell's folders and menus**, `shell32`, `shlwapi`, `user32` (`SHGetDesktopFolder`, `SHParseDisplayName`,
  `SHBindToParent`, `SHGetKnownFolderIDList`, `SHOpenWithDialog`, `ShellExecuteExW`, `StrRetToBufW`, the
  interfaces `IShellFolder` and `IContextMenu`, and the menu functions `CreatePopupMenu`, `TrackPopupMenuEx`,
  `GetMenuItemCount`, `GetMenuItemID`, `DestroyMenu`, `PostMessageW` in `src/AxFile/Shell.cs`): an entry's Windows
  menu, Open with, Properties, Eject (AF-4.7). Stable: the interfaces Explorer itself is built on.
- **`shell:AppsFolder`**, `shell32` (`SHCreateItemFromParsingName`, `SHGetKnownFolderPath`, the interfaces
  `IShellItem` and `IEnumShellItems` in `src/AxStart/AppsFolder.cs`): the Start menu's programs for axStart
  (AS-2.1). The folder's parsing name is not promised by Microsoft; the fallback is the Start menu's shortcut folders
  (the user's and the common one), which lose the Store apps.
- **`SetCurrentProcessExplicitAppUserModelID`**, `shell32` (`src/Axit/Program.cs`): one taskbar group per app
  (AX-1.6). Stable.
- **MFPlay**, `mfplay` (`MFPCreateMediaPlayer`, the interfaces `IMFPMediaPlayer`, `IMFPMediaItem` and
  `IMFPMediaPlayerCallback` in `src/AxMedia.Core/MediaPlayer.cs`): axMedia's playback. Its documentation carries a
  deprecation notice while it is still shipped, Windows 11 included; the fallback is the Media Session (AM-D5),
  behind the same seam.
- **UI Automation notifications** (`AccessibleObject.RaiseAutomationNotification`, WinForms over
  `UiaRaiseNotificationEvent`, in `src/Axit.Forms/Announcer.cs`): everything the apps say (AX-7.3). Windows 10
  1709 and later; NVDA has read them since 2018.

## 6. Design decisions

- **B-1 One executable with a verb per app.** A self-contained .NET executable carries the runtime, about 70 MB; one
  per app would multiply the download, and two executables sharing a runtime folder would need a multi-file publish
  or an installed .NET, which the project has always avoided. One executable keeps the zip, the installer, the Apps
  entry and the updater as they are. A later app is another verb.
- **B-2 Apps are class libraries with one entry point.** `Axit`'s `Program` stays the only `Main`; each app is
  `<App>App.Run(args)`. One process shows one window; that keeps the apps apart for the taskbar, Alt+Tab and NVDA.
- **B-3 Shared code moves on the second use, unchanged.** Nothing is generalised on a guess, and moving code as it is
  kept axClaude's behaviour identical when it became one app of the bundle (G5).
- **B-4 Names.** The bundle is Axit; the apps keep their names; Start entries are `Axit <App>` so that the bundle
  name groups them and the app name is still a word that search matches.
- **B-5 Settings and logs per app, under `Axit`.** One folder for the bundle, one file per app, so an app's settings
  code knows only its own record. The old axClaude settings file is copied, not moved, so a return to 1.7.0 still
  finds it.
- **B-6 A bridge release before the rename.** The 1.6.0 updater finds a release's zip by its `-win-x64.zip` ending
  but refuses a download without `AxClaude.exe`. 1.7.0 relaxes that check, so every installed copy can take the 2.0.0
  update instead of failing with an error.
- **B-7 The GitHub repository is renamed to `Axit` with the 2.0.0 release.** GitHub redirects the old web and API
  addresses, so installed 1.7.0 copies still find the releases; `UpdateCheck.Repository`, the installer and the
  guides then name the new address.
- **B-8 `axit` without an argument starts axClaude on the current folder.** That is what `axclaude` does today, and a
  console inside a project is where the command is typed. The usage text is one `--help` away.
- **B-9 No silent default program.** Windows protects the user's choice of default program; an installer cannot set
  it without a prompt. axDown is registered as a program for its file types and offered in the right-click menu; the
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
  every window asks the same way (AX-6.4). They moved to `Axit.Forms` unchanged when axDown needed them (2.0.0).
- **B-14 An Axit menu instead of tabs.** One window holds one file or one conversation (B-2), and axJSON will keep it
  so. Tabs in one window were considered for an editor of several files (2026-10-18) and dropped: a tab needs a
  modified state of its own, a close of its own against the window's, a title and a status bar that follow it, and
  keys whose meaning changes with the tab under the user's fingers, the thing a screen reader user feels first (B-12).
  The Axit menu (AX-9) gives the one thing tabs give, a list of what is open in one place, by recency and by app;
  Windows' own order of its windows is the recency, so nothing is stored or kept in step, and Alt+Tab is unchanged.
- **B-15 Local first; AI help is one shared, optional service (2026-10-18).** The apps are tools over files, folders,
  repositories and a shell on the user's machine, and they stay useful on a plane or behind a firewall: every app but
  axClaude works entirely offline, every network connection can be disabled without loss of the app's utility, and
  the update check is the only call a window makes on its own (G6). AI-based editing and feedback, asked for first as
  a feature of axDown, are not built into any one app: they are a shared service every app can call from inside its
  window to get things done there, written once and optional, so that the apps stay plain and complete without it.
  The bundle is to have a shared database as well, which the service and the apps share. The service's design (how it
  reaches Claude, what it may change in a window, the database) is a specification of its own, not yet written
  (`docs/axit/plan.md`).

## 7. Status

- 2.0.0 (2026-10-02): the bundle. `Axit.exe` with axClaude unchanged inside it and axDown new; `Axit.Core` and
  `Axit.Forms`; the installer, publish and release scripts for the bundle; the repository renamed `KyleKeane/Axit`.
  Preceded by axClaude 1.7.0, the bridge (B-6). Both apps checked with NVDA by the user before the release.
- 2.1.0: the key tables (`AxClaudeKeys`, `AxDownKeys`, `BundleKeys`) with the collision test (AX-8); heading
  navigation in axDown. 2.1.1: axDown's window spoken on opening (`AccessibleRole.Window`).
- 2.2.0: the update notice with Update and restart, Update and close, and a progress bar for the download.
- 2.3.0: the in-place install with one bar for download and install and the restart by the app itself (AX-2.8,
  AX-3.2); the user ran the full path from an older build with NVDA. 2.3.1: the documents pruned after the transition.
- 2.9.0 (2026-10-18), developed on the branch `axit` and checked with NVDA by the user: `MarkdownOutline` and
  `TextJumps` shared for axDown's letters and every notice text (AX-7.2); the Axit menu in every window (AX-9, B-14)
  with About in sections and the embedded Axit guide; axClaude's Session menu and axDown's menus in the same shape;
  an enabled item in every empty list (AX-9.5).
- 2.9.1 (2026-10-18): axJSON, raw mode (`docs/axjson/SPEC.md`), on the text editor shell shared with axDown the same
  day (`TextEditorForm`, `EditorApp`, `EditorKeys`, `EditorHost`; AX-1, AX-2, AX-6.2, AX-8.3); released at the user's
  ask before its NVDA pass.
- 2.10.0 (2026-10-18): every numbered list item reads "N. name (path)" (AX-9.5, FR-8.5, AD-4.1); axJSON marked beta
  until its NVDA pass.
- 2.11.0 (2026-10-18), developed on `axit`: axClaude's handling of the feedback drafts Claude Code queues by itself
  (the review comes up like a question, `/feedback` lists the drafts, a screen's keys from anywhere in its list, the
  edit field, Ctrl+Q to read a question or screen again; axClaude D33), the first menu of every window in the standard
  order of a File menu with axClaude's Save conversation as renamed Export conversation (AX-8.2, AD-4.1); the draft
  flow and Ctrl+Q heard under NVDA by the user, the rest covered by recordings and tests.
- 2.12.0 (2026-10-18), developed on `axit`: axGIT, the fourth app, as a first draft (`docs/axgit/SPEC.md`; section
  3), reached from the other windows' "Show current … in ▸" submenus; the window base `AxitForm` (AX-6.5) with the
  editor shell, axGIT and axClaude's `MainForm` on it. Heard under NVDA by the user: the notices in axClaude and
  axDown on the base, axGIT's start, views, diff, staging and notices, and the way to and from it; axGIT's commit,
  discard, switch and remote commands await their pass.
- 3.0.0 to 3.2.2 (2026-10-19 to 2026-10-21): axTerminal with the console pieces shared (AX-6.1), the in-place update
  clearing old remnants (AX-2.8), one Open with Axit submenu (AX-2.3); axFile (3.1.0) with its operations, Undo and
  pane; the delayed question focus in axClaude (3.2.1); the editors' undo and redo (3.2.2). `CHANGELOG.md` has each.
- 3.3.1 (2026-10-22), developed on `split`: the split of axFile (`docs/axit/axfile-split.md`): axMedia, the player
  with one queue, and axPreview, the viewer as the editor shell's read-only mode, as apps of their own with a
  hand-over from a starting process to the open window (`Axit.Forms/AppMessage`, AX-6.2); axFile lean, Enter the
  user's default program, the pane its Actions list; `MediaFiles`, `NaturalOrder` and the find in a list shared
  (AX-6.3); the dispatcher's media rule (AX-1.2) and the installer's Open with registrations for both apps (AX-2.3).
  Every plan passed under NVDA by the user the same day (`docs/axit/nvda-test-record.md`); 3.3.0 was tagged from the
  same work and never published (a test the Release build refused).
- 3.4.0 (2026-10-22): axStart, the program launcher (section 3, `docs/axstart/SPEC.md`), its NVDA pass still owed;
  `test.ps1` running the tests as CI does (AX-4.4); the repository's pages (AX-4.5).
- Unreleased (2026-10-23), developed on `palette`: the command palette in every window (AX-10); the heading letters
  and Ctrl+F in every notice's text (AX-7.2); the review of the code and documents of 2026-10-18 to 2026-10-22
  (`docs/axit/review-2026-10-23.md`) with its fixes, the tests run without the globalization data as the program
  runs (AX-6.1, `Accents`), and the practices for the years ahead (`docs/axit/longevity.md`). The same day, the
  rewrites the review asked for (one console window for axClaude and axTerminal with one settings class, AX-6.1;
  the notice and focus core of the window base as one rule, AX-6.5; axClaude's wait as one record; axFile's
  operation runner and marks; axTerminal's prompt rows by identity; the smaller ones), the duplication moved into
  the shared projects, the tests asked for, and the longevity items that needed no account (the Windows APIs
  listed in 5.4, the actions pinned, the installer off the GitHub API and putting the commands on the PATH, Claude
  Code's version logged, the release rules, the lock files). Not yet checked with NVDA (the review's section 9).
