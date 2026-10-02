# Axit plan: from AxClaude to a bundle of accessible apps

Written 2026-10-02 against AxClaude 1.6.0. This is the working plan for turning the repository into **Axit**, one
installed program that contains several accessible Windows apps, starting with **AxClaude** (what exists today) and
**AxDown** (a barebones text editor for Markdown and plain text). Each step below is meant to be done on its own, in
order, ending in a green build and a commit, with AxClaude usable throughout. When a step is done, mark it here.

## 1. What we are building

- One program file, `Axit.exe`, one installer, one updater, one version number, one GitHub release workflow.
- Several apps inside it, each a separate window that starts on its own: `Axit.exe claude <folder>` opens AxClaude,
  `Axit.exe down <file>` opens AxDown. A path alone (`Axit.exe <path>`) picks the app: a folder goes to AxClaude, a
  file goes to AxDown.
- Separate Start menu entries, **Axit AxClaude** and **Axit AxDown**, so that Windows+`axc` finds one and
  Windows+`axd` the other, and `axit` lists both.
- Separate right-click entries in File Explorer: **Open in AxClaude** on folders, **Open in AxDown** on files; AxDown
  also appears under "Open with" for `.md`, `.markdown` and `.txt`.
- Separate console commands: `axclaude`, `axdown`, and `axit`, which decides by the path. All three are `.cmd` shims
  on PATH, so they work in cmd and PowerShell alike.
- A codebase and documentation split per app, so that a change to one app needs only that app's folder, spec and
  notes in context. This matters for working with smaller AI models, and it is what keeps the later apps (a
  collaborative editor, an AI-assisted editor) cheap to add.

## 2. The shape, and why

**One executable with a verb per app.** The alternative, one executable per app, was rejected: a self-contained .NET
executable carries the runtime, about 70 MB each, so two apps would double the download, and each would need its own
update check. Two executables sharing one runtime folder would need a multi-file publish (hundreds of files in the
zip) or an installed .NET, which AxClaude has always avoided. One executable keeps the zip, the installer, the Apps
entry and the updater as they are.

**Every app is a class library.** `src/AxClaude` and `src/AxDown` become WinForms class libraries, each with one
entry point (`AxClaudeApp.Run(args)`, `AxDownApp.Run(args)`) and its own usage text. `src/Axit` is the only
executable: its `Program` keeps the crash handling, reads the verb or the path, and hands the remaining arguments to
the app. One process shows one window; the taskbar tells the apps apart by an application id set at process start.

**Shared code lives in two projects.** `Axit.Core` (pure code, unit tested: settings file handling, the paths under
`Axit`, the update check, the generated sounds, the dispatcher) and `Axit.Forms` (WinForms pieces both apps use:
notices, the wave-out device, paging in an edit control, fonts, the log, the updater hand-over). Code moves into them
only when a second app needs it (the rule of two), so that nothing is generalised on a guess.

**No other architecture is needed for the later apps.** A collaborative editor or an AI-assisted editor is another
library, another verb, another Start entry and another shim. If an app ever needs a background process, that is a
verb too (`Axit.exe serve`). Nothing in this plan has to be undone for them.

## 3. Names and places

- Program file: `Axit.exe`. Process name `Axit` in Task Manager, window titles name the app.
- Start menu: `Axit AxClaude`, `Axit AxDown`.
- Console commands: `axit`, `axclaude`, `axdown` (shims in `%USERPROFILE%\.local\bin`, as today).
- File Explorer: `Open in AxClaude` on folders and folder backgrounds; `Open in AxDown` on files; AxDown under Open
  with for `.md`, `.markdown`, `.txt`.
- Settings, Apps: one entry, `Axit`, whose Uninstall removes everything.
- Installed to `%LOCALAPPDATA%\Programs\Axit`. Settings in `%APPDATA%\Axit\axclaude.json` and `axdown.json` (the
  first start copies the old `%APPDATA%\AxClaude\settings.json`). Logs in `%LOCALAPPDATA%\Axit\logs\axclaude.log`
  and `axdown.log`. Updates downloaded to `%LOCALAPPDATA%\Axit\updates`.
