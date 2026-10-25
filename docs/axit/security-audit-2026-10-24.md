# Security and privacy audit, 2026-10-24

Asked by the user: "a complete audit of the code for security and vulnerabilities … to ensure this is of the utmost
certifiably secure privacy protecting quality and can stand up against aggressive bad actors", with programmatic access
to every feature through well-defined MCP APIs "but with absolute protection". This document is the audit: how it was
taken, every finding with its number, its severity and its state, and the two decisions the user took. Each fix
names its finding (S-n) in its commit. The MCP surface is specified in `docs/axit/mcp.md` and is built on the fixes
of section 3 and 4 below, never on the test channel as it was.

## 1. How it was taken

Six read-only reviews on 2026-10-24, each over one surface, each asked for the concrete attack (who controls what
input, what they gain) and the concrete fix, and to list what is sound: processes and paths; inter-process messages
and the automation channel; secrets and personal data; the update, the install and the supply chain; parsers and
untrusted content; and the windows' structure (for the consolidation, `docs/axit/inventory-2026-10-24.md`). The
findings were then checked against the code before a fix. The threat model:

- **A stranger's content**: a downloaded zip or folder, a repository from an archive or a USB stick, a shared task
  folder or KeePass database, a file opened from the web, text a program prints or a web page Claude reads.
- **A network attacker** between Axit and GitHub, or a share the user's machine can be made to authenticate to.
- **A stolen GitHub account or token**, a malicious pull request, a compromised test package.
- **Another program running as the user** (malware, or an AI session hijacked by a prompt injection). It can already
  read the user's files; what Axit must not do is hand it secrets Windows would otherwise protect, a clean control
  surface, or a way to make the user's own actions do its work.
- Out of scope: an administrator, or malware with the user's rights that hooks the keyboard or reads process memory
  at will. Against those the aim is fewer copies of a secret and a shorter life, not a guarantee.

Severity: **critical** (code run or a secret lost with no action by the user), **high** (with one ordinary action),
**medium** (with an unusual action, or a partial loss), **low** (hardening).

## 2. The decisions taken

- **Releases are signed with an offline key of the user's** (2026-10-24): an Ed25519 key kept only on the user's
  machine; `release.ps1` signs the zip's SHA-256 before the release is published; Axit's updater and `install.cmd`
  refuse a zip whose signature does not check against the public key built into Axit. A stolen GitHub account cannot
  sign. Losing the key means one manual install of a release signed with a new key.
- **axPass copies a password itself** (2026-10-24): at Ctrl+C the one password is read from KeePassXC's tool, put on
  the clipboard marked private (`ExcludeClipboardContentFromMonitorProcessing`, `CanIncludeInClipboardHistory` 0,
  `CanUploadToCloudClipboard` 0) and cleared after 30 seconds if it is still there. axPass PW-D1 changes: a password is
  in axPass's memory for that moment, never for the session.

## 3. Findings

State is **open**, **fixed (commit)**, or **decided** with the reason. Grouped by surface; the number is stable.

### Processes and paths

- **S-1, high.** Programs started by bare name (`git`, `cmd.exe`, `powershell.exe`, `explorer.exe`) are looked for in
  the current folder first, and every app but axFile keeps the folder it was started in (a downloaded repository, the
  folder of a file opened from Explorer). A planted `git.exe` runs. Fix: system programs by full path; git found on the
  PATH by Axit with relative entries skipped; `NoDefaultCurrentDirectoryInExePath` set for the process.
- **S-2, high.** A repository that arrives with its `.git` folder runs its own configured programs when axGIT opens it
  (`core.fsmonitor` on `git status`), shows a diff (`diff.external`, `textconv`), commits or switches (hooks,
  `core.hooksPath`), fetches or pushes (`core.sshCommand`). Fix: `core.fsmonitor=false` and `protocol.ext.allow=never`
  always, `--no-ext-diff --no-textconv` on every diff and show, and a trust notice before any command when the
  repository's own configuration or hooks name programs, remembered per repository.
