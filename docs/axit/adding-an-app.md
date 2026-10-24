# Adding an app to Axit

The checklist for a new app of the bundle, written against what axDown took (2026-10-02). An app
is a window with a verb; everything else below exists so that the app can be built, changed and released without the
rest of the bundle in context (`docs/axit/SPEC.md` 5.3). Work in this order: the spec first, then the pure code with
its tests, then the window, then the installer.

## 1. Decide

- The name (`AxThing`), the verb `Axit.exe` takes (`thing`), the console command (`axthing`), the Start menu entry
  (`Axit AxThing`), what in File Explorer opens it (a folder, a file, file types), and what it holds (so that
  `Dispatch` can route a path to it, if a path means this app).
- What is in version one and what waits. Barebones first (axDown AD-D2).

## 2. Documents (`docs/axthing/`)

- `SPEC.md`: summary, goals and non-goals, scenarios, requirements numbered `AT-n.m`, the keys (every key marked
  bundle-wide or window-specific, AX-8), notices and announcements (the list of everything spoken), settings, status
  bar, title, errors, architecture, decisions `AT-Dn`, testing, status. Cite the bundle rules (AX-7, AX-8) by number;
  "SPEC.md" in the app's code means this file.
- `user-guide.md`: the guide the app embeds (Help, User guide); install, start, use, settings, updates, if something
  goes wrong, about, disclaimer. About itself is the bundle's one notice (`Axit.Core/BundleAbout`, AX-3.1): name the
  app in its text.
- `nvda-test-plan.md`: the manual checks, by section, run before a release.

## 3. Pure code (`src/AxThing.Core/`, `tests/AxThing.Tests/`)

- `AxThing.Core.csproj` (net10.0), referencing `Axit.Core` only. Everything that needs no window goes here.
- `ThingSettings`: a plain class of properties with defaults, `DefaultPath => AppPaths.SettingsFile("axthing")`,
  `Load` through `SettingsFile.Load` plus a `Sanitize`, `Save` through `SettingsFile.Save`.
- Tests for every rule in the spec that the pure code carries; the test project references only `AxThing.Core`; its file holds that one reference, since `tests/Directory.Build.props` makes every test project xunit on .NET 10.
- `CLAUDE.md`: what is here, the commands, the gotchas.

## 4. The window (`src/AxThing/`)

A text editor app, one more file type, has no window of its own: it is an `EditorApp` record (name, verb, settings
file, file filter, F1 text, guide, the `TextJump` pairs of its outline, its resources pages, its commands on the text)
handed to `Axit.Forms/EditorHost.Run`, as `AxDownApp` and `AxJSONApp` do; the window is `Axit.Forms/TextEditorForm`.
Its own code is the outline and the commands (pure, in its Core, tested), its key table (the shell's keys through
`EditorKeys.All` plus its letters) and its texts. An app around a program in a pseudo console derives its window from
`Axit.Forms/ConsoleForm`, as axClaude and axTerminal do: its settings class from `Axit.Core/ConsoleSettings`, its model
from `Axit.Core/Transcript/LineStore`, and the window fills in the console hooks (`Working`, `OnTranscriptChanged`,
`UpdateStatus`, `OnProcessExited`, `AskBeforeClosing`, `FolderInHand`, `SwitchFolder`, `ProcessConsoleKey`) besides
the base's. The rest of this section is for a window of a new kind.

- `AxThing.csproj` (net10.0-windows, WinForms), referencing `AxThing.Core`, `Axit.Core` and `Axit.Forms`, embedding
  the guide as `UserGuide.md`.
- `AxThingApp.Run(string[] args)` and `Usage`: one call into `Axit.Forms/AppHost.Run` with the app's name, its log
  name, its usage text and a function from the remaining arguments to the window (`AppHost.OnePath` reads one file
  or folder and reports anything else); the host owns the log, the crash handling, `--help` and `--version`.
  `AxGitApp` is the example.
- `ThingForm`: the window, derived from `Axit.Forms/AxitForm` (AX-6.5), which already holds the notice flow with
  its focus rule, `Announce`, the fonts, the window's place, the settings saving and the settings-not-read notice,
  the Axit menu with the update flow, find in a text box, a list or a transcript, the folder picker, `Do` for a
  command's task, the hand-over from a starting process, the diagnostics and the common keys. Pass the app's name,
  its settings (a class derived from `Axit.Core/WindowSettings`), the settings path and the settings error to the
  base; add `Overlay` to the controls first; fill in the hooks (`Shortcuts`, `HideForNotice`, `ShowAfterNotice`,
  `FocusTarget`, `ApplyFont`, `RestartArguments`, `UpdateRestartText`, `ProcessWindowKey`, and the optional ones
  `NoticeChime`, `OnNoticeOpened`, `OnNoticeClosed`, `UpdateNoted`, `DiagnosticsLines`, `AnnounceSource`,
  `PaletteEntries`, `SaveCommand`, `TakeHandOver`, `SettingsNotRead`, `Fail`, `CopyDiagnostics`). Use the other
  shared pieces, never a copy: `ControlArea` for a field or a list in the input's place, `EditKeys` for an edit
  control's page and paragraph keys, `StatusLayout` for two status labels, `Notice` records for every question and
  text of the window's own, `CreateExitItem` for the end of the first menu, `ChooseFolder` for the folder picker.
  `GitForm` is the smallest example.
