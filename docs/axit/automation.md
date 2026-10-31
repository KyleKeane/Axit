# Automated checks of the apps

The user's direction, 2026-10-23: the assistant drives every Axit app itself, with the keyboard events a user
presses, and reads back what the app and NVDA say, so that the NVDA test plans run as scripts, before releases, at
major moments and as spot checks during development. The user's own passes become sanity checks and experience
design. A workflow may change by choice; a script that fails says what else changed with it.

Three tiers, each usable alone.

## 1. Claude sessions without windows: `tools/QaRun`

Built 2026-10-23. Several Claude Code sessions at once through the app's console pieces (`PtyHost`,
`ClaudeLauncher`, `SessionModel`), each in a fresh folder under `%TEMP%\axit-qa`: the trust prompt, a message, its
reply, `/exit`. The model is chosen on Claude's command line (`--model haiku`, as `axclaude <folder> -- --model
haiku`), never with `/model`, which saves the choice as the user's default for every session. It found on its first
day that Claude drops an early Enter (axClaude FR-2.1).

## 2. The windows driven from inside: the automation channel

Built 2026-10-23: `Axit.Forms/AutomationChannel` (started by the window base when it first shows), `AXIT_HOME` in
`AppPaths`, `tools/AxAutomate` (axAutomate; called AxitDrive until 2026-10-24) with the test desktop, and the first scripts in `tests/scripts` (axDown's notices and
palette, axClaude's message and its set of two questions), all passing.

Run one:

```
dotnet build src\Axit\Axit.csproj -c Release
dotnet run --project tools\AxAutomate -c Release -- tests\scripts --build --report report.md
```

For the windows' own behaviour: keys, focus, announcements, notices, menus, the control area. Runs any time,
including while the user works, because nothing takes the foreground.

- **On only for a test run, and only for the runner** (security audit S-11, 2026-10-24). The runner makes a secret of
  256 random bits for each run and gives it to the windows it starts as `AXIT_AUTOMATION`; a window opens its channel
  only with a secret of 32 characters or more, and the client's first line must be that secret (compared in constant
  time), or the client is let go. `Axit.Core/AutomationEnvironment` reads the variables once, as the process starts,
  and takes them out of its environment: a shell in axTerminal, Claude, git or the user's own programs never inherit
  them; only a new Axit window of the same run gets them (`AxitMenu.StartWith`). Without a secret, no pipe exists and
  nothing is recorded. A window in a test run is shown without activation, so it never takes the keyboard from the
  user, and focus moves inside it with the thread's own active window.
- **Its own files.** The runner sets `AXIT_HOME` to a folder of the run's, under which the windows keep their
  settings, their logs and the recently closed list (`AppPaths`), so that a run neither reads nor writes the user's;
  the folder counts only together with the run's secret.
- **One named pipe per window**, `\\.\pipe\axit-auto-<process id>`, for the current user alone and refused to
  clients over the network (its access list); the runner connects with `CurrentUserOnly`, so a pipe another program
  made under the name is refused. A pipe already taken ends the channel, never the window. One command per line, one
  answer per line (JSON).
- **Secrets never leave**: a password field reads "protected" in `focus`, `text` and the timeline, as NVDA says it
  (S-12).
- **The timeline** (2026-10-23, the user's ask for full diagnostics): every window records, with the milliseconds
  since it opened, each announcement (with whether it interrupts), each sound by name (`Sounds.Played`; in a test
  run the sounds are recorded and not played, so a run is silent on the user's speakers), each move of the focus to
  another control with its name, role and value, and each change of the focused control's value (the caret's line,
  the selected item), which is what NVDA reads by itself. The runner's report lists it whole, and an expectation can
  wait for any of it.
- **The run's own checks**: after the last step a run fails when a window's process ended by itself or a log of the
  run holds an ERROR line, so a crash or a logged exception never passes unseen.
- **Commands**:
  - `keys <keys>`: key presses as a user makes them, `ctrl+shift+p`, `escape`, `down`, posted to the focused
    control through the window's own message queue with the modifiers set for the window's thread, so that
    `ProcessCmdKey`, the menus' shortcuts and the controls see them as typed.
  - `type <text>`: characters into the focused control, as typed.
  - `focus`: the focused control's accessible name and role, its text or the selected item, the caret's line.
  - `said`: what the window announced since the last `said`, each notification with its kind (interrupting or not),
    in order; the announcements are recorded where they are raised (`AxitForm.Announce`).
  - `state`: the title, the status bar's texts, whether a notice shows and its title, the control area's name.
  - `menu <name>`: runs a menu command by the name the command palette knows it by.
  - `close`: closes the window as Alt+F4 does.
  - `problems`: what is wrong with the window's command list (app-kit AK-2.4): a key of its own the F1 text does not
    name, a command an earlier one shadows. Every app's first script runs it as `check commands`, after its start.
- **The AI tools**: the runner can be the MCP client itself (`mcp start`, `mcp call`, `expect mcp`, `refuse mcp`):
  `Axit.exe mcp` as its child with the run's secret and folder, so that it reads the run's settings and key and never
  the user's. `tests/scripts/mcp` turns the access on, pairs the runner in the window's notice and reads the window
  (`docs/axit/mcp.md` MC-6). It never opens an app through `axit_open`, which would start it on the user's desktop.
- **Scripts**: `tools/AxAutomate` starts an app with the channel on and runs a script of steps (`mkdir`, `start`,
  `use`, `keys`, `type`, `menu`, `said`, `expect said|focus|state <text>`, `try said <text>` with `iffound <step>` for
  what may not come, such as Claude's trust question, `wait <ms>`, `close`; `file <path> <text>` writes a file, its
  path in quotes when it holds a space; the header of its `Program.cs` lists them),
  each step's result in a report; what a window said stays queued until an expectation takes it; a failing step prints what was said and focused instead. The NVDA test plans become scripts in
  `tests/scripts/<app>/`, one per section, with Claude always on `--model haiku`.

### What the scripts cover, and what stays manual (2026-10-23)

The NVDA test plans of axDown, axJSON, axPreview, axFile, axGIT, axTerminal, axMedia and axProcess (2026-10-24; the services only read, never started or stopped, since a run with administrator rights would do it for real) are scripts in
`tests/scripts/<app>`, one per section of the plan, about two thousand steps, all passing; axClaude's are being written,
on Haiku. Every step a script cannot drive is a `#` line naming the plan step and why. What stays manual, for now:

