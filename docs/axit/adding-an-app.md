# Adding an app to Axit

The checklist for a new app of the bundle, written against what axDown took (2026-10-02). An app
is a window with a verb; everything else below exists so that the app can be built, changed and released without the
rest of the bundle in context (`docs/axit/SPEC.md` 5.3). Work in this order: the spec first, then the pure code with
its tests, then the window, then the installer.

Since 2026-10-24 one run does the mechanical part (`docs/axit/app-kit.md` AK-1.2, AK-4):

```
.\tools\new-app.ps1 axThing thing Utilities Files -Description 'axThing, what it is' [-IconLetters At -IconRgb 60,90,40]
```

on a branch of its own, from a clean working tree; then `git diff`, a build and `test.ps1`. It writes everything
marked **(script)** below, prints the rest, and `InventoryTests` fails until the text places have the app. What
reads `Dispatch.Apps` needs no step at all, marked **(automatic)**. The sections below say what each place is, for
the review of what the script wrote and for the steps it leaves.

## 1. Decide

- The name (`AxThing`), the verb `Axit.exe` takes (`thing`), the console command (`axthing`), the Start menu entry
  (`Axit AxThing`), what in File Explorer opens it (a folder, a file, file types), and what it holds (so that
  `Dispatch` can route a path to it, if a path means this app).
- What is in version one and what waits. Barebones first (axDown AD-D2).

## 2. Documents (`docs/axthing/`)

The script writes the three as stubs with the headings below; the words are the app's author's.

- `SPEC.md`: summary, goals and non-goals, scenarios, requirements numbered `AT-n.m`, the keys (every key marked
  bundle-wide or window-specific, AX-8), notices and announcements (the list of everything spoken), settings, status
  bar, title, errors, architecture, decisions `AT-Dn`, testing, status. Cite the bundle rules (AX-7, AX-8) by number;
  "SPEC.md" in the app's code means this file.
- `user-guide.md`: the guide the app embeds (Help, User guide); install, start, use, settings, updates, if something
  goes wrong, about, disclaimer. About itself is the bundle's one notice (`Axit.Core/BundleAbout`, AX-3.1): name the
  app in its text.
- `nvda-test-plan.md`: the manual checks, by section, run before a release.

## 3. Pure code (`src/AxThing.Core/`, `tests/AxThing.Tests/`)

