# Axit over MCP: every feature to a client the user allowed

Asked by the user on 2026-10-24: "programmatic access to all the features with very well defined MCP APIs but with
absolute protection". Specification first (the user's order: spec, infrastructure, build); nothing of this is built.
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

- MC-2.1 **Off by default.** Options → General → "Let AI tools use Axit (MCP)" in every window, one setting for the
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

## 7. Built in this order

1. The security fixes S-11 to S-13 (the channel's secret, `IsSecret`, the hand-over pipe).
2. The command lists of `app-kit.md` in the first apps, with their scope marks.
3. The windows' pipe and the broker with read and open tools only; pairing; the audit log.
4. Change tools, then act tools with their confirmation notice.
5. An NVDA pass of pairing, confirmation and the announcements; then on by the user's choice.
