# Axit

[![Build](https://github.com/KyleKeane/Axit/actions/workflows/build.yml/badge.svg)](https://github.com/KyleKeane/Axit/actions/workflows/build.yml)
[![Latest release](https://img.shields.io/github/v/release/KyleKeane/Axit?label=release)](https://github.com/KyleKeane/Axit/releases/latest)
[![Licence: MIT](https://img.shields.io/badge/licence-MIT-blue)](LICENSE)

Accessible Windows apps for screen reader users, built for and tested with NVDA, in one program with one installer and one updater. Every app is made of native Windows controls that NVDA reads reliably, works without a mouse, says the result of each command once, and keeps its questions and errors inside its own window.

## The apps

- **AxClaude**: Claude Code in screen reader mode, shown as a plain-text conversation that NVDA reads line by line, with single-key navigation, a message field, and Claude's questions as lists. You are still talking to the real Claude Code.
- **AxDown**: a barebones editor for Markdown and text files that writes a file back exactly as it found it, with keys for headings, links, lists, quotes and tables.
- **AxJSON**: the same editor for JSON, with keys for keys, objects, arrays, items and errors, and a Format command that repairs what it can.
- **AxGit**: a git manager for one repository: changes, the log and the branches as lists, staging, committing, fetch, pull and push. A first draft.
- **AxTerminal**: PowerShell, Command Prompt or Git Bash as a transcript in AxClaude's kind of window. A first draft.
- **AxFile**: a file manager for one folder, with an Actions list of every operation that Ctrl+Z can take back, selection, zip files, favourites and the drives.
- **AxMedia**: a player for sound and video files with one queue, speed with the pitch kept, and seeking by key.
- **AxPreview**: a read-only viewer for one text file, with the editors' navigation keys.
- **AxStart**: a program launcher. One key from anywhere, Ctrl+Shift+Space unless you choose another, brings up a search field over the Start menu's programs; Down and Up move through the matches while the caret stays in the field. Not yet released.

The apps reach each other: AxFile hands a file to AxMedia or AxPreview, every window can show its folder in the others, and the Axit menu in every window lists the open windows and starts any app.

## Install

Download `install.cmd`, or the zip, from the [latest release](https://github.com/KyleKeane/Axit/releases/latest) and run it; no administrator rights are needed. It adds Start menu entries ("Axit AxClaude" and the others), commands such as `axfile` and `axdown`, and an "Open with Axit" submenu in File Explorer. Installed copies offer each new release themselves: Axit menu, Check for updates. `docs/axit/README.md` is the README inside the zip, with the rest.

Needs Windows 10 1809 or later. AxClaude also needs [Claude Code](https://code.claude.com/docs/en/setup); every other app works offline.

## Build and run

Needs the .NET 10 SDK, and NVDA for the manual checks.

```
.\run-axfile.ps1 C:\path\to\folder       # build, then start AxFile on that folder
.\run-axclaude.ps1 C:\path\to\project    # AxClaude on a project folder
.\run.ps1 -App down C:\notes\todo.md     # any app: claude, down, json, git, terminal, file, media, preview, start
.\run.ps1 -NoBuild                       # start without building
```

There is a `run-ax<app>.ps1` for each app. Close a running Axit before building: the build writes into the folder it runs from. If PowerShell refuses to run scripts: `powershell -ExecutionPolicy Bypass -File .\run.ps1`.

## Test

```
.\test.ps1                 # every test project, in Release, the way GitHub Actions runs them
.\test.ps1 AxFile          # one project
.\manual_tests\prepare.ps1 # the files of the NVDA test passes, made new
```

The unit tests cover everything that needs no window. What a window does is checked with NVDA, following each app's `docs/<app>/nvda-test-plan.md` on the files in `manual_tests/<app>`; each folder's `_main.md` says which section uses what. Every pass is written down in `docs/axit/nvda-test-record.md`, by step, version and commit.

## Release

```
.\release.ps1 3.4.0        # tests, sets the version, commits, tags v3.4.0 and pushes
gh run watch               # follows the workflow that builds and publishes the release
```

Add a `## 3.4.0` section to `CHANGELOG.md` first: it becomes the release notes, which the apps show in their update notice. The Release workflow tests, builds the self-contained `Axit.exe`, and publishes the zip and `install.cmd`; the Build workflow tests every push to every branch. `.\publish.ps1` makes the same zip locally and installs it for this user.

## Where things are

- `src/Axit`: the program. `src/Axit.Core` and `src/Axit.Forms`: the pieces the apps share.
- `src/<App>` and `src/<App>.Core`: each app's window and its pure code. Each folder has a `CLAUDE.md` with its notes.
- `tests/`: the unit tests, one project per app, and the recorded console fixtures.
- `docs/<app>/`: each app's `SPEC.md`, `user-guide.md` (Help, User guide in the app) and `nvda-test-plan.md`. The specifications are the source of truth.
- `docs/axit/`: the bundle's `SPEC.md` (launch, install, update, release, the accessibility and modularity rules), `plan.md`, `adding-an-app.md` and the test record.
- `manual_tests/`: the script that makes the files for the NVDA passes.
- `CLAUDE.md`: the map and the rules for working on this repository with Claude Code.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Report a bug or an idea with the [issue forms](https://github.com/KyleKeane/Axit/issues/new/choose); report a security problem privately, as [SECURITY.md](SECURITY.md) says.

## Licence

MIT, see [LICENSE](LICENSE). Axit is made by Dr. Kyle Keane (www.kylekeane.com). Use it, change it and pass it on; keep that notice with it.

The software is provided "as is", without warranty of any kind, and the developer accepts no liability for its use; the full wording is in `LICENSE`. Axit is an independent project, not affiliated with or endorsed by Anthropic or NV Access.
