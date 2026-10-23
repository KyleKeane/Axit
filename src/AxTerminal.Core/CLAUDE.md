# AxTerminal.Core, the pure code

Everything of AxTerminal that needs no window: which shell to start and how, the shell's output as a transcript with the state its integration marks give, the per-frame pass for the tick and the spoken output, the recent commands and the settings. The console pieces themselves (the ConPTY host, the parser, the screen, the line store, the mirror) are the bundle's, in `src/Axit.Core` (`Pty`, `Vt`, `Transcript`), moved there from AxClaude for this app. Unit tested in `tests/AxTerminal.Tests`, against recorded shell sessions under `tests/fixtures` (`shell-*.vt`, made with `tools/PtyCapture --shell`). The spec is `docs/axterminal/SPEC.md` ("SPEC.md" below); the window is `src/AxTerminal`, with its own `CLAUDE.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
dotnet test tests/AxTerminal.Tests/AxTerminal.Tests.csproj     # works while the app runs (this project is not locked)
AXTERMINAL_UPDATE_EXPECTED=1 dotnet test tests/AxTerminal.Tests/AxTerminal.Tests.csproj   # regenerate tests/fixtures/shell-*.expected.txt after a deliberate change; review the diff
dotnet run --project tools/PtyCapture -- --out session.vt --cwd C:\axit-fixture --cols 240 --rows 50 --script tests/fixtures/shell-powershell.script --shell powershell
dotnet run --project tools/PtyCapture -- --out session.vt --cwd C:\axit-fixture --cols 240 --rows 50 --script tests/fixtures/shell-bash.script --shell bash --env HOME=<a folder holding .config\git\git-prompt.sh with PS1='$ '>
dotnet run --project tools/PtyCapture -- --dump session.vt     # personal data replaced; never --raw into a session
```

A fixture is recorded in a neutral folder (`C:\axit-fixture`, with `a.txt`, `b.md` and `sub` in it), since the prompt prints the directory, and reviewed through the redacting dump before it is committed (bundle rule 6). The expected transcript is the visible lines as the reader sees them; `ShellFixtureTests` replays the recording chunk by chunk and sends each command of the script once its trigger (the prompt's B mark, or the question row) has arrived.

## Where things are

- `ShellLauncher` (SPEC.md AT-1.2, AT-1.3, AT-D2, AT-D7): `ShellKind`, `Find` (pwsh on the PATH or under Program Files, Windows PowerShell and cmd under the system root, Git's `bin\bash.exe` under the Git install folders or next to the git.exe on the PATH), `DefaultKind`, `BuildCommandLine` (PowerShell with `-NoLogo -NoExit -EncodedCommand` and the `PromptSnippet`; cmd alone; a custom line as typed), `BuildEnvironment` (the user's plus `TERM`, `GIT_PAGER=cat`, `GH_PAGER=cat`, cmd's `PROMPT` wrapped in the marks by `CommandPromptWithMarks`, and bash's `PROMPT_COMMAND`, `BashPromptCommand`), `Key`/`Parse`/`Describe` for the settings and the menus.
- `TerminalModel` (SPEC.md AT-1.4, AT-3.2, AT-4): on `Axit.Core.Transcript.LineStore`, which holds the lines, the screen and the parser, `Feed`, `EndFrame`, the bookmarks and the cap (`AttachScreen` adds the shell's handlers to the base's fresh screen); the marks (`OnMark`: D keeps the code, A ends a running command with `CommandFinished` or raises `Ready` once, B hides the prompt rows from the A row to the cursor row and keeps their text as `Prompt`); `Send` (the exchange block above the prompt rows while idle, at the end while a command runs, its `## Folder` and `## Command` headings, the folder from the directory the window passes or the 9;9 mark's; `record: false` for the unrecorded send), `AddSystemLine`, `Clear` (lines off screen go, lines on screen are hidden until the shell writes over them), `ResetScreen` (a restart), `ShellExited`; `Running`, `RunningCommand`, `RunningSince`, `Directory`, `HasMarks`, `CursorRowText` (for the window's "a program asks" heuristic).
- `TerminalArrivals` (SPEC.md AT-5.2, AT-5.3): the per-frame pass, a new visible shell line for the tick and the final, unspoken lines joined into one utterance (`Axit.Core.Transcript.Utterance`), lines told apart by id.
- `CommandHistory` (AT-3.6): the newest first, each once, fifty. `TerminalSettings` (AT-8.1): on the bundle's `WindowSettings`, `axterminal.json`.
- Tests: `TerminalModelTests` (hand-written streams with the marks), `ShellFixtureTests` (the recordings: the expected transcript, the state, the same transcript whatever the chunk boundaries), `ShellLauncherTests`, `CommandHistoryTests`, `TerminalSettingsTests`; `TestHelpers` (the fixture files, `Replay`, `Render`).

## Gotchas already learned (do not rediscover)

- With a screen reader running, PowerShell (Windows PowerShell and 7) turns PSReadLine off at startup and prints a warning line saying so: the input is the console host's own, with no colouring, no prediction, and `>> ` continuation rows printed by the host; and a multi-line block runs only after an empty line, so the window sends one more Enter after a multi-line command (SPEC.md AT-3.1). The warning is the shell's first line and stays in the transcript.
- `$?` must be the first thing the prompt function reads. The exit code follows Windows Terminal's rule (a new history entry means a command ran; `$?` true is 0; the newest error belonging to that entry is PowerShell's own, -1; otherwise `$LASTEXITCODE`); a session with no PowerShell error yet has an empty `$Error`, which the first draft of the rule took for "no code" and reported -1 for `cmd /c exit 3`.
- `cls` (PowerShell: cursor home, then every row erased; cmd: the same, then `ESC[3J`) reuses the screen's rows, whose lines sit at the top of the transcript: without letting the rows go first, the prompt and the output after `cls` land on those old lines and the erase blanks the output already read. `TerminalModel.LetRowsGo` detaches the rows on the screen's `Homed` and `Cleared` events, so the old lines keep their text and what follows gets new lines at the end (the shape of AxClaude's repaint handling).
- The snippet goes in as `-EncodedCommand` (base64 of UTF-16), so no quoting touches it; it is written with `[char]27` and `[char]7` because the backtick-e escape is PowerShell 7 only, and it wraps the user's own `prompt` function so that a profile's prompt keeps its text.
- cmd prints an empty row before every prompt and gives no exit code (`D` without one); its `set /p` echo loses the trailing space of the prompt text.
- A recorded chunk can hold a prompt mark and the echo and output that followed the script's send: the replay feeds a chunk up to the trigger's end, sends, then feeds the rest, or the block lands after the output it should precede (a harness artifact that looked like a model bug).
- Git Bash: Git's profile sets and exports `PS1` but leaves `PROMPT_COMMAND` alone, so the marks go in as a
  `PROMPT_COMMAND` environment variable (`BashPromptCommand`) that reads `$?` first, prints `D` from the second prompt
  on, `A` and the directory, and appends `B` to `PS1` at the first prompt. A `/c/…` directory becomes `C:\…` in bash
  itself (`${var^}` for the drive letter), anything else goes through `cygpath`. Git's `bin\bash.exe` is started
  `--login -i`; a `bash.exe` on the PATH is never used, since in System32 it is WSL's launcher. The fixture is recorded
  with `--env HOME=…` pointing at a folder whose `.config/git/git-prompt.sh` holds `PS1='$ '`, which Git's profile
  sources instead of its own `\u@\h` prompt, so that no user or machine name enters the recording.
