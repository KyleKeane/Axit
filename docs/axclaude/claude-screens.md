# Claude's screens and the app's dialogs

What each slash command of Claude Code shows in screen reader mode, and where the app lets you answer it (SPEC.md D33). The code form of this list is `SlashCommands.All` in `src/AxClaude.Core/Transcript/SlashCommands.cs`; keep the two in step.

Recorded: 33 commands were opened bare with Claude Code 2.1.281 in `C:\Temp\axclaude-fixture`, each in its own Claude process, and cancelled with Escape (2026-09-24). The others come from https://code.claude.com/docs/en/commands only and were not opened, mostly because typing them alone already acts (`/clear`, `/logout`, `/compact`, `/init`) or opens another program. Skills and plugins also appear as slash commands; they are not listed here.

## Where each one is answered

The app does not need to know the command: the shape of Claude's screen decides. The command only decides whether a list goes to the control area or stays in the message field. The control area is the message field's place at the bottom of the window, which holds one list or one field while Claude waits (SPEC.md D33).

1. **None.** The command prints text into the conversation and Claude is back at the `$` prompt, or the session changes or ends, or another program opens. Nothing to answer.
2. **A list of answers.** The cursor sits on `Select with numbers [1-N]. …` or `Enter y/n:` under numbered or `y.`/`n.` answers. The control area lists the answers, named with the question.
3. **A screen's lines and keys.** The cursor sits on a key-hint row (`↑/↓ to select · Enter to view · Esc to close`) or on a tab row (`Settings  Status   Config   Usage   Stats`). The control area lists the screen's lines, then one item per key the hint row names.
4. **Typed input.** A command that wants text after it goes in the message field, guarded so that a message is not sent into a question by mistake (SPEC.md FR-2.10). An `Enter text for` row (after "Other") gets a field in the control area; the message field also sends your words there without the guard, one keystroke at a time.

## One gap in the app today

- **Tab screens are hidden from the conversation** (still so; the control area lists their lines). `/status`, `/usage`, `/cost`, `/help` and `/permissions` put the cursor on the tab row at the top, and every row below the cursor is treated as chrome (FR-3.5), so their content never reaches the conversation.

## Lists of answers

