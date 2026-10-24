# Axit over MCP: every feature to a client the user allowed

Asked by the user on 2026-10-24: "programmatic access to all the features with very well defined MCP APIs but with
absolute protection". Specification first (the user's order: spec, infrastructure, build); the first stage, read and
open, is built (section 7, 2026-10-24); change and act tools are not.
It depends on two things before it: the security fixes of `security-audit-2026-10-24.md` (S-11 to S-13 above all),
and the command lists of `app-kit.md`, which are what the tools are made from. Requirements are numbered MC-n.m.

## 1. Goals and non-goals

- G1. An AI client (Claude Code first, any MCP client) can do in Axit what the user can: open an app on a path, list
  and read what a window shows, run any command a window's menus have, and hear back what the window said.
- G2. Only a client the user paired, only the commands the user allowed for it, every change heard as it happens,
  every call in an audit log the user can read in a window.
- G3. One definition: a tool is a window's command (`app-kit.md`), so a new app's commands are tools without a line of
  MCP code, and a command the user cannot see in a menu cannot be called.
- Non-goal: raw keys, typed characters or screen reading as tools (the test channel's `keys` and `type`). They make
  every safeguard below moot.
- Non-goal: any network listener. Not TCP, not HTTP, not on localhost: a web page can reach localhost.

## 2. Shape

- MC-1.1 **The broker**: `Axit.exe mcp`, started by the MCP client as its child, speaking MCP over standard input and
  output (JSON-RPC 2.0, the stdio transport). It is not a window and holds no state of the apps; it is configured in
  the client as `{"command": "<installed Axit.exe>", "args": ["mcp"]}`.
- MC-1.2 **The windows' side**: every Axit window serves one named pipe, `\\.\pipe\Axit-<user SID>-<random>`, made
  with an access list for the current user's SID alone, remote clients refused, written with its random name into a
  per-user file only the user can read. The pipe answers only a client whose process is the installed `Axit.exe`
  (`GetNamedPipeClientProcessId`, its image path), that is, the broker. The same pipe replaces the hand-over message
  of a second start (S-13).
- MC-1.3 The test channel (`automation.md`) stays a separate, test-only tool behind its own secret (S-11); MCP is never
  built on it.

## 3. Pairing and authentication

- MC-2.1 **Off by default.** Options → General → "Let AI tools read and open Axit (MCP)" in every window, one setting for the
  bundle. Off: the broker answers every call with "Axit's MCP access is off" and nothing else.
- MC-2.2 **Pairing**: the first call of a client that is not paired shows a notice in the Axit window in front (or
  opens axStart's): "Claude Code asks to use Axit. It can: <the scopes asked>. Allow?", with Allow and Deny; Enter on
  Allow pairs, nothing else does. The client is recorded by its process's program file and its command line. The
  pairing key (256 bits) is kept for the user with DPAPI in `%LOCALAPPDATA%\Axit\mcp.key`; each connection proves it by
  a challenge and HMAC, never by the key itself.
- MC-2.3 **Forget**: Axit menu → "MCP clients…" lists the paired clients with what each may do; Delete forgets one.
  "Stop all AI access" in the Axit menu turns the setting off at once and ends every connection.

## 4. Tools and what each may do

- MC-3.1 **Scopes**, granted per client and per app at pairing and changed in "MCP clients…":
  - **read**: list the apps and their open windows; a window's title, status and focused item; a list's items; a
    text's lines (an editor's, a viewer's, a transcript's); what the window said lately.
  - **open**: start an app on a path or none, bring a window to the front without taking the keyboard from the user.
  - **change**: a command that changes the app's own state or a file the user opened in it (save, rename, stage, add to
    the queue, a field of an axTask item).
  - **act**: a command that acts outside the window: delete, move, run in axTerminal, send a message to Claude in
    axClaude, push, start a program, install, update.
- MC-3.2 **Tools are commands.** Every command of a window's command list (`app-kit.md`) is one tool, named
  `<app>.<command>` from its menu name ("axfile.delete", "axgit.commit"), with its scope from the command's own mark,
  its arguments typed (a path, a name, a text), its description from its help text. `tools/list` lists only the tools
  the client's scopes allow. Arguments are checked as the menu checks them: the same refusal says the same words.
