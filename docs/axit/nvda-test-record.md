# NVDA test record

What was checked with NVDA, against which version, and with what result. When a bug comes up, this says when its step last passed, and so which changes since then to look at. The plans are each app's `docs/<app>/nvda-test-plan.md`; the files they use are made by `manual_tests/prepare.ps1` (`manual_tests/_main.md`).

## How to add a pass

Add a section under this one, above the earlier passes, so that the newest comes first: the date, the version, the commit tested, who ran it and how the result was given, and the environment: the NVDA version, the synthesizer and the Windows build (so that a regression is dated to an NVDA release rather than to some time). Under it, one table per app with a line per section of its plan: the steps run and the result, Passed, Failed (with what was heard) or Not run. A step that fails gets a line of its own with what NVDA said and what it should have said, and the bug's fix names the step. When a plan gains or loses steps, the next pass lists them as the plan has them then.

## 2026-10-31: the consent box's state, by the assistant (branch `licence`, after c634215)

Run by the assistant through the NVDA tier (`tests/nvda-scripts/axterminal-consent-checkbox.txt`, `docs/axit/automation.md`
section 3), the user away at its question; NVDA's speech read from the capture add-on. The user had reported that
Space in the box said nothing.

| Step | Result |
|---|---|
| axTerminal's first start: the reminder's title and text spoken | Passed |
| Tab: "I understand that … check box not checked" | Passed |
| Space: "checked", once | Passed |
| Space again: "not checked", once | Passed |

## Owed: access layers and consent (the branch `licence`, 2026-10-29, `docs/axit/SPEC.md` AX-11)

Not run yet; before the branch merges:

- The titles: each access layer's window title, New window, Open windows, Recently closed and Show in read "axClaude, access layer for Claude Code" and the like, after a separator below the standalone apps; F1 starts with the same line for the four.
- The consent notice (axGIT's first open and axTerminal's first shell are scripts since 2026-10-31,
  `tests/scripts/axgit/06-consent.txt` and `tests/scripts/axterminal/07-consent.txt`, heard through `nvda-pass.ps1`): axClaude's first start, axTerminal's first start of Windows PowerShell and of Command Prompt, axGIT's first open and first fetch to github.com, axPass's first unlock and New database. Each: the title "Before <app> connects to <program>", the text, Tab to "check box, not checked, I understand that …", Enter before checking says "Check the box first", Space checks, Enter connects; only two buttons, Connect and Not now; Not now leaves the line said in the spec. The second start asks nothing.