- **S-3, medium.** axFile's own programs: a `.ps1` started through a bare `powershell.exe` with the target's folder as
  the working folder; a `.bat` or `.cmd` given a file name with `&` runs what follows (the BatBadBut class). Fix: full
  path of PowerShell; targets with cmd's special characters refused for batch programs.
- **S-4, medium.** axPass's Ctrl+U opens whatever an entry's URL says through the shell (`\\server\x.exe`, `file:`,
  `search-ms:`). Fix: http and https only.
- **S-5, medium.** An axTask link field that is rooted (`//server/x.json`) is opened on every selection, which makes
  Windows authenticate to the server (the user's password hash sent away) and may read any `.json` file. Fix: links
  stay under the top folder and on this computer.
- **S-6, medium.** An axPass entry at the top level whose name starts with `-` is read by KeePassXC's tool as an
  option (`--key-file=\\server\k`). Fix: every entry path starts with `/`.
- **S-7, medium.** axFile's Extract drops the downloaded-file mark (Mark of the Web), so SmartScreen and Protected View
  no longer warn on the extracted files. Fix: the zip's `Zone.Identifier` copied onto each file.
- **S-8, low.** axFile's Compress follows folder links and junctions into other folders or shares. Fix: links skipped.
- **S-9, low.** axClaude follows a `.git` file's `gitdir:` before Claude starts, reading and writing a path the file
  names, a share included. Fix: only a folder on this computer.
- **S-10, low.** `ProgramInstall` (branch `installs`): winget not pinned to its source, and a `winget.exe` on the PATH
  used when the system one is missing. Fix on that branch: `--source winget`, no PATH fallback.

### Messages between programs, and the automation channel

- **S-11, high.** The automation channel opens for any process started with `AXIT_AUTOMATION` set, a variable every
  child inherits (new windows, axTerminal's shells, Claude); its pipe has a predictable name, the default access list
  and no secret. A program running as the user gets a clean way to type into axTerminal, run any menu command and read
  any window. Fix: a random secret the runner hands only to its child, taken out of the environment at once, asked as
  the pipe's first line; the pipe for the current user alone and local clients alone; a squatted pipe ends the
  channel, not the process. `AXIT_HOME` is honoured only with the channel's secret.
- **S-12, high.** The channel's `text` and `state` return a password field's typed text. Fix: one `IsSecret` check
  for every command that reads; a password field reads as "protected".
- **S-13, medium.** The hand-over message of a second start (`AppMessage`) is sent to a window found by process name
  and title, and trusted by the receiver. A program named `Axit.exe` with such a window receives the paths the user
  opens; any program can hand axMedia or axPreview a network path, which authenticates to it. Fix: the sender checks
  that the target's program file is its own; the receivers refuse network paths in a hand-over.
- **S-14, low.** axClip's paste checks the window in front 150 ms after hiding, then types; a narrow race. Accepted;
  the check is right and nothing is typed into a window the user did not come from.

### Secrets and personal data

- **S-15, high.** axPass's Ctrl+C goes through KeePassXC's tool, which on Windows sets no private mark: Windows
  clipboard history, cloud sync and axClip keep the password. Decided (section 2) and fixed by axPass copying itself.
  The user name copy gets the same marks.
- **S-16, medium.** Entry notes, URL, user name and title are passed to KeePassXC's tool as command-line arguments when
  saved, where other programs and the system's process auditing see them. The tool has no other way in. Fix: the
  guide and the spec say so plainly, and the notes field is saved only after a notice says it travels on a command
  line.
- **S-17, medium.** The master password is a string held in several copies and never cleared. Fix: fewer copies (the
  hidden field emptied on every notice close, one held copy dropped on lock and close); the rest is the limit of a
  managed program and is said in the spec.
- **S-18, medium.** axPass does not lock when Windows locks, the user switches, or the computer sleeps. Fix: it locks
  on all three.
- **S-19, medium.** The tool's path is trusted from the settings file or any PATH folder; a planted
  `keepassxc-cli.exe` receives the master password. Fix: the tool's signature checked (its publisher, KeePassXC)
  before a password is handed to it; Program Files before the setting.
- **S-20, medium.** Copy diagnostics copies the settings path with the user's name and 200 log lines with file paths,
  for a public issue. Fix: `Redaction` moves to `Axit.Core` (its second use) and Copy diagnostics goes through it.
- **S-21, low.** axClip's kept history is plain text in the roaming profile; Clear leaves a `.bad` copy. Fix: the
  history encrypted for the user (DPAPI), the `.bad` copy removed on Clear, copies made by axPass or KeePassXC skipped.
- **S-22, low.** axCalc keeps its history and variables on disk by default. Fix: the guide says so; the default stays,
  since a calculator's history is its purpose (the user's to change).
- **S-23, low.** A crash report of axPass's process may hold memory with secrets. Fix: no heap in its error reports.
- **S-24, low.** Raw recordings default to Documents, often synced to OneDrive. Fix: `%LOCALAPPDATA%\Axit\recordings`.
- **S-25, low.** axPass's backups keep old secrets under the old master password. Fix: the guide says so.
- **S-26, low.** axPass speaks an entry's user name in the navigator, which NVDA's log or speech viewer can record.
  Accepted for now; an option later if wanted.

### Update, install and supply chain

- **S-27, high.** The update zip is never checked: no signature, no checksum, any download address the API returns.
  Decided (section 2): signed with the user's offline key; meanwhile the address must be the repository's releases and
  the zip must match the checksum GitHub records for the asset.
- **S-28, high.** `install.cmd` alone downloads and runs the latest release unchecked, from a fixed folder in `%TEMP%`.
  Fix: the same signature check, a fresh folder, TLS 1.2 at least.
- **S-29, medium.** An update folder already in `%LOCALAPPDATA%\Axit\updates` is reused without a check. Fix: always a
  fresh download into a fresh folder.
- **S-30, medium.** The release workflow runs the tests while holding a token that can write releases, and leaves it in
  the checkout. Fix: a test job that can only read, a publish job that only packs and uploads, no stored credentials.
- **S-31, medium.** A published release's files can be replaced (`--clobber`). Fix: only a draft's files are uploaded;
  build provenance attested.
- **S-32, low.** A workflow input is pasted into a script. Fix: passed through the environment and checked.
- **S-33, low.** The build workflow has no `permissions` block. Fix: read only.
- **S-34, low.** The test packages' lock files are not enforced in CI. Fix: locked mode in CI.
- **S-35, low.** The console shims pass `%*` through cmd. Accepted and documented: they are for the user's own typing.
- **S-36, low.** No `CODEOWNERS`, tags unsigned; branch protection unseen. Fix: `CODEOWNERS`; the rest is the account's
  settings, listed for the user in `longevity.md`.

### Parsers and untrusted content

- **S-37, medium.** A program's output can forge axTerminal's shell marks (OSC 133): "Done", the next command going to
  a still-running program, a forged prompt. Fix: a random per-session nonce in the marks the shell's prompt prints;
  marks without it are ignored.
- **S-38, medium.** A program's output can set axTerminal's folder (OSC 9;9) to anything, a share included, which the
  window then checks (authenticating to the share) and offers to the other apps. Fix: the folder mark needs the nonce
  and a local path.
- **S-39, medium.** axJSON's parser and formatter have no depth limit: a file of nested brackets ends the process with
  a stack overflow, unsaved edits lost. axCalc the same for typed brackets. Fix: a depth limit of 512.
- **S-40, low.** The terminal parser's buffers grow without limit inside an unterminated sequence, which also hides the
  output after it. Fix: caps, and the sequence dropped past them.
- **S-41, low.** Scroll up and down take a count of 65,535 unclamped. Fix: clamped to the scroll region.
- **S-42, low.** Names and text from outside (file names, commit subjects, task titles) are shown with bidi overrides
  and control characters, which can disguise a file's extension visually and in braille. Fix: one `Axit.Core` helper
  that makes them visible, used for list items.
- **S-43, low.** axTerminal's "Asks:" speaks any program's last line ending in a colon with the question chime; inherent
  to a terminal. Accepted; the guide says that a password prompt in axTerminal comes from the program, never Axit.
- **S-44, low.** axClaude's question reader takes a "0." row above option 1 as an answer. Fix: only when the prompt
  names a key below 1.
- **S-45, low.** The e-mail pattern of `Redaction` backtracks on long input (tools only). Fix: a match timeout.

## 3a. State at the end of 2026-10-24

Fixed on the branch `security`, one commit per finding or close set, each with its tests and its spec:

| Findings | Commit | What |
|---|---|---|
| S-1, S-3 | e777871 | programs by full path; the PATH searched in its full folders only (`SystemPrograms`); batch programs refused cmd's special characters |
| S-2 | 3a61de4 | git's file monitor and `ext::` always off, no diff programs; a repository's own programs set aside until trusted (axGIT AG-1.6) |
| S-11, S-12 | 07091d6 | the channel's run secret, taken out of the environment; the pipe for the user alone; password fields read "protected" |
| S-13 | 7ee6136 | a hand-over only to a window of this very `Axit.exe`; the receiver's side accepted (a program of the user's could open the path itself) |
| S-20, S-45 | 88a4c0f | Copy diagnostics redacted; `Redaction` in `Axit.Core`; its e-mail pattern without backtracking |
| S-9 | 127bec4 | a `.git` file's `gitdir` only on this computer |
| S-7, S-8 | 69e3052 | the mark of the web kept on extracted files; links left out of a compressed folder |
| S-5 | 1bffbbe | axTask links rooted or off this computer never opened |
| S-4, S-6, S-15, S-18, S-19 | 5e332b1 | axPass: private clipboard (the user's decision), lock with Windows, a signed tool only, web addresses only, option-like names as paths |
| S-27 to S-34 | c9adcf0 | signed releases (the user's decision): the key on the maintainer's machine, the draft workflow, the updater's and `install.cmd`'s checks, read-only build tokens, locked restore |
| S-37, S-38 | 9dc315f | axTerminal's marks carry a per-session secret; the folder mark only after the prompt's own, on this computer |
| S-39, S-40, S-41 | b4f0b1b | depth limits in axJSON and axCalc; caps in the terminal parser; clamped scrolls |
| S-44 | 5e9d3cf | no "0." answer above the first |
| S-42 | 1f29a76 | hidden direction marks and control characters made visible in list lines |
| S-21 | 82136a9 | axClip's kept history encrypted for the account (`UserSecret`) |
| S-17, S-23, S-24 | c608eb2 | a hidden field emptied as its notice closes; no memory in axPass's crash reports; recordings in the local profile |
| S-16, S-22, S-25, S-35, S-36, S-43 | this document's commit | said in the guides and `SECURITY.md`; `CODEOWNERS` for what can ship code |

Accepted, with the reason in section 3: S-14 (the paste's check is right), S-26 (the user name in the navigator, an option
later if wanted).

Open:

- **S-10**: done with the merge of `installs` (3.7.0): winget only Windows' own from WindowsApps, with
  `--source winget`, no PATH fallback (`Axit.Core/ProgramInstall`).
- **The first signed release**: done, 3.7.0 on 2026-10-24; 3.7.1 and 3.7.2 followed signed (one release-workflow fix
  each, `tools/sign-release.ps1` and the draft's tag).
- **The account's own settings** (S-36), which no file can set: branch protection on `main` with required reviews and
  the build passing; tag protection for `v*`; GitHub's immutable releases; two-factor sign-in with a hardware key;
  signed commits and tags. Listed here for the maintainer.
- An outside review by a security professional (section 5).
## 3b. The review of 3.7.x (2026-10-25)

A second review, of the code added after this audit in 3.7.0 to 3.7.3 (the AI tools, Install it now, the Open with
links, axPreview's file readers, the update failure details, axClaude's notice letters). No high finding; each fixed
on the branch `inventory`, one commit per finding or close set, with its tests:

| Finding | Severity | Commit | What |
|---|---|---|---|
| S-46 | medium | 3c154f1 | axPreview's Ctrl+E and Open with handed any file to Windows to open, so a stranger's `.lnk`, `.hta` or `.exe` previewed and Ctrl+E'd ran; now `Axit.Core/DangerousFiles` (Windows' own `AssocIsDangerous`) and the notice "Not run from axPreview" |
| S-47 | medium | 05c26c2 | a Word, PowerPoint or OpenDocument file could unpack each part to 256 MB and be held whole on the window's thread; now one 32 MB budget per document, counted on the stream |
| S-48 | low | 05c26c2 | `XDocument.Load` parsed DTDs (internal entities expanded); now an `XmlReader` with DTDs prohibited and no resolver |
| S-49 | low | 13cdc51 | the AI tools' record of announcements kept what was said while access was off; now only what came after the setting last changed, in every process |
| S-50 | low | d6b38a4 | the Open with link allowed every process to take the foreground (`ASFW_ANY`); now its own `Axit.exe` alone |
| S-51 | low | d6b38a4 | `*.old*.exe` deleted in whatever folder `Axit.exe` ran from; now the installer's names in the installation folder only |
| S-52 | low | 13cdc51 | more script hosts never paired (bash, sh, wsl, mintty, wt, OpenConsole, dotnet, perl, ruby, java, bun, deno); node stays for npm's Claude Code |
| S-53 | low | 13cdc51 | the `claude.cmd` command line quoted only arguments with spaces; now every one, and cmd's special characters refused (`SystemPrograms.CmdSpecials`, axFile's S-3 list on its second use) |

The review's notes, unconfirmed: Windows' property handlers run inside axPreview's process for the Details list (a
third-party handler that crashes on a malformed file ends it; a hung one is left after five seconds), as they run
inside File Explorer; accepted. Copy details kept the computer's and the domain's names and a work OneDrive's
organisation: `Redaction` hides them too since the same day.

