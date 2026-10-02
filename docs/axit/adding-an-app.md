# Adding an app to Axit

The checklist for a new app of the bundle, written against what AxDown took (plan steps 6 to 9, 2026-10-02). An app
is a window with a verb; everything else below exists so that the app can be built, changed and released without the
rest of the bundle in context (`docs/axit/SPEC.md` 5.3). Work in this order: the spec first, then the pure code with
its tests, then the window, then the installer.

## 1. Decide

- The name (`AxThing`), the verb `Axit.exe` takes (`thing`), the console command (`axthing`), the Start menu entry
  (`Axit AxThing`), what in File Explorer opens it (a folder, a file, file types), and what it holds (so that
  `Dispatch` can route a path to it, if a path means this app).
- What is in version one and what waits. Barebones first (AxDown AD-D2).

## 2. Documents (`docs/axthing/`)

- `SPEC.md`: summary, goals and non-goals, scenarios, requirements numbered `AT-n.m`, the keys (every key marked
  bundle-wide or window-specific, AX-8), notices and announcements (the list of everything spoken), settings, status
  bar, title, errors, architecture, decisions `AT-Dn`, testing, status. Cite the bundle rules (AX-7, AX-8) by number;
  "SPEC.md" in the app's code means this file.
- `user-guide.md`: the guide the app embeds (Help, User guide) and shows under About; install, start, use, settings,
  updates, if something goes wrong, about, disclaimer.
- `nvda-test-plan.md`: the manual checks, by section, run before a release.

## 3. Pure code (`src/AxThing.Core/`, `tests/AxThing.Tests/`)

- `AxThing.Core.csproj` (net10.0), referencing `Axit.Core` only. Everything that needs no window goes here.
- `ThingSettings`: a plain class of properties with defaults, `DefaultPath => AppPaths.SettingsFile("axthing")`,
  `Load` through `SettingsFile.Load` plus a `Sanitize`, `Save` through `SettingsFile.Save`.
- Tests for every rule in the spec that the pure code carries; the test project references only `AxThing.Core`.
- `CLAUDE.md`: what is here, the commands, the gotchas.

## 4. The window (`src/AxThing/`)

- `AxThing.csproj` (net10.0-windows, WinForms), referencing `AxThing.Core`, `Axit.Core` and `Axit.Forms`, embedding
  the guide as `UserGuide.md`.
- `AxThingApp.Run(string[] args)` and `Usage`: `Log.UseApp("axthing")` first, the crash handling as `AxDownApp` has
  it, `--help` and `--version`, the settings, `Application.Run(new ThingForm(...))`.
- `ThingForm`: the window. Use the shared pieces, never a copy: `OverlayPanel` and `Notice` for every question and
  text of the window's own, `ControlArea` for a field or a list in the input's place, `EditPaging` for an edit
  control's page keys, `StatusLayout` for two status labels, `Sounds.Notice` for an unprompted notice,
  `WindowAccessibleObject` from `CreateAccessibilityInstance`. Copy the notice flow (`ShowNotice`, `CloseNotice`,
  `WithKeyLine`, the swallowed shortcuts and Alt/F10 while a notice shows), `Announce`, the font handling, the window
  placement and the update flow from `EditorForm` until plan step 11 shares them.
- `AxThingKeys`: the window's keys as constants with an `All` list; handle every key in `ProcessCmdKey` from
  `BundleKeys` and `AxThingKeys` and nowhere else; menu items show the key with `ShortcutKeyDisplayString`.
- `HelpText`: the F1 text (bundle-wide keys first, then the app's), the embedded guide, the disclaimer.
- Every control gets an `AccessibleName`; the caret moves only on the user's command; nothing speaks on a plain key.
- `CLAUDE.md`: where things are, the rules, the gotchas.

## 5. Wiring

- `Axit.Core/Dispatch`: the verb constant, the `Decide` branch, the usage text; a test in `DispatchTests`.
- `src/Axit/Program.cs`: `case Dispatch.AxThing: SetAppId("Axit.AxThing"); AxThingApp.Run(launch.Arguments);`.
- `src/Axit/Axit.csproj`: the project reference. `Axit.sln`: the two projects (`dotnet sln add`).
- `tests/Axit.Tests`: the project reference and the window's table in `KeyTests.Windows`.
- `run.ps1`: a way to start the app in development, if a file or folder argument does not already reach it.

## 6. Installer and release

- `tools/make-icon.ps1`: a line at the end with the app's letters and colour; run it; `publish.ps1` copies the icon
  next to the executable (a project item would be bundled into the single file).
- `install.ps1`: a block in `$apps` (verb, Start menu entry, shim, description, icon); File Explorer keys if the app
  opens folders or files (`-LiteralPath` for keys with `*`), a ProgID and `OpenWithProgids` values for its file
  types; add the new places to `$places` and the file-type values to `Remove-Installation`; copy the icon; the
  `Say` lines; a `-Start…` parameter for the updater if the app should come back on what it had open, and the
  matching argument in `Updater.LaunchInstaller`.
- `docs/axit/README.md` (the zip's guide): the app in the list, its Start entry, its command.
- `docs/axit/SPEC.md` section 3: the app. `CLAUDE.md` (root): the map line. `CHANGELOG.md`: bullets with the app's
  name in front.
- Build to a scratch folder, run every test project, run the `accessibility-review` skill on the window, probe the
  window off screen (see `src/AxDown/CLAUDE.md`), publish without installing and check the zip, then install and run
  the app's NVDA test plan before the release.