The script writes the project, `ThingSettings`, a first pure class (`ThingLines`, the list's lines and the title), their tests and the notes.

- `AxThing.Core.csproj` (net10.0), referencing `Axit.Core` only. Everything that needs no window goes here.
- `ThingSettings`: a plain class of properties with defaults, `DefaultPath => AppPaths.SettingsFile("axthing")`,
  `Load` through `SettingsFile.Load` plus a `Sanitize`, `Save` through `SettingsFile.Save`.
- Tests for every rule in the spec that the pure code carries; the test project references only `AxThing.Core`; its file holds that one reference, since `tests/Directory.Build.props` makes every test project xunit on .NET 10.
- `CLAUDE.md`: what is here, the commands, the gotchas.

## 4. The window (`src/AxThing/`)

The script writes a window of the new kind below on `AxitForm`, as small as axCalc's: `AxThingApp` (with or without a path, by what the app opens), `ThingForm` with one list, the menus of the bundle's frame and find in the list, an empty `AxThingKeys`, `HelpText` and the notes. An editor or a console app replaces it as this section says.

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

- `Axit.Core/Dispatch` **(script)**: the verb constant, the usage text (the app's line, the verb in the first line,
  the console command), and the entry in `Apps`, placed in its group: the name, the verb, the project, the
  `AppGroup`, what it `Opens`, `ShowIn` for an app that works in a folder, and for the scripts its `Description`
  (the Start menu entry's), its `Icon` (two letters and a colour), `Explorer` when File Explorer's Open with Axit
  submenu should offer it on other things than it `Opens`, the `Types` that submenu shows it on, and the `OpenWith`
  types with their `TypeName`. The script writes the name, verb, project, group, opens, description and icon; the
  rest is by hand, as is a rule in `Decide` when a path alone should open the app (AX-1.2). The entry gives the app
  its item under New window, its submenu in Open windows by type, its place in axFile's Open in Axit and the Show in
  submenus, and its line in About, Version (AX-9.2) **(automatic)**; `DispatchTests` and `BundleAboutTests` cover
  every entry by themselves, and the app order `DispatchTests` expects is rewritten by the script.
- `src/Axit/Program.cs` **(script)**: the line `[Dispatch.AxThing] = AxThingApp.Run,` in `Runs`, and its `using`;
  the taskbar id `Axit.AxThing` follows from the project name (AX-1.6).
- The project references **(automatic)**: `src/Axit/Axit.csproj` and `tests/Axit.Tests` reference every project of
  `src` by a glob. `Axit.sln`: the three projects **(script**, `dotnet sln add`**)**.
- `tests/Axit.Tests/KeyTests` **(script)**: the window's table in `Windows` and its F1 text in `F1Texts`; by hand,
  the known keys and the overrides when the window's keys meet another window's.
- `run.ps1` **(automatic)**: `-App` takes any verb of `Dispatch.Apps`, read off `Dispatch.cs` by `tools/apps.ps1`,
  and leaves the path out for an app that opens nothing. `run-axthing.ps1` **(script)** calls `run.ps1 -App thing
  @args`; `tests/scripts/axthing/01-start.txt` **(script)** is a first `tools/AxAutomate` script. By hand: the
  examples in `run.ps1`'s help, and its restart of a background app (`start|clip`).

## 6. Installer and release

- The icon: `src/Axit/AxThing.ico` **(script**, through `tools/make-icon.ps1 -Out`**)**; `make-icon.ps1` without
  arguments draws every app's icon from its `Icon` in `Dispatch.Apps` **(automatic)**; `publish.ps1` copies every
  `.ico` next to the executable (a project item would be bundled into the single file) **(automatic)**.
- `install.ps1` **(automatic)**: it builds its table from `Axit.exe apps` (AX-1.4), which prints `Dispatch.Apps` as
  JSON, and keeps the listing as `apps.json` in the installation for an uninstall that cannot run the program. The
  Start menu entry, the shim, the submenu entries, the ProgID, the icon's copy, the removal and the `Say` lines all
  follow (AX-2.3). By hand: a background app's line in `$background`; a `-Start…` parameter for the updater if the
  app should come back on what it had open, and the matching argument in `Updater.LaunchInstaller`; the header
  comment's list of commands.
- `.github/ISSUE_TEMPLATE/bug.yml` and `idea.yml`: the app among the choices; `src/Axit/Axit.csproj`: the
  `<Description>`; `docs/axit/SPEC.md`: the verb in AX-1.1 and the taskbar id in AX-1.6 **(script)**.
- The text places **(script, as drafts to rewrite in real words)**: `README.md` (the repository's page) and
  `docs/axit/README.md` (the zip's guide), the app in the list; `docs/axit/SPEC.md` section 3, the app's entry; the
  root `CLAUDE.md`, the map line; `CHANGELOG.md`, a bullet with the app's name in front under Unreleased. By hand:
  the app in the root `CLAUDE.md`'s opening paragraph and Commands paragraph, `docs/axit/SPEC.md`'s summary, the
  zip guide's Start entries and commands, and its removal from `docs/axit/plan.md`'s Still to come.
- The documents and the code the script wrote are a first draft: the spec, the guide and the test plan are stubs,
  and the window is one list. The first build writes the new test project's `packages.lock.json` and changes
  `tests/Axit.Tests/packages.lock.json`; commit both.
- `tests/Axit.Tests/InventoryTests` checks the places that stay text or code for every app of `Dispatch.Apps` (the
  folders, the three documents, the notes, the key table, the icon, the issue forms, the pages, the map, the spec,
  the program's line, the executable's description, the changelog) and fails until the new app has them all
  (axStart was missing from nine of them on 2026-10-23).
- Build to a scratch folder, run every test project, run the `accessibility-review` skill on the window, probe the
  window off screen (see `src/AxDown/CLAUDE.md`), publish without installing and check the zip, then install and run
  the app's NVDA test plan before the release.