- Install it now in each app: "Before <app> installs <program>" with Install, then the installer.
- The AI tools' pairing notice: Allow before checking says "Check the box first"; Enter is still Deny.
- The update notice: the terms sentence first, and the buttons "I accept all terms, install and restart" and "… install and close" (axClaude plan steps 138 to 140, axDown's update steps).
- About, Licence: NOTICE.md opens with "What this is", and ends with "What you have agreed to on this computer", its lines, or "Nothing yet."
- axClaude: /login says "Sent. You are signing in to Anthropic …"; axGIT: the first fetch of a window says "Connecting to github.com; …" and the result is still heard.

## 2026-10-25: every owed pass (main acae61f, released as 3.7.4)

Run by the user from the checkout on the branch `inventory` (acae61f), the result given as "all nvda tests are complete"; NVDA
version, synthesizer and Windows build not given. Every section that was owed, as it stood, passed; each keeps its steps
below as the record of what was checked.

### the inventory and the review of 3.7.x (the branch `inventory`, 2026-10-25): Passed

Automated: every app's scripts pass on the branch. By ear:

- Every window: Ctrl+V into axClaude's message field, axDown's text, axTerminal's command field, a Find or Go to line field says "Pasted" ("Pasted, 3 lines" for several lines into a text of several); with a picture on the clipboard, "Nothing to paste as text"; in axPreview's text, nothing.
- axProcess: a process's size reads "350.4 MB" with one decimal.
- axGIT: Ctrl+F in the list says "Name contains" and finds a line of the list; F3 the next; in the details, the text find as before.
- axTask and axPass: Shift+F10 (and the Applications key) opens the List or Group menu on a list, the Item or Entry menu on an item, the Field menu in the detail pane, and NVDA reads its first item; a locked axPass's menus stay unavailable.
- axPreview: Ctrl+E on an `.exe` or a `.lnk` gives the notice "Not run from axPreview"; on a picture, its program still opens.
- axMedia and axFile: the queue and the Actions list still change one line at a time while something plays or copies, the line you are on kept.
- Page Up, Page Down, Ctrl+Up and Ctrl+Down in axDown, the conversation and the notices, and the heading keys in a notice: as before (their code moved).

### axClaude's side answer as read-only text (the branch `kit-j`, docs/axclaude/SPEC.md FR-2.11): Passed

`/btw` and the other screens that only inform (`/goal`, `/tasks` with nothing running) are a notice in the window, not
a list in the control area; automated in `tests/scripts/axclaude/14-side-answer.txt`. By ear, axClaude's plan section
12, step 22 and the new step 6:

- `/btw <question>` from the message field: the trill, the notice's title and the answer read; Down Arrow reads
  "Enter or Escape: Close."; NVDA+End "Claude: showing a side answer"; Tab: Close, Copy (c), Fork (f).
- Escape and Enter both close it, "Closed", the message field back, NVDA+End "Claude: ready".
- A second `/btw`: Browse (Shift+Left) changes the text with the focus kept on the button.
- From the conversation, the notice comes a second later; with Go to them at once off, nothing is cut off.
- `/tasks` with nothing running: the notice "Background"; with a task, still the list.
- Since 2026-10-25: c in the answer's text copies it, as the Copy button does (paste it somewhere); f forks. A long
  answer that arrives in parts keeps its first lines in the text.

### the command list of every window (the branch `app-kit`, `docs/axit/app-kit.md` AK-2): Passed

Every window's keys moved from its key switch onto one list of commands, meant to change nothing a key does; the
automated scripts of every app pass. Heard with NVDA, a short round in each app's plan, and these steps above all:

- axFile: F2 starts the rename a moment after the key, as the menu's Rename always did: the box and its text read.
- axClaude: with the menu bar open (Alt), Ctrl+N, Ctrl+Shift+R and the other keys that were menu shortcuts do nothing
  until the menu closes, as every other key of the window already did (AX-6.6); outside the menu they act as before.
- axTask, axPass: in the rename box (F2), Ctrl+Z and Ctrl+C act on the box's text, never on the list.
- axPass: F5 says "Refreshed" and shows an entry added in KeePassXC's own window meanwhile.
- axStart, axClip: the six match keys, Escape hiding, and the window coming back on the hotkey with the field empty.
- axGIT: Enter in each view: the diff in Changes, the switch in Branches, the commit in Log.

### the AI tools, first stage (the branch `app-kit`, `docs/axit/mcp.md` MC-6): Passed

Automated in `tests/scripts/mcp` (the runner as the client); by ear with NVDA, in any window:

- Axit menu (Alt+A), AI tools (I): "Let AI tools read and open Axit" read with its checked state; turning it on asks
  "Turn on AI tools?", Enter turning it on and adding Axit to Claude Code ("Axit is added to Claude Code");
  turning it off says "AI tools off".
- Then a new Claude Code session, asked to list the Axit windows: its first call brings the
  pairing notice with its chime into the newest Axit window, which flashes when it is not in front; the title names
  the client, the text its program; Enter at once does nothing for a moment, then Deny; Tab to Allow, Enter: "Claude
  Code may read Axit".
- A pairing while another notice is open shows nothing and the client hears "ask again".
- Forget AI tools: "AI tools forgotten"; the next call asks again.

### the installer reads the apps from Axit.exe (3.7.0, `docs/axit/app-kit.md` AK-4.1): Passed

- On a computer with Axit installed (not the development machine), update to 3.7.0 or run install.cmd from its zip:
  every app is in the Start menu ("Axit axCalc" and the rest), the console commands start them, File Explorer's
  "Open with Axit" lists the same apps as before on a folder, a file, a .json file and a sound file, and Settings,
  Apps, Axit, Uninstall removes it all.

### Install it now (3.7.0, `docs/axit/SPEC.md` AX-2.9, after the review of 2026-10-24): Passed

- axGIT without Git, axPass without KeePassXC, axClaude without Claude Code: the notice's last line says "Enter:
  Close" and Enter closes; Tab to Install it now and Enter installs, the progress notice reads, and the app goes on.
- During an install, Escape hides the progress notice; open another notice (F1): the install's end does not close it,
  and a failure is said, not shown over it; Install it now again says it is being installed already.
- axTerminal: Session, Switch shell opens at once and lists only the shells there; after Install PowerShell 7 it
  lists it.

### merged to main on 2026-10-24 before their pass (the branch `calc-clip`): Passed

Merged at the user's word ahead of the rule, so that main holds them; no release until these have passed.

- axCalc, `docs/axcalc/nvda-test-plan.md`: every section (new app; its history became the bundle's list at the merge).
- axClip, `docs/axclip/nvda-test-plan.md`: every section (new app).
- axStart, `docs/axstart/nvda-test-plan.md`: the typing, the keys in the field and the edge sound, hiding and the
  hotkey (its window moved onto `HotkeyForm`, and the search field into it).
- The cleanups of `docs/axit/inventory-2026-10-24.md`, meant to change nothing a key does: axGIT's and axProcess's
  views (`ListViewState`), the copy keys of axFile, axPass, axProcess, axCalc and axClaude (`CopyText`), the Show
  current folder in submenus of every window (the names now axGIT, axTerminal, axFile), axDown's and axPreview's
  Markdown keys (`MarkdownKeys`), axFile's Go to folder completion and Open with (moved files).
- The app lists and Show in (AX-9.2, AX-9.10): Alt+A, N and Alt+A, B in any window, the four groups in order with the
  separators skipped; axFile's Item, Open in Axit (no utilities); Show current file in from axDown, axPreview and
  axMedia (axClaude now there); Recently closed after closing axCalc and quitting axClip.

### the security fixes of 2026-10-24 (the branch `security`, `security-audit-2026-10-24.md`): Passed

- axGIT: open `manual_tests/axgit/scratch-repository-with-a-hook` (plan section 1, step 7): the notice names the hook;
  Keep them off, then Trust this repository.
- axPass: Ctrl+C, then Windows+V: the password is not in Windows' clipboard history; after 30 seconds the clipboard is
  empty; Windows+L locks axPass ("Locked" when you come back); Ctrl+U on an entry whose URL is not a web address says
  "Not opened".
- axTerminal: `type` a file holding the shell marks (the plan's new step): no "Done", the command still running.
- axClip: Keep history between runs on, quit, open `axclip.json`: no copied text in it; start again: the history back.
- Every window: Copy diagnostics, paste it into axDown: the account's name reads `<user>`.
- axClaude's `tests/scripts/axclaude/09-options.txt` fails at step 191 on main as on this branch (the second line of the
  conversation after a forced restart is the recording's line): an older break, most likely Claude Code's output, to be
  looked at with a recording.
## 2026-10-24: axPreview as the general viewer (the branch `preview`, before 3.7.2)

Commit 99ba45a, run from the checkout by the user, the result given as "nvda tests pass"; NVDA version, synthesizer and
Windows build not given. axPreview's plan section 5 (the Details list and the kinds of file) and step 1.9, with the
automated 	ests/scripts/axpreview (five scripts) passing on the same build.

| App | Section | Result |
| --- | --- | --- |
| axPreview | 1.9 (every file opens) | Passed |
| axPreview | 5 (Details list, kinds, Ctrl+E, Open with) | Passed |

Afterwards, Tab to the Details list was reported missing on a build made a minute before the merge (it had no list);
on 3.7.2's code, rebuilt from main, the user confirmed it: "success".

## 2026-10-24, axTask and axPass (the branch `axtask`, before its merge)

- Run by the user with NVDA on builds of `axtask` up to cffd363, over the day's rounds of asks; the result given as
  "all tests pass for all pending requests". NVDA version, synthesizer and Windows build not given.
- axTask, `docs/axtask/nvda-test-plan.md`: Passed, after its rounds were built and heard again: the sections of the
  top level with Ctrl+Up and Ctrl+Down (1.1), Ctrl+M and the Section field (1.9), one level of lists, F2 in place.
- axPass, `docs/axpass/nvda-test-plan.md`: Passed, with KeePassXC 2.8.0: unlocking, the fields, the copies, Ctrl+M,
  the password fields named as instructions (4.9) and the backups (4.10).
- The shared pieces on the branch, heard through these apps: `AxitList` (type-to-find, the edge earcon, the rename in
  place moved from axFile), `ItemForm`, the hidden notice field. axFile's rename in place and the edge sound in axGIT
  and axMedia were among the pending requests the user's words cover; not listed step by step.
- Automated alongside: `tests/scripts/axtask` (three scripts), `tests/scripts/axpass`, `tests/scripts/axfile`, all
  passing; every test project.

## 2026-10-24, axProcess (the branch `axprocess`, before its merge)

- Run by the user with NVDA on a Debug build of `axprocess` at 479792e, started from a scratch folder; the result given
  as "axprocess seems to be working and all my tests pass".
- axProcess, `docs/axprocess/nvda-test-plan.md`, every section: Passed, after the first round's asks were built and
  heard again: the edge earcon at the ends of the lists (1.7), Tab and Shift+Tab round the views (1.8), Shift+F10 and
  the Applications key opening the view's menu (1.9), only the view's menu enabled (1.9a), type-to-find with Shift and a
  letter backwards (1.9b), axProcess in Recently closed (1.10).
- In the same build and so heard with it: type-to-find with Shift backwards in axFile's folder and Actions lists,
  axGIT's list and axMedia's queue (AX-8.6), and axFile's Shift+F10 through the window base's `OpenBarMenu`; not run
  as their own plans' steps.
- Automated alongside: `tests/scripts/axprocess` (two scripts) and the axFile, axGIT and axMedia scripts, all passing.

## 2026-10-23, Axit 3.5.0 (the branch `palette`)

- Run by the user with NVDA, step by step in batches, on `palette` from 0d46e82 to the release; the fixes the pass asked for went in as it ran, each heard again. The user stopped tired before the last apps and released; still owed, for the next pass: axGIT steps 4a and 5, axMedia steps 1a and 2a, axPreview step 9a (all passing in the automated scripts), axStart section 1 step 7 and section 5 steps 1, 2 and 4 (the Start menu, sign-in and the update in place, on the installed release), the help section's steps 5 and 8 of axClaude.

Run so far, by the user with NVDA, 2026-10-23, on `palette` after 0d46e82:

- axStart section 1 steps 1 to 6 and 5a, section 2 steps 2, 3, 5 and 9: Passed. The first run was against a stale
  axStart from a scratch build of 2026-10-22, which kept the hotkey; on the current build all passed, but the edge
  note was "a bit soft and swallowed by the speech". The note was made louder and two notes falling an octave, and
  the steps passed again with it.
- axStart section 2 steps 1, 4, 6, 7 and 8, section 3 steps 1 to 8: Passed. Section 1 step 7 (the Start menu
  shortcut) waits for the release, since the shortcut starts the installed copy.
- axStart section 4 steps 1 to 4 and 6 (step 5 with Windows+E), section 5 steps 3 and 5: Passed. Section 5 steps 1, 2
  and 4 (sign-in and an update in place) wait for the release.
- axClaude section 12 steps 4a, 18, 19, 20 and 21: Passed, in a second window on
  `manual_tests/axclaude/project-for-conversations`.
- axClaude Session, Open folder in new window and Open recent folder in new window (new, dfe9ec1): Passed.
- axDown section 6 step 6 (the palette) and section 2 step 6's Alt+Backspace and Shift+F10: Passed.
- axFile section 3 step 7: Failed at first (Shift+F10 opened no menu, then a context menu silent on its first opening); now Shift+F10, the Applications key and a right click open the bar's Item menu as Alt+I does (AF-D13): Passed, "good enough for now". Alt+F4 on axFile left axClaude's Session menu open: fixed in the window base, Passed.
- axFile, by ear: the menu bar walk (3.7a), Open in Axit (3.9), Copy and Paste (3.6, with the paths as text and File Explorer both ways), two copies side by side (3.6), Move to folder with completion (3.1a, new), the palette (5.6): Passed.
- axTerminal: Speak output all lines by default (AT-5.3, decided at this pass); Ctrl+Up and Ctrl+Down through the history with the recall earcon and the changed command kept as the last item (AT-3.9, new): Passed, "working perfectly".
- axTerminal: the unrecorded command's status, an output line that starts like the prompt, a two-row prompt, New session's fresh screen, Open recent folder in new window, the stop at once, the quick keys reading one row (016d23a): Passed.
- axFile steps 2a (type-ahead), 10a (filter and sort), 12b (Ctrl+N at This PC), 13a (marks under a filter): Passed.

## 2026-10-22, Axit 3.3.1

- Commit: the `split` branch at its merge into `main` (8504ede), released as 3.3.1; 3.3.0 was tagged from it but not published (a test the Release build refused, fixed in 2b91f7a, no change to the apps).
- Run by the user, with NVDA. The user asked for every step of every plan to be marked as tested, and will report anything that comes up; no step was reported failing. The results below are that word, not a step-by-step log.
- In it, among the rest: the split of AxFile into AxFile, AxMedia and AxPreview; the Actions list with its sections and selection lines; Shift+Space and the Selection menu; Escape deselecting; Left and Right in the folder list; completion in Go to folder; the Drives submenu and Alt+D; AxDown's and AxJSON's undo and redo from 3.2.2.

### AxClaude

Plan: `docs/axclaude/nvda-test-plan.md`. Files: `manual_tests/axclaude`.

| Section | Steps | Result |
| --- | --- | --- |
| 1. Startup and focus | 1.1, 1.2, 1.3, 1.4, 1.4a, 1.5, 1.6, 1.7 | Passed |
| 2. Dialogs and plain conversation | 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.7a, 2.8 | Passed |
| 3. Reading while output arrives | 3.1, 3.2, 3.3, 3.4, 3.5 | Passed |
| 4. Headings and quick keys | 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 4.7a, 4.8, 4.9, 4.10, 4.11, 4.12, 4.13, 4.14, 4.15 | Passed |
| 5. Permission mode, prompts and interruption | 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7 | Passed |
| 6. Sending while Claude works | 6.1, 6.2, 6.3 | Passed |
| 7. Project folder | 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7, 7.8 | Passed |
| 8. Lifecycle | 8.1, 8.2, 8.2a, 8.3, 8.4, 8.5, 8.6, 8.7, 8.8 | Passed |
| 9. Options and diagnostics | 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7, 9.8, 9.9, 9.10 | Passed |
| 10. Claude Code missing, and the installer | 10.1, 10.2, 10.3 | Passed |
| 11. Updates | 11.1, 11.2, 11.3, 11.4, 11.5, 11.6 | Passed |
| 12. Claude's questions and screens: the control area (D33) | 12.1, 12.2, 12.3, 12.4, 12.5, 12.6, 12.7, 12.8, 12.9, 12.10, 12.11, 12.12, 12.13, 12.14, 12.15, 12.16, 12.17 | Passed |
| 13. The Axit menu (bundle AX-9) | 13.1, 13.2, 13.3, 13.4, 13.5, 13.6 | Passed |

### AxDown

Plan: `docs/axdown/nvda-test-plan.md`. Files: `manual_tests/axdown`.

| Section | Steps | Result |
| --- | --- | --- |
| 1. Install and start | 1.1, 1.2, 1.3, 1.4 | Passed |
| 2. Reading and writing | 2.1, 2.2, 2.3, 2.3a, 2.4, 2.5, 2.6 | Passed |
| 3. Files | 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9, 3.10, 3.11 | Passed |
| 4. Find and go to line | 4.1, 4.2, 4.3 | Passed |
| 5. Text size and font | 5.1, 5.2 | Passed |
| 6. Menus and help | 6.1, 6.2, 6.3, 6.4, 6.5 | Passed |
| 7. Updates and uninstall | 7.1, 7.2, 7.3 | Passed |
| 8. Navigation (from the version with section 9 of the spec) | 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7 | Passed |

### AxJSON

Plan: `docs/axjson/nvda-test-plan.md`. Files: `manual_tests/axjson`.

| Section | Steps | Result |
| --- | --- | --- |
| 1. Start | 1.1, 1.2, 1.3, 1.4 | Passed |
| 2. Navigation (AJ-2.2) | 2.1, 2.2, 2.3, 2.4, 2.5, 2.6 | Passed |
| 3. Format (AJ-2.3) | 3.1, 3.2, 3.3, 3.4 | Passed |
| 4. The Axit menu and About | 4.1, 4.2 | Passed |

### AxGit

Plan: `docs/axgit/nvda-test-plan.md`. Files: `manual_tests/axgit`.

| Section | Steps | Result |
| --- | --- | --- |
| 1. Start (AG-1, AG-2) | 1.1, 1.2, 1.3, 1.4, 1.5, 1.6 | Passed |
| 2. From and to the other windows (AG-1.4) | 2.1, 2.2, 2.3, 2.4, 2.5 | Passed |
| 3. Changes (AG-3) | 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7 | Passed |
| 4. Log and branches (AG-4, AG-5) | 4.1, 4.2, 4.3, 4.4, 4.5 | Passed |
| 5. Text, notices, the Axit menu (AG-2.4, AG-6) | 5.1, 5.2, 5.3, 5.4, 5.5, 5.6 | Passed |

### AxTerminal

Plan: `docs/axterminal/nvda-test-plan.md`. Files: `manual_tests/axterminal`.

| Section | Steps | Result |
| --- | --- | --- |
| 1. Start (AT-1, AT-2) | 4 checks | Passed |
| 2. Commands (AT-3, AT-5) | 10 checks | Passed |
| 3. Shells and folders (AT-1.2, AT-1.6) | 6 checks | Passed |
| 4. Text, notices, the Axit menu (AT-4, AT-6) | 6 checks | Passed |

### AxFile

Plan: `docs/axfile/nvda-test-plan.md`. Files: `manual_tests/axfile`.

| Section | Steps | Result |
| --- | --- | --- |
| 1. Start and the list (AF-1, AF-2, AF-3) | 1.1, 1.2, 1.3, 1.3a, 1.4, 1.4b, 1.5, 1.6, 1.7, 1.8, 1.9, 1.10, 1.10a, 1.11, 1.12, 1.12a, 1.13 | Passed |
| 2. From and to the other windows (AF-1.3) | 2.1, 2.2, 2.3, 2.4 | Passed |
| 3. File operations and the entry's menu (AF-4) | 3.1, 3.1a, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.7a, 3.7b, 3.8, 3.9, 3.10, 3.11, 3.12, 3.13, 3.14 | Passed |
| 4. Sound, video and reading: the hand-overs (AF-5) | 4.1, 4.2, 4.3, 4.4, 4.5 | Passed |
| 5. Text, notices, the Axit menu (AF-6) | 5.1, 5.2, 5.3, 5.4, 5.5 | Passed |
| 6. The Actions list (AF-9) | 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7, 6.8, 6.9 | Passed |

### AxMedia

Plan: `docs/axmedia/nvda-test-plan.md`. Files: `manual_tests/axmedia`.

| Section | Steps | Result |
| --- | --- | --- |
| 1. Start and the queue (AM-1, AM-2, AM-3) | 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8 | Passed |
| 2. The hand-over (AM-1.2, AM-1.3) | 2.1, 2.2, 2.3, 2.4, 2.5, 2.6 | Passed |
| 3. Playing (AM-4) | 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9 | Passed |
| 4. Text, notices, the Axit menu (AM-5) | 4.1, 4.2, 4.3, 4.4 | Passed |

### AxPreview

Plan: `docs/axpreview/nvda-test-plan.md`. Files: `manual_tests/axpreview`.

| Section | Steps | Result |
| --- | --- | --- |
| 1. Start and the text (AP-1, AP-2) | 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.9, 1.10, 1.11, 1.12 | Passed |
| 2. The hand-over (AP-1.2, AP-1.3) | 2.1, 2.2, 2.3, 2.4, 2.5, 2.6 | Passed |
| 3. Text, notices, the Axit menu (AP-3) | 3.1, 3.2, 3.3, 3.4 | Passed |

