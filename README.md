# Axit

One Windows program, several accessible apps for screen reader users, built for NVDA. **AxClaude** runs Claude Code in screen reader mode inside a hidden console and shows the conversation as plain text that NVDA reads line by line, with single-key navigation and a message field at the bottom; you are still talking to the real Claude Code. **AxDown** is a barebones editor for Markdown and text files that writes a file back exactly as it found it. Both share one installation, one updater and one release; adding an app follows `docs/axit/adding-an-app.md`.

**Download:** `install.cmd` or the zip on the [latest release](https://github.com/KyleKeane/Axit/releases/latest); `docs/axit/README.md` (the README inside the zip) explains the rest. Installed copies offer newer releases themselves under Help.

Where things are:

- `docs/axit/`: the bundle. `SPEC.md` (launch, install, update, release, shared pieces, the accessibility and modularity rules), `README.md` (shipped in the zip), `plan.md` (the steps from AxClaude to the bundle).
- `docs/axclaude/`: AxClaude. `SPEC.md`, `user-guide.md` (Help, User guide inside the app), `claude-screens.md`, `nvda-test-plan.md`.
- `CLAUDE.md`: the map and the rules for developing with Claude Code in this repository; each project under `src/` has its own `CLAUDE.md` with its notes.
- `src/Axit`: the executable. `src/Axit.Core`: shared pure code. `src/AxClaude`, `src/AxClaude.Core`: AxClaude's window and pure code. `tests/`: xunit tests and the recorded fixtures. `tools/PtyCapture`: records raw console output; the recordings are the parser's test fixtures.

## Build and run

```
.\run.ps1                                  # build, then start AxClaude on the current folder
.\run.ps1 C:\path\to\project               # start on that folder
.\run.ps1 C:\path\to\project -- --resume   # arguments after -- go to claude (default: --continue)
.\run.ps1 C:\path\to\project -New          # a new conversation (PowerShell swallows a bare --)
.\run.ps1 -NoBuild                         # start without building
.\run.ps1 -Test                            # run the unit tests
```

If PowerShell refuses to run the script: `powershell -ExecutionPolicy Bypass -File .\run.ps1`. Without the script: `dotnet build Axit.sln`, then `dotnet run --project src/Axit -- claude "C:\path\to\project"`. `Axit.exe claude …` and `Axit.exe down …` name the app; a folder or a file alone picks it.

Needs Windows 10 1809 or later, the .NET 10 SDK, Claude Code and, for testing, NVDA.

## Publish

```
.\publish.ps1                # self-contained Axit.exe + installer + README, zipped, then installed for this user
.\publish.ps1 -NoInstall     # build publish\win-x64 and the zip only
```

Send the zip. The recipient extracts it and double-clicks `install.cmd` (no administrator rights); `install.cmd -Uninstall` removes it again. Close a running installed Axit before publishing again. `tools\make-icon.ps1` draws the icons.

## Release

```
.\release.ps1 2.0.0          # tests, sets the version, commits, tags v2.0.0 and pushes
gh run watch                 # follows the GitHub Actions run that builds and publishes the release
```

Add a `## 2.0.0 - <date>` section to `CHANGELOG.md` first: it becomes the release notes, which the apps show in their update notice. GitHub Actions (`.github/workflows/release.yml`) tests, runs `publish.ps1`, and attaches the zip and `install.cmd` to the release at `github.com/KyleKeane/Axit/releases`; installed copies find it there. `build.yml` runs the tests on every push.

## Licence

MIT, see `LICENSE`. Axit is made by Dr. Kyle Keane (www.kylekeane.com). Use it, change it and pass it on; keep that notice with it.

The software is provided "as is", without warranty of any kind, and the developer accepts no liability for its use; the full wording is in `LICENSE` and at the end of the README in the zip. Axit is an independent project, not affiliated with or endorsed by Anthropic.