- Zip: `Axit-<version>-win-x64.zip`, with `install.cmd` and `install.ps1` inside, as today. First Axit version: 2.0.0.
- GitHub repository: see decision 1 in section 7.

## 4. Repository layout when the work is done

```
CLAUDE.md                 a short map of the bundle: names, commands, bundle rules, where each app's notes are
CHANGELOG.md              one file; every bullet starts with the app it concerns (AxClaude:, AxDown:, Axit:)
Axit.sln
run.ps1                   -App claude|down, or decided by the path; -Test; -NoBuild
publish.ps1, install.ps1, install.cmd, release.ps1
docs/axit/                SPEC.md (the bundle: launch, install, update, release, shared pieces, modularity rules),
                          README.md (shipped in the zip), adding-an-app.md (the checklist), plan.md (this file)
docs/axclaude/            SPEC.md, user-guide.md, claude-screens.md, nvda-test-plan.md (moved from docs/ and the root)
docs/axdown/              SPEC.md, user-guide.md, nvda-test-plan.md
src/Axit/                 Program.cs, Axit.csproj, Axit.ico, AxClaude.ico, AxDown.ico; CLAUDE.md
src/Axit.Core/            Dispatch, SettingsFile, AppPaths, Updates/, Audio/; CLAUDE.md
src/Axit.Forms/           OverlayPanel, Sounds, EditPaging, StatusLayout, Fonts, Log, Updater, Crash; CLAUDE.md
src/AxClaude/             MainForm, TranscriptView, ControlArea, ListKeys, HelpText, StartupOptions, AxClaudeApp; CLAUDE.md
src/AxClaude.Core/        Vt/, Transcript/, Pty/, Privacy/, AppSettings (unchanged)
src/AxDown/               EditorForm, AxDownApp, HelpText; CLAUDE.md
src/AxDown.Core/          TextDocument, EditorSettings, MarkdownOutline (step 11)
tests/Axit.Tests/         DispatchTests, SettingsFileTests, UpdateCheckTests, WaveToneTests, PrivacyTests, LayoutTests
tests/AxClaude.Tests/     as today, minus what moved
tests/AxDown.Tests/       TextDocumentTests, MarkdownOutlineTests
tests/fixtures/           as today (AxClaude's recordings)
tools/PtyCapture/, tools/make-icon.ps1 (one icon per app), tools/release-notes.ps1
.claude/skills/           accessibility-review (bundle wide), record-fixture (AxClaude)
.github/workflows/        build.yml, release.yml
```

## 5. Modularity rules

These go into `docs/axit/SPEC.md` as requirements and into the root `CLAUDE.md` as rules. They are what lets a
change to one app be made without the rest in context.

1. **One app, one set of folders.** An app is `src/<App>` (the window), `src/<App>.Core` (pure code with tests),
   `docs/<app>/` (its spec, user guide and NVDA test plan) and `tests/<App>.Tests`. Nothing of the app lives elsewhere.
2. **References point one way.** An app references `Axit.Core` and `Axit.Forms`, never another app. The shared
   projects never reference an app. A unit test (`LayoutTests`) reads the project files and fails when this is broken.
3. **Shared code moves on the second use.** A piece goes to `Axit.Core` or `Axit.Forms` when a second app needs it,
   in a commit of its own, with the accessibility review. Nothing is moved on a guess.
4. **Notes next to the code.** Each `src/<App>/CLAUDE.md` holds that app's gotchas and points to its spec. Claude Code
   reads these files when it works in that folder. The root `CLAUDE.md` is a map and the bundle rules, under about
   60 lines. The AxClaude gotchas that are in the root today move to `src/AxClaude/CLAUDE.md`.
5. **One spec per app, complete on its own.** The bundle spec covers only what crosses apps: launch and dispatch,
   installation, updates, releases, the shared pieces, and the accessibility rules every app follows (every control
   named, no second window, caret moved only by the user, results announced through UI Automation). App specs refer
   to those rules by number instead of repeating them. In AxClaude's code, "SPEC.md" keeps meaning AxClaude's spec.