- `/model`: "Select model", five models with the current one marked. Hints: `←/→` adjusts effort on the same screen, `s` uses the model for this session only, Enter sets it as the default.
- `/effort`: "Effort", six levels, current marked, no text rows.
- `/advisor`: "Advisor (experimental)", three models and "No advisor", current marked; a long explanation above and a recommendation below.
- `/theme`: "Theme", eight themes, current marked. Answer 8 ("New custom theme…") leads further. `Ctrl+T` turns syntax highlighting off.
- `/export`: "Export conversation", copy to clipboard or save to a file. Saving probably asks for a file name next (not recorded).
- `/memory`: "Memory", user instructions, project instructions, open the auto-memory folder. The answers open an editor or a folder.
- `/resume`: "Resume session", the recent conversations of the folder with a search row above. Typing searches; `Ctrl+A` shows all projects, `Ctrl+B` only the current branch.
- `/chrome`: "Claude in Chrome", four actions; the last is a toggle ("Enabled by default: No"). The prompt row says only "Then Enter to submit".
- `/mcp`: "Manage MCP servers", one answer per server with its state. An answer leads to that server's screen, another list (recorded with 2.1.282).
- `/hooks`: "Hooks", 33 hook events. An answer leads to that event's screen, which waits on `Esc to go back`: a screen (recorded with 2.1.282). Two-digit answers are typed quickly, as the list's own type-ahead.
- `/config` (also `/settings`): 44 numbered settings under the tab row (`Settings  Status   Config   Usage   Stats`) and the prompt row "Enter a number to change [1-44], or Escape to save and close:", printed after the frame's end. A number flips a true-or-false setting, or opens a numbered list of the setting's values ("Project instructions"), whose Escape returns to the settings; either way the list comes back. Escape on the settings saves and closes (recorded with 2.1.282). The app lists it as "Claude's settings": Enter on a true-or-false setting shows True and False first and sends the number only for a change; Enter on any other setting opens Claude's list of values.
- `/feedback`: "Feedback drafts", first "Loading…" over `Enter to write new feedback · Esc to close` for a moment (a spinner row, so not a screen), then the drafts Claude Code has queued on this machine, newest first: this session's under a "This session" heading, the others under "Other sessions" ("1. <title>, idea · 1m · <folder> · transcript available"), and "+ Write new feedback" last, with the prompt row "Select with numbers [1-N]. Then Enter to submit:" (recorded with 2.1.302). The headings are left out of the list of answers. A draft leads to its review screen: Type, Title, Area, Failure mode, Task, Details, "Send transcript: yes · sends this conversation to Anthropic", the environment line, "❯  Send feedback", waiting on `↑/↓ to move · Enter to send · d to discard · Esc to later`, which the area lists as Move (Up), Move (Down), Send (Enter), Discard (d) and Later (Escape). The ❯ marks Claude's highlight, which Up and Down move through the rows, and the hint row changes with it: on Send transcript, Task and Failure mode (`◀ yes ▶`) it is `↑/↓ to move · ←/→ to change · Enter to next · d to discard · Esc to later`; on Details, Area and Title the row becomes a field, with the cursor at the end of its text and the hint `↑/↓ to move · Enter to next · ctrl+j to new line · Esc to cancel edit` under the rest of the review, typed keys going into the field (the reader finds that hint row under the cursor, so the screen still waits, and the area shows a field named with the row: Enter sends the typed text to the end of the row and moves on, Escape cancels the edit). Enter on Send feedback: `Sending feedback…`, then `done: Feedback sent (receipt <id>). Thanks!` over `Any key to continue`, a screen with Continue (Enter), then the list without the draft. Discard and Later both bring the list back, Discard without the draft; Escape on the list closes it. (Recorded 2026-10-18, not kept as fixtures: the lists named other sessions' folders.) "Write new feedback" leads to typed text. With no draft queued the panel shows only `Enter to write new feedback · Esc to close`, a screen.

Extra keys are not offered. `/model` names `s` (this session only) and `←/→` (effort), `/resume` Ctrl+A and Ctrl+B, `/theme` Ctrl+T. They act on the highlighted row, and a typed number answers at once without moving the highlight: `s` tried in `/model` with 2.1.282 applied to the model already highlighted. The app would first have to move Claude's highlight with the arrow keys, more machinery than these keys are worth; the message field still reaches them.

## Lists of answers: questions from Claude's tools

Recorded with Claude Code 2.1.281 (2026-09-24); every answer is typed one keystroke per character, then Enter.

- Workspace trust at startup: "Permission Required: Accessing workspace", y or n.
- A tool's permission question (`--permission-mode default`): "Permission Required: Create file" or "Bash command", the command or file and a preview, "Do you want to proceed?", then Yes, Yes and always allow / switch mode, and No as the last answer. A `Tip:` row inside it is skipped. "Tab to amend" draws nothing in screen reader mode and is not offered. No returns to the prompt with "What should Claude do instead?".
- Plan approval (`--permission-mode plan`): "Permission Required: Ready to code?", Claude's plan as text, then Yes with auto mode, Yes with manual approval, and "No, keep planning", which asks for feedback on an "Enter text for option 3" row: a field in the control area, as after Other.
- Claude's own question (AskUserQuestion): a header row (" ☐ Colour"), the question, the answers, then Other and "Chat about this". Other asks for your words on an "Enter text for option 4 (Other)" row: the control area holds a field named with the question (Enter sends, Escape goes back to the list). "Chat about this" ends the question like Escape. Seen again with 2.1.307 in an off-screen replay of the window (2026-10-21, `src/AxClaude/CLAUDE.md`), the same shape; no fixture of that version is kept, so a change in the reader is checked against the 2.1.281 fixtures until one is recorded (the `record-fixture` skill).
- Several questions in one call: a tab row ("←   ☐ Colour   ☐ Fruit   ✔ Submit   →") above each, one list per question ("Question 1 of 2"), then "Review your answers" with y or n.
- Several answers allowed ("comma- or space-separated for several"): the list has check boxes; the ticked keys go joined with commas.
- An answer longer than the console width ("1. Label — description") wraps onto the rows under it. The reader joins those rows to the answer when the numbers show they belong to it: the answer above is numbered just before the one below, or the last answer is the one the prompt row names ("[1-5]"). Recorded in `wrapped-answers`.

## Screens to read

- `/status`, `/usage`, `/cost`: one tabbed screen, tabs Settings, Status, Config, Usage, Stats; `/cost` and `/usage` open on Usage. Cursor on the tab row; `Esc to cancel` at the bottom; the Usage tab also takes `d` (day) and `w` (week).
- `/help`: tabs Help, General, Commands, Custom commands; shortcuts and pointers. Cursor on the tab row.
- `/diff`: "Uncommitted changes (git diff HEAD)". Hint: `↑/↓ to select · Enter to view · Esc to close`.
- `/tasks`: "Background", the running tasks. Hint: `↑/↓ to select · Enter to view · Esc to close`. A task's details (seen in the background fixture) take `← to go back · Esc/Enter/Space to close · x to stop`.
- `/goal`: "Goal", the current goal or "No goal set". Hint: `Esc to dismiss`.
- `/mobile`: where to get the phone app. Hint: `Esc to close`, with the cursor on the row under it ("Then run /rc to continue this session from your phone"), which the list shows as its last line (recorded with 2.1.282).
- `/rewind`: "Rewind", "Nothing to rewind to yet." in a new conversation. Hint: `Esc to cancel`. With checkpoints the docs describe a list, probably numbered (not recorded).
- `/ide`: "Select IDE", "No available IDEs detected." Hint: `Enter to confirm · Esc to cancel`. With an IDE running, probably a list.

## The feedback card

Claude Code drafts feedback for Anthropic by itself (its SendFeedback tool, the `feedbackDrafts` setting in `/config`) and prints a card above the prompt at the end of the turn, with the cursor back on the `$` row (recorded with 2.1.302 in `feedback-card`):

```
Product feedback drafted: <title>
Draft preview, 3 lines:
> - **What happened:** …
> - **What the user said:** …
> - **Repro:** …
End of preview. Review the full draft with /feedback.
1 to review · 2 to send · 0 to dismiss
auto mode on (shift+tab to cycle)  ·  1 feedback draft
$
```

In a repository the branch row `  ● main` can sit between the mode line and the prompt. The card's keys cannot be pressed from the app: a digit typed in the message field goes to Claude as text with Enter (a 2 went as a message), and at Claude's own prompt a digit lands in the prompt as text too, a 0 also asking `Turn off Claude-drafted feedback? 0 to turn off · Esc to keep` under it. Claude draws the card again after every turn while the draft waits, and `/exit` asks "You have 1 unsent feedback draft" with `Enter to review & send · Esc to discard and exit`, a screen. The way that works is `/feedback`, the list above, and the app goes through it itself for a draft made in a turn of this session: when the mode line's count is higher after a turn than before it and Claude is idle, the app sends `/feedback`, answers the list with the draft the card named (`SessionModel.DraftTitle`) and shows the review as a screen, like a question; the list that comes back after Send, Discard or Later is closed for the user (SPEC.md D33). Drafts already counted when Claude is ready, and a draft put off with Later, wait for `/feedback` typed by hand, which shows the list. The card's rows stay out of the conversation, and the count is not background work. With Options → Questions → Answer from a list off, nothing opens and the card stays in the conversation as printed, less its hint row.

## Panels

- `/permissions`: tabs Permissions, Recently denied, Allow, Ask, Deny, Auto mode, Workspace; a search row; a numbered list starting with "Add a new rule…". Hint: `←/→ to switch · ↓ to select · Esc to cancel`. Cursor on the tab row.
- `/autocompact`: "Auto-compact window", a value adjusted with `←/→`; `Enter to apply · Esc to cancel`.
- `/artifacts`: "Artifacts", "Loading artifacts…" at first; a list with actions after (not recorded).
- `/auto-mode-setup`, `/bug` (docs only): a draft to review and save; a consent screen before a report is sent.

## None: text in the conversation

- `/agents`: says the wizard was removed and where subagent files live.
- `/output-style` bare: the available styles and "Usage: /output-style <style>". With a style it switches.
- `/import`: "No other AI coding agents detected" here; with one installed the docs describe a picker.
- `/copy`: "No assistant message to copy" in a new conversation; with replies the docs describe a picker of code blocks, `w` to write a file.
- `/btw` bare: "Usage: /btw <your question>".
- `/context`: works for a moment, then prints the context use.
- `/list-agents`: this session's name and the other Claude sessions.
- `/tui`: the current renderer and "Usage: /tui <default|fullscreen>".
- `/color`, `/heapdump` (docs only): a random prompt colour; a heap snapshot written to a file.

## Typed input

- `/add-dir <path>`, `/cd <path>` (docs only): the path follows the command in the message.

## None: the session changes or ends

- `/clear`, `/compact`, `/exit`, `/logout`, `/plan`, `/branch`, `/fork`, `/background`, `/subtask`, `/teleport` (docs only). Several act at once and some start work that costs tokens; none was opened.

## None: another program opens

- `/login`, `/keybindings`, `/desktop`, `/insights`, `/install-github-app`, `/install-slack-app` (docs only).

## Not known yet

- `/init`, `/fast`, `/voice`, `/upgrade`, `/passes`, `/remote-control` (also `/rc`), `/autofix-pr` (docs only; the docs do not say what the bare command shows).
- `/rename`, `/claim-credit`, `/usage-credits`, `/powerup`: named in hints on recorded screens, not in the docs' list.