## 4. Sound, as checked

Arguments are passed as lists everywhere (no command line joined by hand), paths after `--` for git; the master and
entry passwords go to KeePassXC's tool on standard input only; password fields are the system's password edit;
settings are written atomically and a bad file falls back to defaults; zip extraction refuses paths outside the target
(in axFile and in the updater); nothing runs elevated; the installer writes only the user's own registry and quotes
every path; the workflows pin actions to commit hashes and never run on `pull_request_target`; the terminal never
answers a query back to the program (no injection by reply) and ignores clipboard writes, links and titles; the text,
item and settings parsers have size or depth limits; logs hold no typed text, clipboard text or conversation, roll at
1 MB and live in the user's own local folder; window messages cannot come from a lower-integrity process or another
user (UIPI).

## 5. What "certifiably secure" can and cannot mean here

No audit makes software certifiably secure; it makes the known attacks fail and the remaining risk written down.
What this bundle can promise after the fixes: nothing runs that the user did not start or Axit did not ship, signed;
secrets stay with the program made to hold them (KeePassXC) and pass through Axit for the shortest time, marked
private; content from strangers is displayed, never obeyed; programmatic control exists only for a client the user
paired and only for the commands the user allowed, each heard as it happens (`docs/axit/mcp.md`). An outside review by
a security professional is the step after this one, and `SECURITY.md` invites reports.