6. **One version, one changelog, one release.** The bundle has one `<Version>`; bullets name their app.
7. **An app's texts belong to the app.** Its user guide is embedded by its own project, its F1 text lives in its
   `HelpText`, its usage text in its `StartupOptions`.
8. **A new app follows the checklist** in `docs/axit/adding-an-app.md`: folders, verb, Start entry, context menu,
   shim, icon, spec, guide, test plan, CLAUDE.md, tests.
9. **Keys live in tables, in two kinds** (SPEC AX-8, B-12). Every key is bundle-wide (the same in every window,
   defined once in `Axit.Forms/BundleKeys`) or window-specific (one table per window, `AxClaudeKeys`, `AxDownKeys`).
   Each app's keyboard table in its spec marks the kind. A key is changed in its table and nowhere else, and a unit
   test fails when a window key collides with a bundle key and lists keys two windows use differently.
10. **The notices and the question area are shared, not copied** (SPEC AX-6.4, B-13). `OverlayPanel`, `ControlArea`
    and `ListKeys` are how every window of Axit asks the user something; they move to `Axit.Forms` unchanged.

## 6. The steps

Each step says what changes, when it is done, and what is needed from Kyle. Steps 1 to 5 keep AxClaude working as
today; AxDown starts at step 6.

### Step 1. The bundle spec and the document split (spec first, no code behaviour change)

- Write `docs/axit/SPEC.md`: the bundle's requirements (dispatch, installation, updates, releases, shared pieces),
  the accessibility rules that every app follows, the modularity rules of section 5, and the decisions of this plan
  as numbered decisions.
- Move `SPEC.md`, `docs/user-guide.md`, `docs/claude-screens.md` and `docs/nvda-test-plan.md` to `docs/axclaude/`.
  Point `AxClaude.csproj` (the embedded guide), `publish.ps1` (the README), the skills and the root notes at the new
  places.
- Split `CLAUDE.md`: the root becomes the map and the bundle rules; the AxClaude gotchas go to `src/AxClaude/CLAUDE.md`.
- Done when: build and tests pass; no stale path remains (grep for `docs/user-guide`, `SPEC.md` in scripts and skills).
- Kyle: read `docs/axit/SPEC.md` and settle the decisions of section 7.

### Step 2. Bridge release 1.7.0 (so installed copies can update into Axit)

- The 1.6.0 updater finds a release's zip by its `-win-x64.zip` ending, so it will find the Axit zip, but it then
  refuses a download without `AxClaude.exe` in it. Change `Updater.IsComplete` to ask for `install.ps1` and any
  `.exe`, and keep `install.ps1`'s hand-over parameters (`-WaitForProcess`, `-Start`, `-ContinueConversation`,
  `-LogFile`) stable from here on.
- CHANGELOG: "AxClaude is becoming part of Axit; this version can update into it."
- Done when: 1.7.0 is released and installed on Kyle's machine through Help, Update.
- Kyle: `.\release.ps1 1.7.0` (I will not release without being told).

### Step 3. `Axit.exe` with AxClaude inside

- New `src/Axit` (executable: `Program`, icon) and `src/Axit.Core` with `Dispatch`: arguments to an app name plus the
  app's arguments. Verbs `claude` and `down`; a lone path by kind (folder, file); the rules for no argument and for
  a missing path follow decision 2. Unit tested in `tests/Axit.Tests`.
- `src/AxClaude` becomes a class library with `AxClaudeApp.Run(string[] args)`; `Program`'s crash handling, version
  and log start move to `Axit`. The solution becomes `Axit.sln`; `run.ps1` builds `src/Axit`.
- Each process sets its application id (`Axit.AxClaude`, later `Axit.AxDown`) so the taskbar does not group the two
  apps.
- Done when: tests pass and `Axit.exe <folder>` behaves exactly like `AxClaude.exe <folder>` did.
- Kyle: restart into the new build and use it for a while.

