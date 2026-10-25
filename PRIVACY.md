# Privacy

Axit collects nothing. It has no telemetry, no analytics, no crash reporting, no accounts and no advertising. The
developer receives no data from it, and has no server it could send data to. Everything below is checked against the
code; a change to what Axit sends anywhere changes this page in the same commit.

## What Axit sends over the network on its own

One thing, and it can be turned off:

- **The update check.** When an Axit window starts, it asks GitHub for the latest release of this repository
  (`https://api.github.com/repos/KyleKeane/Axit/releases/latest`). The request carries the version you have
  (`User-Agent: Axit/<version>`) and, as every web request does, your network address. GitHub's privacy statement applies
  to it: <https://docs.github.com/site-policy/privacy-policies/github-general-privacy-statement>. Turn it off in any
  window: Options, General, Check for updates at start. You can still check by hand (Axit menu, Check for updates).

When you choose to update, Axit downloads the release's zip and its signature from GitHub's release pages, checks both,
and installs it for your Windows account only.

## What Axit does on the network only when you ask

- **Help pages.** Each app's Help menu has links to documentation (Microsoft, GitHub, CommonMark, JSON, git, KeePassXC,
  Claude Code). Choosing one opens it in your browser; Axit itself fetches nothing.
- **axClaude** runs Claude Code, Anthropic's program, which you install yourself. What you type in axClaude goes to
  Claude Code and, through it, to Anthropic under Anthropic's own terms and privacy policy
  (<https://www.anthropic.com/legal/privacy>). Axit does not read, keep or send your conversation anywhere else; it shows
  what Claude Code prints, and records it to a file only when you turn recording on (Options, Developer).
- **axGIT** runs git, your own installed program. It contacts a remote (GitHub or wherever your repository's remotes
  point) only when you fetch, pull or push.
- **axTerminal** runs your own shell (PowerShell, Command Prompt or Git Bash). What the commands you type do is up to
  them.
- **Install it now.** When an app needs a program that is missing (Git, KeePassXC, Claude Code, PowerShell 7), its
  notice offers to install it, and does so only when you choose to. Git, KeePassXC and PowerShell 7 come through
  Windows' own package manager, winget, from its community repository (`--source winget`), which downloads each maker's
  installer. Claude Code comes from Anthropic's own installer (`irm https://claude.ai/install.ps1 | iex`). Each program
  then has its maker's terms.
- **Links you open**: a link in axTask's fields, in a Markdown file or in a notice opens in your browser when you press
  Enter on it.
- **axFile** works on the folders you open, network shares among them when you open one. Its Open with, Properties and
  Windows menu hand the file to Windows and to the programs you choose.

## The AI tools (off unless you turn them on)

Axit menu, AI tools, Let AI tools read and open Axit. When it is on, an AI tool on your computer that you pair, such as
Claude Code, can read what Axit's windows show and open apps (`docs/axit/mcp.md`). This happens through named pipes
for your Windows account alone, never over the network; what the tool then does with what it read is under its own
terms (Claude Code: Anthropic's). axPass and axClip are never read. Every call is written to `mcp-audit.log` on your
computer, with its arguments hashed. Forget AI tools removes every pairing; turning it off ends access at the next
call.

## What stays on your computer

- **Settings**: `%APPDATA%\Axit`, one file per app.
- **Logs**: `%LOCALAPPDATA%\Axit\logs`. They hold starts, errors and what an update did; never what you type, copy,
  read or say to Claude. Each rolls over at 1 MB.
- **axClip** keeps its history in memory; on disk only when you choose Keep history, and then encrypted for your
  Windows account.
- **axPass** never stores your passwords: KeePassXC's own tool reads and writes your database, and the master password
  goes to it on a pipe. A copied password is marked private for Windows' clipboard history and cleared after 30
  seconds.
- **Copy diagnostics** (Options, Developer) and **Copy details** (a failed update's notice) put text on your clipboard
  only when you choose them, with your account's name and folders, your computer's name and any e-mail address
  replaced, for you to read before you share it.

## Programs Axit runs

Axit starts only Windows' own programs by their full paths (PowerShell for the installer, File Explorer, cmd for
batch files), the programs you installed and chose (Claude Code, git, your shells, KeePassXC's tool, the programs of
Open with), and itself. It never runs anything with administrator rights; an installer you choose in Install it now
may ask Windows for them, and Windows asks you.

## Children, changes, contact

Axit is not directed at children and collects nothing from anyone. This page changes when Axit's behaviour changes; the
history is in this repository. Questions: open an issue
(<https://github.com/KyleKeane/Axit/issues>), or for a security matter a private advisory, as `SECURITY.md` says.