- MC-3.3 **Confirmation**: an **act** tool is never run on the client's word alone. The window shows a notice naming
  the client, the command and its arguments ("Claude Code asks to delete notes.md in C:\work. Allow?"); only the user's
  own Enter on Allow runs it, a key the client cannot press (no key tool exists). "Allow for this session" is offered
  per tool, never for delete, install or update.
- MC-3.4 **Paths**: a path argument must be on this computer (`LocalPaths`), never a share, and inside the folders the
  user granted the client ("MCP clients…", folders); a path outside asks as an act tool does.
- MC-3.5 **Limits**: at most 10 calls a second per client; a text read is at most 1 MB per call.

## 5. What is never exposed

- MC-4.1 axPass: no tool reads or copies a password, the master password, a key file, notes, a generated password or a
  field marked secret. With the read scope a client may know whether a database is open and locked, and the titles of
  entries in the group shown, only after a pairing that names axPass explicitly. No tool unlocks.
- MC-4.2 axClip's history contents; the clipboard itself.
- MC-4.3 Any field the window marks secret (`IsSecret`, S-12), in any app.
- MC-4.4 The settings files, the logs, the pairing key, the audit log's writing.
- MC-4.5 A test fails when a tool of axPass exists beyond MC-4.1, or when a command marked secret is listed.

## 6. Heard and recorded

- MC-5.1 Every change or act tool run is announced in the window it acts on, as a user's command is, prefixed with the
  client's name: "Claude Code: deleted notes.md". Reads are silent, counted in the status bar ("AI read 3 times").
- MC-5.2 The audit log, `%LOCALAPPDATA%\Axit\mcp-audit.log`: the time, the client, the tool, a hash of the arguments
  and their kinds, the result, whether the user confirmed. Axit menu → "MCP clients…" → "Read the log" opens it in
  axPreview.

## 7. The first stage as built (2026-10-24, the branch `app-kit`): read and open

Step 3 of section 8, with the choices it needed; what is not here waits for the later steps.

- MC-6.1 **The setting**: `%APPDATA%\Axit\mcp.json`, `{"enabled": false, "clients": []}`, one for the bundle; Options →
  General → "Let AI tools read and open Axit (MCP)" in every window turns it on or off. Turning it on makes the key
  (MC-6.3) and shows a notice with what to add to the client: the command, this `Axit.exe`, and the argument `mcp`.
  Options → General → "Forget AI tools" forgets every paired client at once. Off, every window refuses every
  connection before anything else, so that turning it off in one window ends every client's access at its next call.
  The file carries a seal, HMAC-SHA256 of the setting and the clients under the key (MC-6.3); a file whose seal does
  not check, written by hand or by an AI tool's own file writing, reads as off with no clients, so that turning the
  access on or pairing takes code that runs as the user, not a written file (the review of 2026-10-24).
- MC-6.2 **The windows' pipes**: every window serves its own pipe, `\\.\pipe\Axit-mcp-<user SID>-<random>`, for the
  current user alone and refused over the network (the access list of S-11). The broker finds them by listing the
  pipes with that prefix, so no file names them. A window of axPass or axClip serves none (MC-4.1, MC-4.2): those apps
  are not reachable at all in this stage. The test channel stays apart (MC-1.3).
- MC-6.3 **Both ends prove who they are**: the window answers only a client whose process runs this very program file
  (`GetNamedPipeClientProcessId`, its image path, as the hand-over checks, S-13), and the broker talks only to a pipe
  whose server process runs it (`GetNamedPipeServerProcessId`). Then the window sends a fresh random challenge and the
  broker answers it with HMAC-SHA256 under the key, `%LOCALAPPDATA%\Axit\mcp.key`: 32 random bytes kept with DPAPI for
  this user (`UserSecret`); then the broker sends a challenge of its own and the window answers it, so that a pipe of
  another user's copy of the same `Axit.exe` cannot pass for this user's window. The key itself never crosses the
  pipe; the broker connects at the Identification level, so a window can never act as the user it serves. A line
  longer than 64 KB is not read; a request waits at most ten seconds for the window's thread; whatever fails is the
  call's answer and never ends the window's process or the broker.