### Step 4. The shared projects, first contents

- `Axit.Core`: `AppPaths` (settings, logs and updates under `Axit`, per app), `SettingsFile` (the load, save and
  error pattern that `AppSettings` has today, made reusable, plus the take-over of an old file), `Log` (per-app file
  name), `Updates/` (`UpdateCheck` and `Updater`; neither needs a window, so they live in Core, not Forms) and
  `Audio/WaveTone` moved from AxClaude. `AppSettings` stays AxClaude's and uses `SettingsFile`. The first start
  copies the old settings file to the new place. The crash report stays per app until a second copy shows what is
  common.
- Tests move with their code into `tests/Axit.Tests`, with the new `SettingsFileTests` and `LayoutTests` (rule 2 of
  section 5). `PrivacyTests` stays in `tests/AxClaude.Tests`: it tests `Redaction` too, and it scans the whole
  repository from either place.
- Done when: tests pass; AxClaude's log and settings are in the `Axit` folders; Help, About and the update check work.

### Step 5. Installer, publish and release for the bundle

- `install.ps1`: installs to `Programs\Axit`; Start entry `Axit AxClaude`; folder context menu running
  `Axit.exe claude "%1"`; shims `axit.cmd` and `axclaude.cmd`; Apps entry `Axit`; removes an old AxClaude
  installation completely when it finds one (folder, shortcut, shim, registry keys, Apps entry); `-Uninstall` removes
  everything of Axit. `-Start <folder>` keeps meaning "start AxClaude there", for the 1.7.0 updater.
- `publish.ps1`: publishes `src/Axit`, names the zip `Axit-<version>-win-x64.zip`, ships `docs/axit/README.md` as
  the zip's README. `install.cmd` downloads `Axit-*`. `release.ps1` and `release.yml` read the version from
  `src/Axit/Axit.csproj`; the release title is `Axit <version>`. `Updater.IsComplete` asks for `Axit.exe`.
- Done when: `.\publish.ps1` installs on Kyle's machine; Windows+`axc` finds the Start entry; `axit`, `axclaude` and
  the folder menu start the app; `install.ps1 -Uninstall` leaves nothing behind. The Start search is the one thing
  to verify by hand: if the second word of a shortcut name is not matched, the entries are renamed (`AxClaude
  (Axit)`), which is a one-line change.
- Kyle: run `publish.ps1`, check Start, the folder menu and the console commands.

### Step 6. The AxDown spec

- Write `docs/axdown/SPEC.md`, `docs/axdown/user-guide.md` and `docs/axdown/nvda-test-plan.md`.
- Barebones scope: one window with a menu bar, one plain multi-line edit control (the native EDIT control, as the
  conversation view; `MaxLength` 0, Tab typed, Enter typed, word wrap on by default), one status bar. File: New,
  Open, Save, Save as, Exit, with the Windows file picker. Edit: Undo, Cut, Copy, Paste, Select all, Find, Find next,
  Go to line. View: Word wrap, Text size, Font. Help: Shortcuts (F1), User guide, Check for updates, About. The
  title is `<file name> - AxDown`, with a modified mark. Closing with unsaved changes asks through a notice in the
  window, never a popup. Line and column on demand (a key, not automatic speech).
- Files: UTF-8 with or without a byte order mark, UTF-16 by its mark, otherwise the system code page; line endings
  detected and kept as found; the file is written back in the encoding it was read in; a new file is UTF-8 without a
  mark with `\r\n`. A file changed on disk behind the editor is noticed on save.
- Command line: `Axit.exe down [<file>]`, `axdown [<file>]`. Settings: font, word wrap, window placement.
- Out of scope for now: tabs, syntax colouring, auto-save, spelling, a preview, printing.
- Kyle: read the spec; settle the keys and decision 4.

### Step 7. `AxDown.Core`

- `TextDocument`: read and write with the encoding and line-ending rules of the spec; `EditorSettings`.
- `tests/AxDown.Tests` with files for every encoding and line-ending case.
- Done when: tests pass. No window yet.

