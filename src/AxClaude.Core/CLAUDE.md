# AxClaude.Core, the pure code

Everything of AxClaude that does not need a window: the VT stream to transcript conversion, the model of what Claude waits on, and the ConPTY host. Unit tested in `tests/AxClaude.Tests` against recorded fixtures under `tests/fixtures` (the golden files show joined wrapped rows, as the reader sees them). The spec is `docs/axclaude/SPEC.md` ("SPEC.md" below); the catalogue of Claude's screens is `docs/axclaude/claude-screens.md`. The window is `src/AxClaude`, with its own `CLAUDE.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
dotnet test tests/AxClaude.Tests/AxClaude.Tests.csproj     # works while the app runs (this project is not locked)
AXCLAUDE_UPDATE_EXPECTED=1 dotnet test                      # regenerate tests/fixtures/*.expected.txt after a deliberate parser change; review the diff
dotnet run --project tools/PtyCapture -- --out session.vt --script demo.script -- claude --ax-screen-reader
dotnet run --project tools/PtyCapture -- --dump session.vt  # personal data replaced; --raw shows it, never print that into a session
```

New fixtures follow the `record-fixture` skill and are reviewed for personal data (account name, paths, session ids) before they are committed. When the conversion changes, add or update a fixture-based test.

## Where things are

- `Vt`: `VtParser` (byte stream to print/control/CSI/OSC), `Screen` (cells, cursor, scroll region; rows carry their transcript `Line`), `Wcwidth`.
- `Transcript`: `SessionModel` (frames to lines: chrome hiding, classification, the wrapped-row join flag, markers around the `you:` echo, status; `PendingQuestion`, `PendingScreen`), `LineClassifier` (labels, chrome patterns, heading heuristic), `Line`, `Arrivals` (the per-frame pass behind the tick and the spoken replies: did a line from Claude arrive, and which reply lines and, when asked, tool call rows became final, joined into one utterance in the chosen mode), `TranscriptMirror` (visible lines to edit-control edits with `\r\n` or space separators, where the caret belongs afterwards, display line numbers, the find search, the reading breaks), `QuestionReader` and `ScreenReader` (what Claude waits on, read off the rows around the cursor: a question with numbered or y/n answers, or a screen waiting on a key-hint or tab row), `QuestionRouter` and `SlashCommands` (which dialog a question gets; the code form of `claude-screens.md`).
- `Pty`: `PtyHost` (ConPTY through P/Invoke, the one implementation; `tools/PtyCapture` records through it), `ClaudeLauncher` (find claude and list the places tried, install command, command line, environment), `StreamRecorder` (raw stream to a `.vt` file plus chunk index, the PtyCapture format).
- `AppSettings.cs`: AxClaude's settings, a plain class of properties read and written through the bundle's `Axit.Core/SettingsFile` (SPEC.md Appendix C; `%APPDATA%\Axit\axclaude.json`, the 1.x file copied on the first start). `Privacy`: `Redaction` (e-mail addresses, account paths, pipe names, session ids and the account name to placeholders; used by PtyCapture's dump and `PrivacyTests`), `GitExclude` (AxClaude's `axclaude-*` patterns in the project repository's `.git\info\exclude`, SPEC.md D34).
- In `src/Axit.Core` since step 4 of the plan: the sounds as WAV bytes (`Audio/WaveTone`), the update check and the updater (`Updates/`), the log and the paths.
- Tests: golden-file and chunking tests over the fixtures, chunk-by-chunk replays with the app's send timing (`ReplayTests`, `*.vt.chunks.txt`), session model, arrivals, mirror, reading break, settings and argument splitting tests, and `PrivacyTests` (the whole repository; it stays here because it tests `Redaction` too). `TestHelpers` holds what they share.

## Gotchas already learned (do not rediscover)

- Clear this process's standard handles around `CreateProcess` when attaching a child to a pseudo console (see `PtyHost.Start`). Otherwise a child started from a process with redirected stdio inherits those pipes, sees no TTY, and Claude Code silently switches to non-interactive print mode.
- Strip every `CLAUDE*` environment variable from the child's environment so a nested Claude Code session is not detected, and set `TERM=xterm-256color`.
- Startup can show dialogs before the prompt (workspace trust, one-time onboarding questions). They are flat `y/n` or numbered prompts in screen reader mode; the app must simply show them and let the user answer.
- Frames are bracketed by `ESC[?25l` and `ESC[?25h`. Chrome, finality and announcements are evaluated at frame end. Rows below the cursor row are chrome (autocomplete list, dialog hints). Only the cursor row may be treated as the `$` prompt; reply and tool rows can start with `$ ` too. On a full screen Claude can park the cursor on the mode line in the bottom row while it works; that row is chrome too, or the spinner and mode line above it are spoken with the reply (FR-3.5).
- Screen reader mode emits no SGR styling. Markdown headings arrive as plain short rows, and the first block of a reply shares the `claude:` row. Heading detection is structural (SPEC.md §7.6).
- Claude echoes `you:` for messages but not for slash commands or dialog answers. `BEL` and `ESC]133;C/D` arrive just before the final frame of a turn, so "Done" is announced after a short quiet period.
- A message sent while Claude works is drawn *inside* the working block as `you: …` over `ctrl+x ctrl+s to send now`, on rows that shift every frame; it is not the echo. The real echo is the `you:` row left behind when the hint disappears (see the `steering` fixture and SPEC.md FR-4.7). Never attach markers or hidden flags to rows inside the working block: the echo matcher skips chrome rows, and the markers of such a message wait for the echo (the user wants the message out of the conversation until Claude takes it up, SPEC.md D16).
- Claude hard-wraps long reply rows at the console width; the join rule (FR-3.2a) lives in `SessionModel.JoinWrappedRows` and the mirror renders the join. A labelled row (`claude:`, `tool:`) starts with a lowercase letter too, which is why only `Plain` rows inside a reply or echo block may join.
- The draft is printed without a cursor hide/show bracket; a lone `\n` write is Ctrl+J (newline in the draft), verified in the `steering` fixture. Text plus `\n` in one write is a paste.
- The default mode line is `manual mode on` with no `(shift+tab to cycle)` hint. Mode changes and similar confirmations appear for one frame as a bracketed row under the prompt (`[accept edits on]`) with the cursor parked on it; the app speaks those.
- A tool result, a replayed conversation or a slash command can print any row, including `you: …`, `Permission Required:`, `Enter y/n:` and `[Screen Reader Mode: …]`. Text alone never proves Claude's state: an echo must repeat a sent text, a question is pending only while the cursor sits on the prompt row, and the replay boundary of a restart is a remembered line, not a text match. Rows that scroll away inside one frame are classified when they are committed.
- `TranscriptMirror.Update` reuses its lists between frames: a frame that changes nothing visible (a spinner tick) must not allocate, and a 20 000-entry list goes to the large object heap. The update it returns is valid until the next call.
- `SessionModel.EndFrame` walks the whole transcript several times per frame. A lambda inside one of those loops that captures a local declared in the loop body makes the compiler allocate a closure on every iteration (that was 700 KB a frame in `HandleEchoes`); keep such work in a separate method called only for the rare line. The steady state is 0.12 ms and zero allocation at 18 000 lines; measure with a scratch console app against `AxClaude.Core` before and after touching that path.
- Personal data (SPEC.md D34): read recordings through PtyCapture's redacting `--dump`, never `--raw`, `cat` or `type`; keep `PrivacyTests` passing.