- The menu bar gets the bundle's Axit menu before Help, `CreateAxitMenu()` from the base (AX-9), which places the
  window's Check for updates and Update to items and the bundle's About submenu (AX-9.9); Help holds the app's own
  items only; the window's title ends in ` - AxThing`,
  which is how the Axit menu tells the apps' windows apart; Recently closed takes the window when it closes by itself
  (AX-9.4: by the app's name alone, or by the file or folder the window gives as `PathInHand`), and a window that
  swaps its file calls `RecentlyClosed.Remember` for the old one; it closes as on Alt+F4 when Close all windows posts
  `SC_CLOSE` (AX-9.8). A window that holds a file or a folder takes the bundle's Show in submenu,
  `CreateShowInMenu` (AX-9.10), never one of its own.
- `AxThingKeys`: the window's keys as constants with an `All` list; every key is a command of the window
  (docs/axit/app-kit.md AK-2): `Add(menu, new Command(label, AxThingKeys.Key, run) { When, Mark })` makes the menu
  item and handles the key, `AddKey` a key with no item; `ProcessWindowKey` only for what must come before them
  (the base handles the text size keys, F1 and the notice's hold first). axCalc's `CalcForm` is the small example.
- `HelpText`: the F1 text (bundle-wide keys first, then the app's) and the embedded guide.
- The base sets `AccessibleRole = AccessibleRole.Window`: NVDA then speaks the window's title when it opens
  ("name - AxThing window") before the focused control; without the role the window is a nameless pane to NVDA and
  the app is never named (axDown 2.1.1).
- Every control gets an `AccessibleName`; the caret moves only on the user's command; nothing speaks on a plain key.
- A list the user moves through is `Axit.Forms/AxitList`, never a plain `ListBox`: it has type-to-find (Shift with a letter
  backwards) and the edge earcon at its ends built in (AX-8.6), so the window writes nothing for them; set `Names` only
  when some items are not names (an empty list's one line). `tests/Axit.Tests/ListTests` fails on a plain list box.
- `CLAUDE.md`: where things are, the rules, the gotchas.

## 5. Wiring

- `Axit.Core/Dispatch`: the verb constant, the usage text, and the entry in `Apps`, placed in its group (the name, the
  verb, the project, the `AppGroup`, what it `Opens`, and `ShowIn` for an app that works in a folder), which `Decide`
  loops over and which gives the app its item under New window, its submenu in Open windows by type, its place in
  axFile's Open in Axit and the Show in submenus, and its line in About, Version (AX-9.2); `DispatchTests` and
  `BundleAboutTests` cover every entry by themselves.
- `src/Axit/Program.cs`: `case Dispatch.AxThing: SetAppId("Axit.AxThing"); AxThingApp.Run(launch.Arguments);`.
- `src/Axit/Axit.csproj`: the project reference. `Axit.sln`: the two projects (`dotnet sln add`).
- `tests/Axit.Tests`: the project reference and the window's table in `KeyTests.Windows`.
- `run.ps1`: the verb in `-App`'s `ValidateSet` and a branch that starts it; a `run-axthing.ps1` next to the
  others that calls `run.ps1 -App thing @args`.

## 6. Installer and release

- `tools/make-icon.ps1`: a line at the end with the app's letters and colour; run it; `publish.ps1` copies the icon
  next to the executable (a project item would be bundled into the single file).
- `install.ps1`: one block in `$apps` (the name, the verb, the description, the icon, what the app opens from File
  Explorer's Open with Axit submenu: folders, files, or both, and the file types a files entry is shown on; the file
  types it is a program for under Open with, with their type name). The Start menu entry, the shim, the submenu
  entries, the ProgID, the icon's copy, the removal and the `Say` lines all follow from the block (AX-2.3). A
  `-Start…` parameter for the updater if the app should come back on what it had open, and the matching argument
  in `Updater.LaunchInstaller`.
- `docs/axit/README.md` (the zip's guide): the app in the list, its Start entry, its command. `README.md` (the
  repository's page): the app in its list. `.github/ISSUE_TEMPLATE/bug.yml` and `idea.yml`: the app among the
  choices. `src/Axit/Axit.csproj`: the `<Description>`.
- `docs/axit/SPEC.md` section 3: the app, and its verb in AX-1.1. `CLAUDE.md` (root): the map line. `CHANGELOG.md`:
  bullets with the app's name in front.
- `tests/Axit.Tests/InventoryTests` checks every place above for every app of `Dispatch.Apps` (the folders, the
  three documents, the notes, the icon, the installer's block, the icon script, the templates, the pages, the map,
  the spec, the program's dispatch and `run.ps1`) and fails until the new app has them all (axStart was missing from
  nine of them on 2026-10-23).
- Build to a scratch folder, run every test project, run the `accessibility-review` skill on the window, probe the
  window off screen (see `src/AxDown/CLAUDE.md`), publish without installing and check the zip, then install and run
  the app's NVDA test plan before the release.