### Step 8. The AxDown window

- `src/AxDown`: `EditorForm`, `AxDownApp.Run`, `HelpText`, the embedded user guide. `Dispatch` learns the `down`
  verb and sends files there. `run.ps1 -App down <file>` starts it.
- Shared pieces move to `Axit.Forms` as the editor needs them, one commit each, with the accessibility review:
  `OverlayPanel` (the unsaved-changes notice), `ControlArea` and `ListKeys` (the first question AxDown asks in the
  field's place, Go to line, uses them), `Sounds` and the notice chime, `EditPaging`, `StatusLayout`, the font
  handling (text size and the font picker) out of `MainForm`. AxClaude is checked after each move.
- The first key tables: `Axit.Forms/BundleKeys` with the keys settled at step 6, and `AxDown/AxDownKeys`; the menu
  items, the key handlers and the F1 text of AxDown read them. The collision test of SPEC AX-8.4 starts here.
- Done when: a file opens, edits, saves and closes correctly with NVDA; AxClaude unchanged.
- Kyle: an NVDA pass of both apps.

### Step 9. AxDown installed

- `install.ps1`: Start entry `Axit AxDown`; `Open in AxDown` on files; Open-with registration for `.md`,
  `.markdown`, `.txt` (decision 4); `axdown.cmd`; `-Start` grows a way to start AxDown on a file for the updater.
  `AxDown.ico` drawn by `tools/make-icon.ps1`. `docs/axit/README.md` describes both apps.
- Done when: Windows+`axd` finds AxDown; right-click a file, Open in AxDown works; `axdown file.md` works.

### Step 10. NVDA check and release 2.0.0

- Both test plans run with NVDA; fixes; CHANGELOG `## 2.0.0`.
- Kyle: `.\release.ps1 2.0.0`; then Help, Update from the installed 1.7.0 must bring Axit in.

### Step 11. AxClaude's key table

- `AxClaude/AxClaudeKeys`: every key `MainForm`, `TranscriptView`, `ControlArea` and `ListKeys` handle, as named
  constants, the bundle-wide ones taken from `BundleKeys`. The menu items, the handlers and the F1 text read the
  table; no behaviour changes. The keyboard table in `docs/axclaude/SPEC.md` §6.3 marks each key's kind.
- The collision test now covers both windows and lists the keys they use differently.
- Done when: tests pass, the accessibility review finds no change, and the NVDA test plan's key sections pass.

### Step 12. Markdown navigation in AxDown

- `AxDown.Core/MarkdownOutline`: headings (`#` lines), fenced code blocks, list items, links, computed from the text
  and a caret position: next and previous of each kind. Unit tested.
- Keys: Ctrl+letter for the next item, Ctrl+Shift+letter for the previous (the letters are decision 5, checked
  against `BundleKeys` and `AxClaudeKeys` by the collision test before they are chosen). The jump moves the caret,
  as the user asked, and the line is announced through a UI Automation notification; "No next heading" and the like
  when there is none. Nothing speaks on a plain arrow key.
- Release 2.1.0.

### Step 13. The pattern made explicit

- `docs/axit/adding-an-app.md`: the checklist of section 5, rule 8, written against what steps 6 to 9 actually took.
- Each `CLAUDE.md` checked for length and for pointing at its spec; the root under about 60 lines.

## 7. Decisions for Kyle

1. **Repository name.** Recommended: rename `KyleKeane/AxClaude` to `KyleKeane/Axit` on GitHub when 2.0.0 is
   released. GitHub redirects the old web and API addresses, so installed 1.7.0 copies still find releases; the code
   then changes `UpdateCheck.Repository` and the links in the installer and the guide. Alternative: keep the name.
2. **`axit` with no argument.** Recommended: start AxClaude on the current folder, what `axclaude` does today, since a
   console inside a project is where that command is typed. Alternative: show the usage text.
3. **Bridge release 1.7.0.** Recommended: yes; it is a few lines and saves every installed copy from an update that
   fails with "the zip does not contain AxClaude.exe".
4. **File types for AxDown's Open with.** Recommended: `.md`, `.markdown`, `.txt`; `Open in AxDown` on every file.
   Windows does not let an installer set the default program silently; the guide explains Settings, Default apps.
5. **AxDown's navigation keys** (step 12). Settled 2026-10-02: NVDA's browse-mode letters with Ctrl (next) and
   Ctrl+Shift (previous): H, 1 to 6, K, L, I, Q, T; built headings first (AxDown SPEC AD-D7).
6. **Program file name.** `Axit.exe`, process `Axit`. Confirm.

## 8. Risks

- `MainForm` is 2 600 lines; moving shared pieces out of it can break AxClaude's focus and announcement behaviour.
  Mitigation: one piece per commit, the accessibility review skill on each, the NVDA test plan before a release.
- A running `Axit.exe` locks `src/Axit/bin`; build to a scratch folder to check a change, as today.
- Start menu search indexes new shortcuts with a delay of up to a minute after installation.
- A word-wrapped EDIT control re-wraps everything below an edit near the top (0.7 s at 20 000 lines, see AxClaude's
  notes). AxDown is for documents, not multi-megabyte logs; the spec says so.
- Renaming the repository changes the clone address for anyone who has it; GitHub's redirect covers fetches and
  pushes, and a note goes into the release notes.

## 9. Status

The work runs on the branch `axit` (SPEC B-11); `main` holds AxClaude 1.7.0 until 2.0.0 is checked with NVDA.
Decisions 1 to 4 and 6 of section 7 were taken as recommended on 2026-10-02; decision 5 waits for the AxDown spec.
Added the same day at Kyle's request: rules 9 and 10 of section 5 (key tables in two kinds; the notices and the
question area shared), step 11, and the key work in steps 8 and 12.

