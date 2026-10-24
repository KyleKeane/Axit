# NVDA test record

What was checked with NVDA, against which version, and with what result. When a bug comes up, this says when its step last passed, and so which changes since then to look at. The plans are each app's `docs/<app>/nvda-test-plan.md`; the files they use are made by `manual_tests/prepare.ps1` (`manual_tests/_main.md`).

## How to add a pass

Add a section under this one, above the earlier passes, so that the newest comes first: the date, the version, the commit tested, who ran it and how the result was given, and the environment: the NVDA version, the synthesizer and the Windows build (so that a regression is dated to an NVDA release rather than to some time). Under it, one table per app with a line per section of its plan: the steps run and the result, Passed, Failed (with what was heard) or Not run. A step that fails gets a line of its own with what NVDA said and what it should have said, and the bug's fix names the step. When a plan gains or loses steps, the next pass lists them as the plan has them then.

## Owed: the branch `palette` (2026-10-23, unreleased)

Not yet checked with NVDA, at commit f733b6d of `palette`; this section becomes the pass's section when it is run.
The list is `docs/axit/review-2026-10-23.md` section 9, in short:

- Every window: the focus after a notice (one rule of the window base now), F1 and Escape in a notice, the heading
  letters and Ctrl+F in a notice's text, the command palette (Ctrl+Shift+P: AxDown plan section 6 step 6, AxFile plan
  section 5 step 6).
- AxClaude: plan section 12 steps 18 to 21 and step 4a of the questions section; the console window shared with
  AxTerminal (reading while output arrives, sending, find, export, the sounds); the help section's steps 5 and 8.
- AxTerminal: the continuation rows, New session's fresh screen, the unrecorded command's status, the stop at once.
- AxFile: steps 2a, 10a, 12b, 13a, 6, 3a, 6a, 7 and 9.
- AxGit: steps 4a and 5. AxMedia: steps 1a and 2a. AxDown: step 6. AxPreview: step 9a.
- AxStart: its whole plan, owed since 3.4.0, with step 5a and section 2 steps 2, 3 and 5.

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

