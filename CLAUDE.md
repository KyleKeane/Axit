# Axit

Axit is a bundle of accessible Windows apps for a blind developer who uses the NVDA screen reader, shipped as one self-contained executable. Its first app is **AxClaude**, a front end for the Claude Code CLI (`claude --ax-screen-reader` in a ConPTY pseudo console, shown as a line-by-line transcript); the second, **AxDown**, a barebones editor for Markdown and text files, is being built. The transition from the AxClaude repository to the bundle runs on the branch `axit` following `docs/axit/plan.md`; `main` is AxClaude 1.7.0 until 2.0.0 is released.

Specifications are the source of truth: `docs/axit/SPEC.md` for the bundle (launch, install, update, release, shared pieces, the accessibility rules, the modularity rules), `docs/axclaude/SPEC.md` for AxClaude, `docs/axdown/SPEC.md` for AxDown. A behaviour change updates the spec in the same commit.

## Stack and constraints

- .NET 10 (LTS), C#, WinForms. WinForms is deliberate: native Win32 controls (EDIT, menus, status bar, ListBox) that NVDA reads reliably. No WPF, Avalonia or web view.
- Zero third-party runtime dependencies, no NuGet packages. Windows APIs are called through P/Invoke.
- Plain code (SPEC B-10): plain classes, static methods, records for data, one obvious way to do each thing. No dependency injection, no interfaces with one implementation, no reflection beyond what WinForms needs, no clever generics. A smaller model must be able to change one app with only that app's folders, spec and notes in context.
- Everything that does not need a window lives in a `.Core` project and is unit tested; recorded fixtures live under `tests/fixtures`.

## Commands

Until step 3 of the plan the solution is `AxClaude.sln` and the executable `src/AxClaude`; the commands are in `src/AxClaude/CLAUDE.md`. Bundle-level scripts: `publish.ps1` (self-contained exe + installer + guide into `publish\`, zip, install for this user; `-NoInstall`), `install.ps1` and `install.cmd` (per-user install, `-Uninstall`), `release.ps1 <version>` (after a `## <version>` section in `CHANGELOG.md`: tests, version, commit, tag, push; GitHub Actions publishes the release the apps update from), `gh run watch`, `tools/make-icon.ps1`.

## The apps and where their notes are

- **AxClaude**: window code in `src/AxClaude` (notes: `src/AxClaude/CLAUDE.md`), pure code in `src/AxClaude.Core` (notes: `src/AxClaude.Core/CLAUDE.md`), tests in `tests/AxClaude.Tests`, documents in `docs/axclaude/` (`SPEC.md`, `user-guide.md`, `claude-screens.md`, `nvda-test-plan.md`). In AxClaude's code and documents, "SPEC.md" means `docs/axclaude/SPEC.md`.
- **AxDown** (from step 6): `src/AxDown`, `src/AxDown.Core`, `tests/AxDown.Tests`, `docs/axdown/`.
- **Bundle**: `src/Axit` (the executable), `src/Axit.Core` and `src/Axit.Forms` (shared pieces), `tests/Axit.Tests`, `docs/axit/` (`SPEC.md`, `plan.md`, `README.md` shipped in the zip, `adding-an-app.md`), the scripts and `.github/workflows` (`build.yml` tests every push; `release.yml` builds a `v*` tag into a GitHub release).
- Skills: `.claude/skills/accessibility-review` (run on any UI change before committing), `.claude/skills/record-fixture` (AxClaude's recordings).

## Rules of the bundle

1. Accessibility first (SPEC AX-7). Every control gets an `AccessibleName`; every action works without a mouse; the caret moves only on the user's own command; results of navigation are announced through one UI Automation notification.
2. No second window of an app's own (AX-7.2): questions, errors and help texts are notices inside the window (`OverlayPanel`), what an app waits on goes in place of its input field (`ControlArea`); never a `Form` or a `MessageBox` once a window exists. The Windows folder, file and font pickers are the only separate windows.
3. Never block the UI thread on I/O; background reads are marshalled to the UI thread and batched.
4. Modularity (SPEC 5.3): one app, one set of folders; references point one way (an app never references another app; shared projects never reference an app; `LayoutTests` enforces it); shared code moves on the second use, unchanged, one piece per commit; notes next to the code; one version, one changelog with app prefixes (`AxClaude:`, `AxDown:`, `Axit:`).
5. Keys (SPEC AX-8): every key is either bundle-wide (`Axit.Forms/BundleKeys`, the same in every window) or window-specific (one table per window); a key is changed in its table and nowhere else; a test fails on a collision.
6. Personal data never enters the repository or a session's output: only the licence identity (Dr. Kyle Keane, www.kylekeane.com) appears; `PrivacyTests` fails on an e-mail address or an account path; recordings are read through PtyCapture's redacting `--dump`, never `--raw`, `cat` or `type`.
7. Git hygiene: small commits with imperative subject lines; never commit `bin/`, `obj/`, ad-hoc captures or `.claude/settings.local.json`. During the transition every commit on `axit` builds and passes the tests, and AxClaude keeps behaving as released.

## Environment gotchas (bundle-wide)

- A running app locks its `bin` folder. Build somewhere else to check a change (`dotnet build <project> -o <scratch dir>`) and ask the user to restart the app for a real run. Dialogs and menus can be checked off screen from a small probe project that references the scratch build; never activate windows or send keys while the user is working.
- The Bash tool mangles backslashes inside heredocs: use the Write tool for source files.
- Windows PowerShell 5.1: `$PSScriptRoot` is empty while parameter defaults are evaluated, and a function that returns a byte array unrolls it unless written `return , $bytes`.

## Manual verification

Any change to focus handling, key handling, announcements or an edit control is checked with NVDA running, following the app's test plan (`docs/<app>/nvda-test-plan.md`).