- [x] Step 1 bundle spec and document split (2026-10-02)
- [x] Step 2 bridge 1.7.0 released from `main` (2026-10-02)
- [x] Step 3 Axit.exe with AxClaude inside (2026-10-02; `publish.ps1`, `install.ps1` and the release pipeline wait for step 5)
- [x] Step 4 shared code in `Axit.Core` (2026-10-02; `Axit.Forms` comes with AxDown at step 8)
- [x] Step 5 installer, publish and release (2026-10-02, code and zip; Kyle's checks of Start search, folder menu and commands pending)
- [x] Step 6 AxDown spec, guide and test plan written (2026-10-02); Kyle to read them and settle AD-D7, the navigation letters
- Queued after step 10 (Kyle, 2026-10-02): two AxClaude bugs seen in the question interface during this work, recorded
  in `docs/axclaude/SPEC.md` §12 To do: a two-question AskUserQuestion that kept refiring in the control area, and
  an own-words (Other) answer that reached Claude with most characters dropped. Both need a raw recording first.
- [x] Step 7 AxDown.Core: `TextDocument`, `EditorSettings`, 14 tests (2026-10-02); `WindowPlacement` moved to `Axit.Core` on its second use
- [x] Step 8 AxDown window: `EditorForm`, `AxDownApp`, `AxDownKeys`, `BundleKeys`, the shared pieces in `Axit.Forms`, the key collision test (2026-10-02); NVDA pass at step 10
- [x] Step 9 AxDown installed: Start entry, file menu, Open with, `axdown`, `AxDown.ico`, `-StartFile` for the updater (2026-10-02); Kyle's checks pending
- [x] Step 10 NVDA check by Kyle, repository renamed to `KyleKeane/Axit`, Axit 2.0.0 released (2026-10-02)
- [x] Step 11 AxClaude's key table `AxClaudeKeys`, both windows in the collision test (2026-10-02; no key changed)
- [ ] Step 12 Markdown navigation, release 2.1.0
- [x] Step 13 the pattern made explicit: `docs/axit/adding-an-app.md` (2026-10-02, written while the pattern was fresh; the CLAUDE.md length check stays for after step 11)
