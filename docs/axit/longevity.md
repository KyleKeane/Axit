# Axit for the years ahead

The user's aim, set 2026-10-23: a repository and a program that stay a stable resource for decades, as long as .NET
is supported, so that anyone who relies on Axit can go on relying on it. This page is the plan: what already keeps
Axit stable, what was adopted on 2026-10-23, what is proposed with its cost, and the calendar. It is kept with the
code like the specs; `review-2026-10-23.md` is the review it came out of.

## 1. What already keeps Axit stable

- **No dependencies.** The apps use .NET, Windows Forms and Windows' own APIs through P/Invoke, and no package (G4,
  B-10). Nothing can be unpublished, abandoned or changed under the program. The only packages are the test stack
  (xunit, the test SDK), pinned to exact versions and bumped by Dependabot once a month.
- **One self-contained executable.** The .NET runtime ships inside `Axit.exe`; an installed copy keeps working whatever
  happens to the .NET installed on the machine, and every released zip stays downloadable from GitHub while the
  repository exists.
- **Local first (G6, B-15).** Every app but AxClaude keeps its full use with the network off; the update check is the
  only call a window makes on its own, and it is a setting. A world without the GitHub API is a world without
  updates, not without Axit.
- **Specifications as the source of truth,** one per app, with every behaviour change in the same commit, numbered
  requirements the code cites, a decision record of why things are as they are, and a test plan per app with the
  record of every NVDA pass (`nvda-test-record.md`). A reader in ten years finds what the program does and why
  without the people who wrote it.
- **Plain code** (B-10): plain classes, static methods, records, no framework, no reflection, no generics beyond the
  obvious, notes next to the code (`CLAUDE.md` in every project) with the gotchas learnt the hard way. A smaller
  model, or a person new to the project, can change one app with its folders in context.
- **Tests that guard the structure, not only the behaviour**: the project references point one way (`LayoutTests`),
  no window key equals a bundle key and every key is named in its F1 text (`KeyTests`), no hand-made menu drop-down
  (`MenuTests`), no personal data in any file (`PrivacyTests`), every app has its places (`InventoryTests`), the
  About text names every app (`BundleAboutTests`); CI runs them on every push to every branch.
- **Releases from `main` only**, every commit building and passing, larger work on a branch until its NVDA checks pass
  (B-11); the release is a tag that GitHub Actions turns into the zip and the notes from `CHANGELOG.md`.

## 2. Adopted on 2026-10-23

- **The tests run as the program runs.** The executable runs with invariant globalization and the tests did not, which
  hid two defects for a week (`Accents`). `tests/Directory.Build.props` sets the same, and a test asserts it.
- **The .NET version is one property**, `AxitDotNet` in `Directory.Build.props`, from which every project's
  `TargetFramework` is built; the SDK band is `global.json`, which the workflows read too (`setup-dotnet` with
  `global-json-file`). The move to the next LTS is those two files, a run of the tests and a release.
- **The release notes name the SDK** that built the release, so that a rebuild years later knows what to reproduce.
- **`InventoryTests`** fails when an app lacks one of its places (AxStart lacked nine on this day).
- **The NVDA record asks for the environment** of a pass (the NVDA version, the synthesizer, the Windows build), so
  that a regression is dated to an NVDA release rather than to some time.
- **The status of an app lives in two places only**: its spec's status section and the NVDA record. The spec headers
  no longer say "as of version n" or "not yet built", which rot by construction.
- **`.editorconfig` says CRLF**, which is what the files are on disk; the repository stores LF (`.gitattributes`).
- **The support window is dated**: as of 2026-10-23, .NET 10 (LTS) is supported until November 2028 and the next
  LTS, .NET 12, is due in November 2027; Windows 10 left support on 2025-10-14 (Axit still runs on 1809 and later)
  and Windows 11 is current. The yearly check (section 5) moves these dates.
- **The installer is off the GitHub API**: `install.cmd` on its own follows the redirect of `releases/latest` to the
  tag and downloads the zip by its name, with whatever TLS Windows offers; and `install.ps1` puts
  `%USERPROFILE%\.local\bin` on the user's PATH when it is not there, so the console commands work without Claude
  Code.
- **The actions are pinned by commit** and the runner is `windows-2025`; Dependabot moves the pins.
- **Every project tells the compiler it is Windows code** (one `AssemblyAttribute` item in `Directory.Build.props`)
  instead of CA1416 being silenced.
- **The Windows APIs are a list** in `docs/axit/SPEC.md` 5.4, one entry each with its file and its fallback.
- **Claude Code's version is logged** at the first start of a session and named by Copy diagnostics; a fixture is
  recorded when a Claude Code release touches screen reader mode (the `record-fixture` skill says so).
- **`release.ps1` checks the notes** (every bullet names its app or is for developers, the section is not empty),
  pushes `main`, watches its build with `gh`, then pushes the tag.
