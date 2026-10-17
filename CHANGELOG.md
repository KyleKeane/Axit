# Changelog

## 2.7.1 - 2026-10-17

- AxClaude: old turns read out again at the end of a long reply, the "déjà vu" of long sessions since September, is fixed. Claude Code sometimes repaints the whole conversation from the top of the screen; the app hides that burst, but only when the top row of the screen held text, and a repaint that found a blank line there went through as new lines, which Speak replies read. Found in a raw recording of 2026-10-17 with three repaints, one of them leaking. The app now takes any cursor-home without an erase onto a screen that shows text as a repaint.
- For developers: a consistency audit of AxClaude. The repository's brace rule is enforced by every build; dead code is gone (an unused question field, two unreachable command kinds, two single-implementation interfaces); each copied idiom in the window and the Core has one name; the page and paragraph keys and the UI Automation notification are shared pieces of Axit.Forms; the command-line parser lives in AxClaude.Core with tests; the specification, the guide, the test plan and the notes were brought back in line with the code and shortened (history out of the rules, one name per thing, the long decisions split).
- AxClaude: the status row of a running subagent ("◯ general-purpose Checking callers 2m 15s · ↓ 1.2k tokens", one per agent while Claude Code's Agent tool runs) is screen decoration now, like the spinner. It was taken for reply text and, redrawn every second, was spoken again and again by Speak replies for as long as the agent ran.

## 2.7.0 - 2026-10-17

- AxClaude: Ctrl+3 goes to the newest reply from anywhere, next to Ctrl+1 for the message field and Ctrl+2 for the conversation. It is the key of Navigate, Latest response, which was Ctrl+Shift+O. For the last line of any other kind: Ctrl+End, then Shift with its letter.
- AxClaude: Ctrl+Alt with a quick key letter, from anywhere, reads every line of that kind from the caret's line to the end of the conversation in one go, without moving the caret: Ctrl+3 to the newest reply, then Ctrl+Alt+P for its prose, each paragraph whole, Ctrl+Alt+H for its headings, Ctrl+Alt+C for Claude's lines, and so on for every quick key letter. Control stops NVDA; "No next paragraph" when there is nothing of the kind below.
- AxClaude: a tool line, a tool's output or a thinking line is never a paragraph for p and Shift+P, blank line in front or not; t finds the tool lines.

## 2.6.1 - 2026-10-16

- AxClaude: an "error:" row from Claude, such as "error: API Error: 401 Invalid API key · Please run /login" when the login has lapsed, is now spoken with Speak replies: first lines or all lines, like a "claude:" row. Such a turn announced "Sent", "Responding" and "Done" and never said why; only the e key found the row.

## 2.6.0 - 2026-10-14

- Axit: Help, "Check for updates" stays in the menu and asks GitHub again every time, so a user who chose Later on one version can look again; "Update to AxClaude n" (Axit n in AxDown) is a second item below it while a newer release is known. Before, the check item itself turned into the update item. The items "Installed: n" and "Latest release: n" are gone from Help: the version in use is on the Check for updates item ("Check for updates (AxClaude 2.6.0)..."), and About names it with the latest release and the releases page.
- AxClaude: every Options item is in a submenu now. Text takes "Time stamps on Input and Output lines", and a new General submenu holds "Check for updates at start", "Keep recordings and saved conversations out of git" and "Open settings file".

## 2.5.0 - 2026-10-14

- Axit: both windows are now light or dark as Windows is (Settings, Personalisation, Colours, "Choose your mode"), picked up when the app starts. They were always light. High contrast themes apply as before.
- AxClaude: the menus are regrouped and shortened. Session is now Claude: Send message, Send now, Interrupt, Next permission mode, and a Send key submenu for Ctrl+C, Ctrl+D, Tab, Up and Down. Navigate says Message field and Conversation. Options holds five submenus, Sounds, Speech, When Claude is done, Questions and Text, with the four settings that fit no group after them; the Speak replies choices are the Speech items "Speak replies: none", "first lines" and "all lines". Help gathers the Claude Code pages under "Claude Code on the web" and takes Record raw stream from Options. Every mnemonic is now unique within its menu: User guide and Check for updates shared the letter U, and Open current folder and Force Claude to restart shared F, so Alt and the letter cycled instead of acting.
- AxDown: Help, User guide now has the mnemonic G; it shared U with Check for updates, so Alt+U cycled between the two.

## 2.4.1 - 2026-10-13

- Axit: a notice opened by another notice's button now stays in view. "Update and restart" hid its own progress notice, so the download and installation ran behind a blank window with no Cancel to reach; an error after "Restart now", and AxDown's "The file changed on disk" question after Save in the "Save changes?" notice, were hidden the same way and left the window without a working key.
- AxDown: a file that could not be opened (locked, too large, not text) now leaves the window on an empty, untitled document, so Ctrl+S asks for a name instead of writing the empty editor over the file. A file that is not text, such as a picture or UTF-16 text without its mark, is refused as well: the edit control kept only the text before the first NUL character and would have saved that truncation back.
- AxDown: on a Windows set to "Use Unicode UTF-8 for worldwide language support", a file that is not UTF-8 is now read as Windows-1252 rather than as UTF-8 with replacement characters, which a save would have written back over every accented letter.
- Axit: a settings file with a typo in it (after Options, Open settings file) is now copied to `axclaude.json.bad` or `axdown.json.bad` before the defaults take over, and the message in the window names the copy. The app saves its settings soon after it starts, which wrote the defaults over every other setting in the file; the copy keeps them for mending.
- AxClaude: Shift+Escape now interrupts Claude from its question list and answer field too. The list and the field kept every Escape, with or without Shift, for themselves, so right after an answer nothing on the keyboard could interrupt.
- AxClaude: the message field no longer cuts a long paste at 32 767 characters. The edit control's default limit dropped the rest of a pasted log without a word, and the shortened message went to Claude as "Sent".
- AxClaude: Enter on a conversation line while one of Claude's questions occupies the bottom of the window now says "Answer Claude first; the message field is hidden while it asks" and copies nothing. It used to copy the line into the hidden field and put the focus in the answer list, where the comment typed next picked an answer and Enter sent it.
- AxClaude: in a git worktree (`git worktree add`, or Claude Code's worktree isolation) the patterns that keep AxClaude's recordings and saved conversations out of git now go into the repository's own exclude file. They were written into the worktree's private folder under `.git/worktrees`, which git never reads, while the log reported success.
- For developers: the privacy check and the recording dump now also catch an account path spelt the Git Bash or WSL way (`/c/Users/<name>`, `/mnt/c/Users/<name>`), which a Bash command in a conversation prints.
- Axit: three loose ends of the update: Cancel pressed while the downloaded zip is being unpacked now cancels (it was ignored and the update installed anyway); a download that is not a zip is reported as a failed update instead of a crash notice; and when the installed program cannot be started as the window closes, the failure goes to the log and the window still closes, where it used to stay open and throw again on every close. AxDown: Cancel in the "Save changes?" question after "Update and restart" now says "Update installed. It starts with the next AxDown" and forgets the restart, as AxClaude's Keep working does; a later Exit started the new version unasked.
- AxClaude: a question that Claude redraws while it is already shown at the bottom of the window is no longer shown again, with its chime and announcement, when a redraw leaves Claude's cursor off the prompt row for a frame. This fixes the question that "kept refiring" on 2026-10-02.
- AxClaude: a message sent while Claude works on a step longer than 30 minutes (a long subagent, a slow build) is no longer forgotten before Claude takes it up; its Input and Output lines and the "Responding" announcement were missing for such a message. A message whose Input and Output lines were trimmed from a very long conversation is no longer printed again at the bottom. Force Claude to restart now prints the Input and Output lines of a message Claude had not taken up yet, at the end of the old run, instead of holding it for an echo that never comes.
- AxClaude: an argument for Claude Code that ends in a backslash and needs quotes, such as `--add-dir "C:\Dir With Space\"` with the backslash tab completion adds, is now passed on correctly. Windows read the backslash and the closing quote as a quote character, so the folder came out wrong and every argument after it was swallowed. The Custom arguments field splits by the same Windows rules.
- AxDown: Go to line says "Not a line number" when the text typed is not a number, where it silently did nothing.
- AxClaude: after Force Claude to restart or a new session, a question the new Claude asks at startup (workspace trust) goes to the answer list again. It was routed as if it belonged to the slash command sent last in the old session, so after `/status` or `/config` it was left to the message field.
- AxClaude: a question of Claude's with an empty title no longer crashes the window when its name is built.
- AxClaude: the first lines of a reply longer than the screen are no longer lost. Claude prints a long reply in one go where its spinner and mode line stood, and the lines written over those rows scrolled off the hidden screen before the end of the frame, so they kept the rows' "interface" marking and were never shown, spoken or saved.
- AxClaude: a reply row shaped like an entry of the slash-command completion list, such as an aligned listing of routes or paths (`/api/users      GET  lists users`), is no longer hidden. Only the rows directly above the typed command, or under Claude's cursor while it prints the list, are the list.
- For developers: a frame that changes nothing in the conversation (a spinner tick) allocates nothing again in the view's mirror, as the notes on the per-frame path promise; the edit list and the no-change result were allocated on every frame.
- AxClaude: Save conversation as (Ctrl+S) no longer writes the extra line break the conversation shows while a reply streams past the line you are reading; the file holds the lines as they are.
- AxClaude: what the window remembers about spoken reply lines and seen tool calls is dropped with the lines when a long conversation is trimmed, instead of growing for as long as the window is open.
- Axit: Page Up and Page Down keep moving a whole screen in a text with more than 65 536 rows (a very large file in AxDown, a long conversation with wrapped lines); the edit control reports the bottom row in 16 bits, and past that count the keys moved one row.
- AxClaude: the handlers for Claude's output and exit are now given to the pseudo console before its reader thread starts, so a chunk printed in the first moments can no longer be missed.
- Axit: the text of a notice (the user guide and keyboard shortcuts under Help, an error, the update's release notes) and the Custom arguments field of New session now have the bundle's Page Up, Page Down, Ctrl+Up and Ctrl+Down. Every notice ends with its key line, AxClaude's Find included ("Enter: Find next. Escape: Cancel."), where AxClaude left it off a notice with no words of its own.

## 2.4.0 - 2026-10-13

- AxClaude: Options, "Soft background music while Claude works" (off until you turn it on) plays a quiet, slow loop of soft plucked notes from the moment Claude starts working until it is done or asks, so you hear that Claude is still at work without reading along. It stops at once when you turn it off, and the chimes and ticks sound over it as before. Ctrl+Space turns it on or off from anywhere in the window and says so, adding "nothing is running" when Claude is idle.
- AxClaude: Options, "Louder alert when Claude is done and the window is in the background" (off until you turn it on) plays a longer, louder call in place of the done or question chime when Claude finishes or asks while you are in another window, where the chime was easy to miss and NVDA does not say "Done".
- AxClaude: `p` and Shift+P now stop at each "claude:" line as well as at the paragraphs after a blank line. Claude prints its short messages between tool calls with no blank line in front of them, so the paragraph key passed over them and found only the paragraphs of the final reply.
- Axit: Ctrl+Up and Ctrl+Down now move to the start of the previous and next paragraph in every text field of the bundle (AxClaude's conversation and message field, AxDown's editor), a paragraph being the first line after a blank line, and Ctrl+Shift+Up and Ctrl+Shift+Down select to there, as in Word. The Windows edit control scrolled the view instead and left the caret where it was, so a screen reader read the same text again.
- AxClaude: Send Up and Send Down to Claude are menu items only now (Session menu), since Ctrl+Up and Ctrl+Down in the message field move by paragraph like everywhere else.

## 2.3.1 - 2026-10-02

- For developers: the documents and notes are pruned of the transition's scaffolding (the plan is now a short record), the accessibility review covers both windows, and the specifications describe the released state only. `docs/axdown/tutorial.md` is a guided tour of AxDown with every Markdown structure to practise the keys on; `run-axdown.ps1` opens it when given no file. No change in behaviour.

## 2.3.0 - 2026-10-02

- Axit: an update now installs while the app is still open, so one progress bar covers the download and the installation, and the app starts the new version itself afterwards: "Update and restart" brings AxClaude back on its folder and AxDown on its file, or empty if it had no file (the restart used to depend on a file being open). "Update and close" installs and closes. The installer puts the running program file aside as `Axit.old.exe`, which the next start removes.

## 2.2.0 - 2026-10-02

- Axit: the update notice offers "Update and restart" (Enter), which installs and starts the app again where it was, and "Update and close", which only installs; "Open release page" and "Later" stay. While the download runs, a notice shows a progress bar, which NVDA reports the way it reports any progress bar, and Escape cancels the download and keeps the version you have.
- For developers: `run-axclaude.ps1` and `run-axdown.ps1` build Axit and start one app; both call `run.ps1 -App claude|down`, which on its own starts AxClaude.

## 2.1.1 - 2026-10-02

- AxDown: NVDA now speaks the window when it opens ("README.md - AxDown window") before the text field, as it does for AxClaude. The window had no accessible role, so NVDA passed over it.

## 2.1.0 - 2026-10-02

- AxDown: Ctrl+H and Ctrl+Shift+H move to the next and previous Markdown heading, as H and Shift+H do on a web page in NVDA's browse mode, and say it the same way ("Install, heading level 2"). Headings inside fenced code blocks are skipped; "No next heading" when there is none. Find, Go to line and the heading keys are in a new Navigate menu, as in AxClaude.
- For developers: every key of AxClaude lives in one table, `AxClaudeKeys`, and the bundle-wide keys in `BundleKeys`; a test fails when a window key collides with a bundle key. No key changed.

## 2.0.0 - 2026-10-02

- Axit: AxClaude is now one app of Axit, one program file that holds several accessible apps and updates them together. `Axit.exe claude <folder>`, or a folder alone, starts AxClaude exactly as in 1.7.0: nothing about how it reads, speaks or sends has changed; the console command `axclaude` stays.
- Axit: the repository is now github.com/KyleKeane/Axit. The old address redirects, so installed copies of AxClaude 1.7.0 still find this release under Help, Update, and it carries them over into Axit.
- Axit: settings and logs moved to the bundle's folders, `%APPDATA%\Axit\axclaude.json` and `%LOCALAPPDATA%\Axit\logs\axclaude.log`. The first start copies the settings of AxClaude 1.x; the old file is left where it was.
- AxDown: a new app, a plain editor for Markdown and text files. One file in one window: a text field named after the file, a menu bar and a status bar. Files are written back in the encoding and line endings they came with; New, Open, Save and Save as; Find (Ctrl+F, F3); Go to line (Ctrl+G), whose field also says where you are; word wrap, text size and font remembered; unsaved changes and a file changed on disk are asked about inside the window. `Axit.exe down <file>`, or a file alone. Nothing is spoken unless asked for or a command finishes.
- Axit: one installation for every app, in `%LOCALAPPDATA%\Programs\Axit`, listed in Settings, Apps as Axit. The Start menu entries are "Axit AxClaude" and "Axit AxDown" (press the Windows key and type `axc` or `axd`); the console commands are `axclaude`, `axdown` and `axit`, which opens the app that fits the path it is given; the right-click entry on folders stays "Open in AxClaude", files get "Open in AxDown", and AxDown appears under Open with for `.md`, `.markdown` and `.txt`. Installing removes an AxClaude 1.x installation first. The zip is `Axit-<version>-win-x64.zip`, with the bundle's README inside; each app's guide is under its Help menu.

## 1.7.0 - 2026-10-02

- AxClaude is becoming one app of **Axit**, a bundle that will also hold AxDown, a plain editor for Markdown and text files. The next release installs as Axit, with AxClaude inside it unchanged. This version is the step in between: its updater accepts a release whose program file is not named AxClaude.exe, so Help, Update now will carry an installed AxClaude over into Axit.

## 1.6.0 - 2026-09-25

- A question whose answers are too long for one line now lists every answer. Claude wraps a long answer onto the next line, and AxClaude used to stop reading answers there, so the first answers were missing from the list and read as part of the question.
- Claude's "Review your answers" no longer needs a y: AxClaude submits it for you after your last answer, unless a question was left without an answer. Options, "Review answers before they go to Claude", brings the review back.
- Enter on a line of one of Claude's screens (`/status`, `/tasks`, `/help`) closes it, as Escape does, and AxClaude says "Closed". The keys listed at the end still do what they name.
- Claude's working line ("Puzzling…") and its mode line ("auto mode on (shift+tab to cycle) · esc to interrupt") are no longer read out at the end of a reply. When the window was full, Claude left its cursor on the mode line, and AxClaude took both for part of the reply.
- Tab and Shift+Tab now simply move between the conversation and the message field, or Claude's question in its place. Claude's permission mode, which Shift+Tab used to change, is Ctrl+Shift+M from anywhere in the window.
- More keys in every list of Claude's answers, settings and screens: a number jumps forward through the answers starting with it (1, 10, 11), and now Shift with the number jumps back; 0 goes through the answers 1 to 9, Shift+0 back; Page Up and Page Down move ten answers; Home and End, and Ctrl+Home and Ctrl+End, go to the first and the last.
- Your files stay out of your project's git. Save conversation and Record raw stream now offer your Documents folder, with names starting `axclaude-`, and when the project is a git repository AxClaude lists those names in its local exclude file (`.git\info\exclude`, never committed), so git ignores them wherever you save them. Options, "Keep AxClaude's recordings and saved conversations out of git", on by default, turns this off.
- For developers: PtyCapture's `--dump` hides e-mail addresses, account paths, pipe names and session ids unless `--raw` is given, and a test fails when any file in the repository holds an e-mail address or an account path.

## 1.5.1 - 2026-09-25

- Fewer words: every announcement is shorter ("Ready", "Sent", "Responding", "Done", "Claude asks"), and the message field has no description read on every arrival.
- Claude's questions and screens now take the place of the message field instead of covering the window. The conversation stays in view with the whole question in it. The answers are a list named with the question, which NVDA reads each time you arrive; Ctrl+Tab or F6 moves between the conversation and the answers. Enter answers, Escape cancels, Space ticks when several answers are allowed. When a question comes, you are taken to it at once and whatever NVDA was reading stops; Ctrl+Tab goes back to the conversation. Options, "Go to Claude's questions at once, interrupting speech", turns that off: you then stay where you are and hear the question after what is being read. The message field comes back, with what you had typed, when Claude has your answer. Nothing can be sent into a question by mistake, since the field is not there.
- Enter sends the message, and Shift+Enter starts a new line (Ctrl+Enter sent before). The same in New session's Custom arguments.
- When Claude asks several questions at once, each says where you are ("Question 1 of 2: …"), and after your answer AxClaude says what comes next: the next question, the review, or your own answer after Other. A question that follows your answer comes without the trill.
- Choosing "Other" (or any answer that asks for your own words) puts a field in the list's place, named with the question and what you chose. Enter sends your words, Escape goes back to the list. This replaces the "Your own answer" field under the answers.
- /config lists Claude's settings with their values. Enter on one changes it: a true or false setting asks True or False first, any other setting opens Claude's list of its values. The list comes back on the setting you changed. Escape saves and closes.
- /mobile is read with all its lines.
- A background agent counts as background work: closing AxClaude and New session warn about it, and the status says so.
- Installing and updating remove the previous AxClaude completely first, also one installed with the earlier install.ps1, and then install fresh. The installer says what it found. Your settings stay.
- Removing the old installation waits a few seconds if Windows still holds the program file after AxClaude closes.

## 1.5.0 - 2026-09-24

- A question that offers "Other" has a field "Your own answer" in its notice (Alt+Y): type your answer there and press Enter, and AxClaude sends it when Claude asks for it. If Claude asks for your own words some other way (Other chosen from the list, or "No, keep planning" when you turn down a plan), AxClaude puts you in the message field and says so.
- A question that allows several answers opens the notice with check boxes: Space or an answer's number ticks it, and Enter sends all the ticked answers.
- `q` and Shift+Q in the conversation go to the next and previous question Claude asked. AxClaude writes a line `Question: …` in front of each question and screen, which stays when Claude replaces the question with your answer.
- Claude's questions with several parts come one at a time with the right title, and the review at the end opens the answer notice too. Permission questions (a Bash command, a file, plan approval) are titled by their "Permission Required:" line.
- Fixed: answers of more than one character did not reach Claude. Claude takes an answer as typed keys and ignores several characters sent at once, so "12" in a long list, "1,3" for several answers and your own words were dropped; every answer now goes one keystroke per character.
- Fixed: choosing "Other" left you with nowhere to type.
- A question that Claude redraws in passing no longer closes its notice or is announced again.
- Installing is a double-click: download `install.cmd` from the release page and open it, and it fetches and installs the latest AxClaude. The zip has it too. No PowerShell setting has to change.
- AxClaude is listed in Settings, Apps (and Add or remove programs), with its version and an Uninstall that removes everything the installer added and keeps your settings. While AxClaude is running, Uninstall removes nothing and says to close it first.
- Updates: an installer error is now written to the update log instead of vanishing, and older downloaded versions are removed.

## 1.4.0 - 2026-09-24

- Claude's questions open an answer notice: the workspace trust question, the lists of commands such as `/model`, `/effort`, `/theme` and `/resume`, and Claude's own multiple-choice questions. A quick high trill, "Claude asks:" with the title, and the focus on the current answer; the arrow keys or the answer's number choose, Enter answers, Escape cancels. Alt+M, "Answer in the message field", leaves the question open for an answer you type. The next question of the same screen opens its own notice, and a notice whose question went away closes. Claude's full question stays in the conversation.
- Screens that wait for a key, such as `/status`, `/usage`, `/help`, `/tasks` and `/goal`, open a screen notice with the screen's text (for `/status` and `/help` it never reached the conversation) and a button for every key the screen offers. They also count as waiting for your answer now.
- While Claude waits for an answer, Ctrl+Enter sends only an answer. A message you typed stays in the field and AxClaude says so; Ctrl+Enter again sends it anyway. A message typed into the trust question used to be lost. Options, "Answer Claude's questions in a notice", turns the notices and this protection off.
- Every notice ends with a line naming its keys ("Enter: Close anyway. Escape: Keep working."), and a notice you did not open yourself (Claude's questions, errors, Claude Code not found, the update offer) plays the notice trill and ignores Enter for a moment, so keys meant for the message field do not answer it.
- Work Claude runs in the background, such as a command started with `run_in_background`, is shown in the status bar ("1 shell in the background"), and closing AxClaude or starting a new session asks first while it runs.
- Restart Claude is now "Force Claude to restart" (Ctrl+Shift+R) and asks first while Claude runs, since it quits Claude Code at once and ends anything it runs in the background.
- Fixed: old content was read out again, and copies of earlier turns appeared in the conversation, when Claude repainted the whole conversation from the top of the screen in a long session. The repaint is now recognised and its copies are hidden; a reply line is also never spoken twice within one reply.
- Fixed: Claude's mode line with background work ("auto mode on · 1 shell · ↓ to manage") appeared in the conversation.
- `docs/axclaude/claude-screens.md` lists every slash command with what its screen shows and the notice it gets.

## 1.3.0 - 2026-09-20

- Change folder and Recent folders clear the conversation before Claude starts in the new folder, as New session does: the window holds only the "Project folder changed" line and what Claude prints there, with that folder's earlier exchanges under their "from previous session" headings when it has any. Restart Claude keeps the conversation.
- Fixed: the ticks stuttered on the speaker and went ragged once many had played. Every sound went through winmm's PlaySound, which opens and closes the audio device for each call and backs up under quick succession. The sounds now play through one wave-out device that stays open for the whole run: each sound is prepared once and queued with a single write, a chime replaces whatever plays, and a tick is dropped while the previous one is still on its way, so they never pile up. A tick that arrives during a chime now follows it instead of being lost.
- The tool tick is two lower knocks of 12 ms each instead of two 5 ms strikes, so it can be heard on a small speaker. The option that turns the ticks off is now called "Tick for each new line and tool call from Claude"; it always covered both.
- Restart Claude (Ctrl+Shift+R) says "Restarting Claude".
- `run.ps1 -New` starts a new conversation; a bare `--` never reached the app because PowerShell takes it for itself.
- `tools/PtyCapture` records through the app's own console host and child environment (`PtyHost`, `ClaudeLauncher`), so a recording shows exactly what the app sees, and the tool no longer carries a second copy of the ConPTY code.
- The specification, the guide, the test plan and the working notes were checked against the code and brought up to date; the test project shares its helpers in one place.

## 1.2.1 - 2026-09-19

- Fixed: Ctrl+O is no longer sent to Claude. In screen reader mode Claude's detailed view redraws the whole conversation from the first message on screen, which gave the conversation a second copy of every message and made Speak replies read old replies again. Ctrl+O now says why it does nothing and points at `--verbose` for tool output; the Session menu item is gone.

## 1.2.0 - 2026-09-19

- New session (Ctrl+N) clears the conversation before Claude starts again: the window holds only the "New session in" line and what the new session prints, with the earlier exchanges under their "from previous session" headings when the last conversation is continued. Messages and bookmarks count from 1 again. Restart Claude keeps the conversation as before.
- Fixed: the status bar said "ready" with the old permission mode while a restarted Claude was still coming up; it now says "starting" until the new session prints its mode line.
- Speak replies as they arrive is now a choice of three in the Options menu: None, First lines only (the first line of each of Claude's messages, where Claude says what it is doing or what it found) and All lines. Each message is spoken in one piece once it is complete, so a long answer no longer stops after its first lines; tool output, thinking and the conversation replayed at startup are never spoken. The setting is `replySpeech` in the settings file; the old `speakReplies: true` is read as All lines.
- Options → Speak each tool call speaks the `tool:` line of every tool Claude runs as soon as it appears, whichever reply choice is in use; tool output stays unspoken. The setting is `speakToolCalls`.
- The tick for a new line is a touch bolder, like a typewriter key, and a tool call has a tick of its own, two lower strikes like a tiny ratchet, so a tool starting is told from a line arriving. Both stay short and quiet.
- The Project menu item that names the current folder is now "Open current folder" (Ctrl+W from anywhere): it opens the folder in File Explorer. It used to copy the path.
- Page Up and Page Down in the message field move one screen of the field at a time and reach the first and last line of the message; the field's own keys did nothing while the message fit its three lines. The conversation's paging works the same way.
- Fixed: the tick for each new line could stop for the rest of the session when the audio device never finished an earlier sound; the tick now yields to a chime by the clock instead of asking Windows whether a sound is still playing.
- Fixed: a `you:` row that repeats no sent message (quoted in a tool result, replayed at startup, or printed for a slash command) no longer claims a waiting message's Input and Output block, which could land far up in the conversation.
- Fixed: the headings for replayed exchanges stopped at a quoted "[Screen Reader Mode:" row, leaving earlier exchanges without them.
- Fixed: rows that scrolled past within one burst of output were never classified, so `i`, `c` and `t` skipped them.
- Fixed: `h` and the heading keys no longer stop on short tool-output rows or on the last line of a code block; a heading is a short row between blank rows inside a reply.
- Fixed: a question is pending only while Claude's cursor sits on its prompt row, so a prompt quoted in a tool result no longer announces "Claude needs your answer" or turns the next message into an answer. The numbered menus of `/effort` and `/model` ("Select with numbers") now count as questions: the typed number is announced as "Answer sent" and gets no Input and Output block.

## 1.1.0 - 2026-09-19

- Ctrl+N opens New session: a notice with the folder and a Choose folder button, a list of ways to start Claude (new conversation, continue the last conversation, choose a conversation to resume, plan mode, edits accepted without asking) and Custom for Claude Code's own command line arguments, one or more on each line. Enter stops the current session and starts the new one; Restart Claude keeps the choice. Change folder stays in the Project menu, above New session, without a shortcut.
- Sounds: a high ping when Claude is ready, a low note when a message is sent, three rising notes when Claude starts replying, the same notes falling when the whole turn is done, two equal notes when Claude needs an answer, and a soft tick for every new line that arrives from Claude. Options, "Click for each new line from Claude" turns the tick off, and "Play sounds: ready, sent, replying, done, question" the chimes, which replace the Windows Asterisk sound.
- Claude continues the last conversation in the folder by default: AxClaude starts it with `--continue` unless the command line says otherwise (`axclaude --` alone starts a new conversation), and New session preselects "Continue the last conversation". A folder with no earlier conversation starts a new one after Claude's own "No conversation found" line. When AxClaude starts, the exchanges Claude replays get their own Input and Output lines, marked "from previous session"; new messages count from 1 as before. Restart Claude keeps the conversation as it is, without a replay under it.
- Backspace in the conversation goes back to the line you were on before the last jump, and again for the jump before that, so a stray `p` or `h` is undone in one press.
- Enter on a conversation line now ends the copied block with the line "_ start of comment on the copied line _" and moves you into the message field, with the caret under it, ready to type the comment.
- `p` and Shift+P jump to the next and previous paragraph, the first line after a blank line. The prompt jump on `p` and the blank-line jump on `b` are gone.
- Session, "Send the waiting message now" (Ctrl+Shift+S) sends Claude's Ctrl+X Ctrl+S, so a message sent while Claude works is taken up at once instead of after its current step. "No message waiting" when there is none.
- Up and Down in the message field no longer bring back earlier messages; they only move through the text you are typing. Earlier messages are in the conversation under their "# Input" lines, and Enter on a line copies it into the message field.

## 1.0.2 - 2026-09-19

- Ctrl+1 goes to the message field and Ctrl+2 to the conversation. When you are there already, the key says where you are.
- A disclaimer in Help, About and at the end of the user guide: no warranty, no liability, no affiliation with Anthropic.
- Enter on a conversation line puts the line's text on its own line in the message, under "Line 12 of 120:".
- The log's start line and Copy diagnostics name the program file and the process, so a report says which copy of AxClaude was running.
- Following a reply as Claude writes it no longer repeats what you just heard: once you have heard the last row, what arrives next is shown on a line of its own, and Down Arrow reads only that. The line reads as one line again once you move on.
- Selecting text in the conversation works while Claude writes: output waits until the selection is gone, Ctrl+C copies the selection and says "Copied", and Escape clears the selection before it goes to the message field.

## 1.0.1 - 2026-09-19

- The Help menu always shows the version you have, the latest release on GitHub and the update action.
- A newer release opens the update notice when AxClaude starts: Enter updates, Escape keeps the version you have. Options, "Check for updates when AxClaude starts" turns the check off.

## 1.0.0 - 2026-09-19

First release.

- Runs `claude --ax-screen-reader` in a Windows pseudo console and shows everything it prints as a line-by-line conversation that NVDA reads with the arrow keys.
- Every message is printed as a block: `# Input n`, the message, a blank line, `# Output n Reply from Claude`, a blank line. Both `#` lines are level 1 headings, Claude's own headings are level 2 and deeper, and Claude's echo of the message is hidden.
- Single-key navigation in the conversation: `i`, `o` or `r`, `h`, `1` to `6`, `c`, `t`, `p`, `e`, `d`, `s`, `b`, `l` for the line number, Page Up and Page Down from the caret, Ctrl+F with F3 and Shift+F3, Ctrl+Shift+O for the latest reply.
- Bookmarks: `m` drops a `Bookmark n` line in front of the current line and takes it away again, `k` and Shift+K walk through them and read the marked line, Ctrl+Shift+K and the Navigate menu do the same from anywhere. They are part of the conversation, so Ctrl+S saves them with it.
- A multi-line message field: Ctrl+Enter sends, Enter starts a new line, Up and Down at the edges recall earlier messages, Shift+Escape interrupts Claude (plain Escape does nothing and says so), Shift+Tab cycles the permission mode, Ctrl+Up and Ctrl+Down reach Claude's menus.
- Enter on a conversation line copies it into the message field under a marker line, so the next message can reply to it.
- Messages sent while Claude works stay out of the conversation until Claude takes them up, with "Message waiting" and the count in the status bar.
- Spoken notices through UI Automation: message sent, Claude is responding, Claude is done, Claude needs your answer, mode changes, Claude's detailed view; a sound and a taskbar flash for the last two.
- Output that arrives while you read never moves the caret, and paragraphs that Claude wrapped at the console width read as one line.
- Project folder from the command line, the Start menu, File Explorer's "Open in AxClaude" or the `axclaude` command; Change folder, Recent folders, Restart Claude.
- Save the conversation as text, the time in the Input and Output lines, "Speak replies as they arrive", font and text size, high contrast through the system colours.
- A "Claude Code was not found" notice with the install command, the install page and a file picker.
- The app's own dialogs (Find, the shortcuts, the user guide, About, errors, Claude Code was not found) are notices inside the window, never separate windows: Enter is the main button, Escape closes, and the rest of the window waits. Closing the app while Claude is still working asks first.
- Help menu with the keyboard shortcuts, the user guide and the Claude Code documentation pages; Copy diagnostics; a rolling log; a raw stream recorder for bug reports.
- Settings in `%APPDATA%\AxClaude\settings.json`; `--help` and `--version`.
- `install.ps1` installs per user without administrator rights and `install.ps1 -Uninstall` removes it; `publish.ps1` builds the self-contained executable and the zip.
- Updates: at startup AxClaude asks GitHub once whether a newer release exists (Options turns that off) and says so. Help, Check for updates asks on demand, and Update now downloads the release, installs it and starts AxClaude again on the same folder.
- Releases are built and published by GitHub Actions from a version tag (`release.ps1`).
- MIT licence.