- MC-6.4 **Pairing**: a client is known by the program file of the broker's parent process (the client that started
  it), with the name it gives in `initialize` for display only. The first tool call of a client not paired asks the
  newest Axit window: a notice "<name> asks to use Axit (<program>). It can read what the Axit windows show and open
  apps; it cannot change or delete anything. Allow?", with Allow and Deny, the window flashing on the taskbar when it
  is not in front. The broker waits up to two minutes for the answer; Deny, or no answer, refuses the call. With no
  Axit window open, the call is refused with "Open any Axit window to allow <name>"; a window that shows a notice
  already shows no pairing (a notice replaces the one shown, which the user may be answering) and the call is refused
  with "ask again once it is answered". An unanswered notice is closed when the call stops waiting, and its Allow then
  pairs nothing; after a Deny the same broker asks no more for a minute. A program that runs whatever script it is
  given (cmd, PowerShell, Windows Script Host, Python, mshta, rundll32, Explorer: `McpAccess.RefusedHosts`) is never
  paired. The name is the client's own word, its hidden characters made visible (`DisplayText`). What a window
  announces is kept for `axit_window_said` only from the first call that proved the key, never from before.
  The limits, said plainly: a pairing trusts every process of that program file (every script `node` runs, when the
  client is a `node` program), and a program of the same user can start `Axit.exe mcp` itself, even with a forged
  parent process that names a paired client. Against code already running as the user there is no absolute
  protection here, nor anywhere on Windows: such code can read every window through UI Automation without Axit at all.
  This is why this stage reads and opens only, and why change and act tools will ask at every call.
- MC-6.5 **The tools** (all `read` but the last): `axit_apps` (the apps and what each opens), `axit_windows` (the
  open windows that serve a pipe: an id, the app, the title), and for one window by its id `axit_window_state` (title,
  status bar, the notice shown and its text, the focused control and its value, a secret field as "protected"),
  `axit_window_list` (the focused list's items, or the window's first list), `axit_window_text` (the focused text's
  lines, or the window's main text, at most 1 MB), `axit_window_commands` (the window's command list: label, key,
  mark; running one is the next stage) and `axit_window_said` (what the window announced since the last call, the
  last 50). `axit_open` (`open`): an app by its verb, on a path or none, but axClaude and axTerminal, which start a program as they open (an act, MC-3.1); the path fully qualified, on this computer
  (`LocalPaths`), through no link (a junction or a symbolic link, which may lead to a share; asked part by part
  before anything follows it), existing, and of a kind the app opens (`Dispatch.PathFor`); started as
  `Axit.exe <verb> <path>` with the arguments as a list, through ShellExecute on the program file itself, never through
  a command interpreter (ShellExecute hands the new window none of the broker's handles).
- MC-6.6 **Limits and the log**: at most 10 calls a second; every call in `%LOCALAPPDATA%\Axit\mcp-audit.log`, one
  line: the time, the client's name and program, the tool, the first 16 hex digits of the SHA-256 of its arguments,
  and the result (done, refused and why, failed). The arguments themselves are not written.
- MC-6.7 **Not in this stage**: change and act tools and their confirmation (MC-3.3), the "MCP clients…" list with
  scopes per app and folders (MC-2.3, MC-3.4), the reads counted in the status bar (MC-5.1), axPass's titles (MC-4.1).

## 8. Built in this order

1. The security fixes S-11 to S-13 (the channel's secret, `IsSecret`, the hand-over pipe).
2. The command lists of `app-kit.md` in the first apps, with their scope marks.
3. The windows' pipe and the broker with read and open tools only; pairing; the audit log.
4. Change tools, then act tools with their confirmation notice.
5. An NVDA pass of pairing, confirmation and the announcements; then on by the user's choice.