- **The test projects have lock files** (`packages.lock.json`, committed), so that a restore years from now gives
  the packages of today.
- **The shipped guide names the Windows requirement** (Windows 10 1809 or later, Windows 11), and the manual release
  path is in `README.md`.

## 3. Proposed on 2026-10-23, with the cost of each and what became of it

In the order of what they do for the decades goal. What needed no account and no decision was done the same day
(section 2); the rest waits for the maintainer.

1. **A dated support window in this page and a yearly check** (section 5). Cost: ten minutes a year. Done
   2026-10-23: the window is dated in section 2.
2. **The repository under a GitHub organization with a second owner**, so that it outlives one account; the
   repository name is in `UpdateCheck.cs` and sixteen other files, and a transfer redirects (B-7). Cost: free; one
   afternoon to move; a settings key for the update repository (twenty lines) if a successor fork is ever to be
   adopted without a rebuild. Open: the maintainer's account and decision.
3. **The manual release path written down** in `README.md` (`publish.ps1 -NoInstall`, then `gh release create
   --draft`, upload, publish), so that a release is possible when GitHub Actions is not. Cost: five lines. Done 2026-10-23 (`README.md`, Release).
4. **The installer off the rate-limited API**: `install.cmd` asks `api.github.com` for the latest release, which allows
   sixty unauthenticated requests an hour per address (a classroom behind one NAT gets 403), and forces TLS 1.2
   alone; it can follow the redirect of `github.com/<repo>/releases/latest` instead and download the asset by name.
   The apps' own check has the same limit, with a notice, which is acceptable. Cost: ten lines of PowerShell, tested
   on a real download. Done 2026-10-23 (`install.cmd`, with whatever TLS Windows offers; the redirect, the tag and
   the zip's address were tried against the 3.4.0 release).
5. **Actions pinned by commit** (`actions/checkout@<sha> # v7`), which Dependabot keeps current, and `windows-latest`
   named by its version (`windows-2025`) so that a runner change is a deliberate edit. Cost: one PR a month to merge. Done
   2026-10-23 (`build.yml`, `release.yml`).
6. **A table of the Windows APIs depended on** in `docs/axit/SPEC.md` section 5, one line each with the file and the
   documented fallback: ConPTY (`CreatePseudoConsole`, Windows 10 1809), `waveOut` (legacy, kept), `RegisterHotKey`,
   `WM_COPYDATA`, `SHFileOperationW` (superseded by `IFileOperation`, still shipped), `IShellFolder` and
   `IContextMenu`, `shell:AppsFolder` (a namespace whose parsing names Microsoft does not promise), **MFPlay** (its
   documentation carries a deprecation notice; the Media Session is the fallback AM-D5 names), `StrCmpLogicalW`,
   UI Automation notifications (1709). Cost: one page once. When Windows removes one, the table says which file. Done
   2026-10-23 (`docs/axit/SPEC.md` 5.4).
7. **`[SupportedOSPlatform("windows")]` on the Core projects** instead of silencing CA1416 in `.editorconfig`, so that
   the compiler knows they are Windows code. Cost: one attribute per project, a build. Done 2026-10-23: one
   `AssemblyAttribute` item in `Directory.Build.props` marks every project, and the silence is gone from
   `.editorconfig`.
8. **The installer adds `%USERPROFILE%\.local\bin` to the user's PATH itself** rather than relying on Claude Code's
   installer having done it, so that the console commands work for a user without Claude Code (G6). Cost: a dozen
   lines, no administrator rights. Done 2026-10-23 (`install.ps1`: the registry value is kept expandable, the
   removal leaves the PATH).
9. **The Windows requirement and the Windows 11 menu fact in the shipped guide** (`docs/axit/README.md`): Windows 10
   1809 or later; on Windows 11 the Open with Axit submenu is in the full menu (Shift+F10), since the modern menu
   needs a packaged app. Cost: two sentences. Done 2026-10-23.
10. **Claude Code's version in the log and in Copy diagnostics** (`claude --version` once at session start), and a
    fixture recorded whenever a Claude Code release note touches screen reader mode (the `record-fixture` skill),
    since AxClaude is the one app coupled to a service that changes. Cost: one process call; a fixture an hour. Done
    2026-10-23: `ClaudeLauncher.VersionAsync`, the log line `Claude Code version:` and the diagnostics line; the rule
    for the fixture is in the `record-fixture` skill.
11. **The cheap text rules enforced in `release.ps1`**: every bullet of the version's changelog section starts with an
    allowed prefix, the section is not empty (`tools/release-notes.ps1` run before the tag), and `main` is pushed
    before the tag so that a CI failure leaves no dangling tag. Cost: twenty lines. Done 2026-10-23 (`release.ps1`:
    the prefixes from the names of `docs/`, the notes from `tools/release-notes.ps1`, `main` pushed and its build
    watched with `gh` before the tag).
12. **A NuGet lock file for the test projects** (`RestorePackagesWithLockFile`), so that a test run in ten years
    restores exactly what runs today. Cost: one file per project. Done 2026-10-23 (`tests/Directory.Build.props`;
    the nine lock files are committed and move with Dependabot's bumps).
13. **The `archive/pre-public-history` branch out of the repository**: it is rewritten pre-public history that
    `PrivacyTests` never scans; pushed by mistake, whatever was scrubbed is public. Keep it as a bundle file outside
    git, or delete it. Cost: a decision. Open: the maintainer's.
14. **A yearly full NVDA pass on the current NVDA** (section 5), recorded with its environment, so that a change in
    NVDA is found in a year rather than in a user's report. Open: due October 2027, or sooner on the current NVDA.

Not proposed: trimming (WinForms does not trim), ReadyToRun (startup is fine), a `win-arm64` build (Windows on ARM
runs the x64 build; a second artifact doubles the release and the updater's rules), the publish flags in the project
file (a `RuntimeIdentifier` in `Axit.csproj` changes every build's output path and the test projects' references;
the flags stay in `publish.ps1`, the one way to build a release).

## 4. What breaks when, and what prevents it

| What | When | Prevented by |
| --- | --- | --- |
| .NET 10 leaves support | November 2028 | The yearly check (section 5); the move is `AxitDotNet`, `global.json`, the tests, a release. An installed copy keeps working regardless. |
| A GitHub Actions runner image or action changes under the workflow | any month | Pinned actions (3.5); the manual release path (3.3); the workflows are two files of twenty lines. |
| The GitHub API changes or rate-limits the installer | any day, for a classroom today | The redirect-based installer (3.4); the apps' check fails into a notice and nothing else. |
| The repository moves or its owner's account ends | years | The organization with a second owner (3.2); the redirect; the settings key. |
| Windows removes an API | years; MFPlay is the one with a notice | The API table with its fallbacks (3.6); every API is behind one file. |
| NVDA changes how it reads a control | each NVDA release | The yearly pass (3.14) with the environment recorded; the gotchas in `src/AxClaude/CLAUDE.md` say what is relied on. |
| Claude Code changes its screen reader mode | any release of Claude Code | The fixtures and the recorded version (3.10); AxClaude is a thin layer and the parser's patterns are one class. |
| Windows PowerShell 5.1, which the local scripts are written for, is retired | not announced; it is a Windows component | The scripts run under pwsh 7 in CI already; a pwsh-only Windows is a known edit to `Updater` (`powershell.exe`) and `prepare.ps1` (`System.Speech`). |
| The one maintainer is away | any time | Everything a stranger needs is written down: `README.md` (build, test, release), `CONTRIBUTING.md`, the specs, the notes, this page; the NVDA pass is the one thing a stranger cannot do alone, and the record says what "passed" meant. |
| The documents rot | every week of fast work | The tests that read the files (`InventoryTests`, `KeyTests`, `MenuTests`, `PrivacyTests`); the status in two places only; the review of 2026-10-23 as the baseline. |

## 5. The calendar

- **Every release**: `test.ps1` in Release; the NVDA plan steps the change touches, recorded with the environment; the
  changelog section with its date; the version named before the release.
- **Every month**: merge Dependabot's test-package and action bumps when CI is green (the pins of the actions and
  the lock files of the tests move with them).
- **Every year, in October** (the project's anniversary of becoming a bundle): the whole NVDA plan of every app on the
  current NVDA and Windows, recorded; this page reread; the .NET support window checked (`AxitDotNet` against
  the .NET support policy); the API table checked against Windows' deprecation notices; the GitHub addresses tried.
- **Every LTS of .NET** (November of even years): `AxitDotNet` and `global.json` moved, the tests run, the publish
  checked on a clean machine, a release; a line here saying when and what.
- **When a release is the last for a while**: the zip and `install.cmd` also kept outside GitHub (any cloud drive),
  since the installed program never needs the repository, but a new machine does.

## 6. What a stranger needs

Windows 10 or 11, the .NET SDK that `global.json` names (any 10.0.3xx), Windows PowerShell 5.1 (a Windows component),
git, and `gh` for a release. Then: clone, `dotnet build Axit.sln`, `.\test.ps1`, `.\run-axdown.ps1` to see a window,
`publish.ps1 -NoInstall` for the zip, `release.ps1 <version>` for a release (or the manual path, 3.3). NVDA for any
change to a window, following `docs/<app>/nvda-test-plan.md` on the files `manual_tests/prepare.ps1` makes. The
rules are `CLAUDE.md` and `CONTRIBUTING.md`; the specs are the truth; the notes are the traps already found.