- The menu bar and context menus opened with Alt, F10 or Shift+F10: menu mode needs the foreground; `menu` runs the
  same command by its name. Submenus that fill as they open (Open recent, Recently closed, Open windows) are not in
  the palette's list either.
- The Windows pickers (Open, Save as, Font, the folder picker) and the shell's own windows (Open with, Properties,
  File Explorer, a default program).
- The clipboard: the test desktop shares it with the user's session, so copy and paste steps are not run.
- Coming back to a window with Alt+Tab (the files changed on disk); F5 is used instead where the app has it.
- What only NVDA shows, and sound and video quality: tier 3 and the user's ear.

The first runs found, and the same day fixed: axGIT's commit while a stage ran, axTerminal's "Done" before a
command's last line, two undo steps for Format, axPreview's size in whole megabytes, axMedia's status after a pause or
a clear and Go to time closing on a wrong time, and in axClaude the banner shown twice and the refused Enter of
Claude's first seconds. They also found plan steps the apps had outgrown, corrected in the plans.

## 3. NVDA in the loop

For what only NVDA can show: what it reads on a focus change, whether a notification cut it off, the order of
speech. NVDA follows the foreground window and the real keyboard, so this tier takes the machine: it runs when the
user says so and steps away, never on its own.

The add-on was installed and its capture checked on 2026-10-23: NVDA's speech and its cancellations were
recorded, and the first capture found a quote marker NVDA was given in every question's name (fixed the same day).
NVDA refuses to be started from the assistant's process ("Access is denied"), so the user installs it: the folder
`tools/nvda-capture` copied to `%APPDATA%\nvda\addons\axitCapture`, then NVDA restarted.

- **An NVDA add-on of the bundle's**, `tools/nvda-capture`, which records every speech sequence NVDA speaks to
  `%TEMP%\axit-nvda-speech.log`, only while the file `%TEMP%\axit-nvda-capture.on` exists, so that nothing the user
  hears in daily use is recorded. Installed once with the user's consent through NVDA's add-on store dialog.
- **The same scripts**, run with `--nvda`: the window is activated, the keys go through `SendInput` as a keyboard's
  do, and `expect spoken <text>` reads the capture file.

Built 2026-10-23: `--nvda` in `tools/AxAutomate` (the windows on the user's desktop, brought to the front through the
channel's `activate`, keys and characters through `SendInput`, `expect spoken`, `refuse spoken` and `spoken` over the
capture log, the capture switched on for the run and its log deleted after it). The NVDA scripts live apart, in
`tests/nvda-scripts`, so that a run of `tests/scripts` never takes the machine:

```
dotnet run --project tools\AxAutomate -c Release -- tests\nvda-scripts --build --nvda --report nvda-report.md
```

### From a Claude session (2026-10-31)

The assistant runs this tier itself to hear a change as NVDA speaks it, in the same session as the change:

1. Check the add-on is installed and current: `%APPDATA%\nvda\addons\axitCapture\globalPlugins\axitCapture.py` the
   same lines as `tools/nvda-capture`'s (a changed add-on is copied by the user and NVDA restarted). Nothing else of
   NVDA's is needed: not the Speech Viewer, not the user's own plugins in `%APPDATA%\nvda\globalPlugins` or the
   scratchpad.
2. Build `Axit.exe` and the runner into scratch folders (never the running app's `bin`), and write the script in
   `tests/nvda-scripts`; a first run without `--nvda` on the test desktop finds the focus order and the texts without
   taking the machine.
3. Ask the user through the question tool to step away, saying how long. The answer is the go: the run starts at once.
4. The run says in Windows' own voice (SAPI, not NVDA, so the capture holds only what the windows made NVDA say, and no
   window takes the focus) "Running NVDA tests. Please do not touch the keyboard or mouse.", then takes the foreground
   and the keys, and says "NVDA tests finished. The keyboard is yours." when it is over.
5. Read the result lines, filtered of account paths: each `expect spoken` prints what NVDA said up to the match,
   `[cancel]` where NVDA cut its speech (a key press does). The capture is on only during the run and its log is
   deleted after it.

The first such run, the consent reminder's check box (`tests/nvda-scripts/consent-checkbox.txt`), heard NVDA say
"checked" and "not checked" once each after the fix of AX-8.8.

## Rules

- Claude sessions in tests use Haiku through `--model haiku` and leave the user's settings alone.
- A test never activates a window or sends keys to the system outside tier 3, and tier 3 runs only at the user's word.
- Test folders are fresh under `%TEMP%`; recordings and reports follow the privacy rule (Redaction, never `--raw`).
- The scripts are run before a release (`release.ps1` names them), and after a change to a window as a spot check.
