# Licence, terms and disclaimer

Axit, copyright (C) 2026 Dr. Kyle Keane (www.kylekeane.com).

## In short

- Axit is free. You may use it, study it, change it and share it, under the GNU Affero General Public License,
  version 3 only (AGPL-3.0-only), in `LICENSE`.
- If you share a changed version, or let people use a changed version over a network, you must offer them its full
  source code under the same licence, mark it as changed, and give it a name of its own.
- It comes with no warranty. You use it at your own risk, and the author is not liable for what happens when you do.
- Most apps are standalone: they need only Windows. Four are connected: axClaude, axGIT, axTerminal and axPass work
  through another program or service you install, which does the real work under its maker's terms. "Who is
  responsible for what" below says, app by app, what Axit does, what the other program does, and what is yours.
- The official copies come only from <https://github.com/KyleKeane/Axit/releases>.

The sections below are the binding terms. `LICENSE` is the licence; this file adds the terms that section 7 of that
licence allows, and the disclaimer in full words. Where this file and `LICENSE` differ on a point section 7 lets this
file decide, this file applies.

## Licence

This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General
Public License as published by the Free Software Foundation, version 3 of the License only.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied
warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more
details. You should have received a copy of it along with this program, in the file `LICENSE`; if not, see
<https://www.gnu.org/licenses/>.

The source code of every release is at <https://github.com/KyleKeane/Axit>, under the release's tag.

The copyright holder may also offer Axit, in whole or in part, under other terms. Nothing in these terms limits what
the copyright holder may do with his own work.

## Additional terms (section 7 of the licence)

These terms apply to Axit and to every copy and modified version of it, and are part of its licence.

### a. Disclaimer of warranty

Axit is provided "as is" and "as available", without warranty of any kind, express or implied, including but not
limited to the implied warranties of merchantability, fitness for a particular purpose, accuracy, title,
non-infringement, and any warranty that it will work without interruption or error, keep your data safe, or work with
any screen reader, program, device or version of Windows. The author has no obligation to provide support,
maintenance, updates, corrections or security fixes.

### b. Limitation of liability

To the fullest extent permitted by applicable law, in no event shall the author, the copyright holder or any
contributor be liable to you or to anyone else for any claim, damages or other liability, whether direct, indirect,
incidental, special, consequential, exemplary or punitive, whether in contract, tort (including negligence), strict
liability or otherwise, arising from or in connection with Axit, its installation, its updates, its use or inability
to use it, or these terms, including but not limited to loss of data, files, passwords, work, time, income or
business, damage to any computer or device, costs of substitute software, and anything done, said, sent or charged by
another program or service that Axit starts or connects to, even if advised of the possibility of such damages.

Where applicable law does not allow a limitation or exclusion of liability, the liability of the author, the copyright
holder and every contributor is limited to the smallest extent that law allows. Axit is given away without charge;
the total liability of each of them, if any remains, shall not exceed zero US dollars or, where law requires an
amount, the smallest amount it allows.

### c. Attribution kept

Every copy and modified version of Axit must keep, in its source, in the `LICENSE` and `NOTICE.md` files it is shipped
with, and in its About notices or wherever it shows its legal notices to the user, the line "Based on Axit by
Dr. Kyle Keane (www.kylekeane.com)" and a link to <https://github.com/KyleKeane/Axit>.

### d. Modified versions are marked and not passed off as the original

A modified version must say plainly, in its name, its About notices and its download page, that it is a modified
version and not the original Axit, and who changed it. No one may present a modified version, or any copy obtained
somewhere other than the official releases page, as the official Axit, or as made, approved, signed or supported by
the author.

### e. No rights to the names and icons

This licence grants no right under trademark law, or any other right, to use the name "Axit", the names of its apps
(axClaude, axDown, axJSON, axGIT, axTerminal, axFile, axMedia, axPreview, axStart, axProcess, axTask, axPass, axCalc,
axClip and the names of any later app), their icons, or any name or icon confusingly similar to them, other than to
say truthfully where a modified version comes from. A modified version that is distributed, or offered to users over
a network, must use a different name and different icons.

### f. No use of the author's name for publicity

The name of the author and copyright holder, Dr. Kyle Keane, may not be used to endorse or promote any product,
service or modified version without his prior written permission.

### g. Indemnification

Anyone who conveys Axit or a modified version of it with contractual assumptions of liability to the recipient (a
warranty, a support agreement, a service level, or any other promise of their own) shall indemnify and hold harmless
the author, the copyright holder and every contributor from and against any liability, claim, damages, cost and
expense, including reasonable lawyers' fees, that those contractual assumptions directly impose on them.

## Who is responsible for what

Axit works on your own computer, files, programs and accounts. Three parties take part, and each answers only for
its own part:

- **Axit** shows you things, and passes on the commands you give it, as this file and each app's guide describe. It
  does nothing on its own except the update check (below), and it gives no warranty for any of it.
- **Other programs and services** that some apps work through (Claude Code and Anthropic, Git and the servers your
  repositories use, your shells, KeePassXC) do the real work in those apps. Each is made, run and supported by its own
  maker, under that maker's licence, terms and privacy policy, which you accept with that maker, not with Axit's
  author. Axit's author has no control over them and is not responsible for what they do, keep, send or charge.
- **You** decide what to do, install the other programs yourself, hold the accounts, and are responsible for the
  commands you give, for checking their results, and for keeping backups.

Axit's apps come in two kinds, kept apart below: standalone apps, which need nothing but Windows, and connected apps,
which work through another program or service. A standalone app keeps working with the network off.

## Standalone apps: Axit and Windows only

These apps use only Windows' own parts. Nothing they do leaves your computer, except a link you choose to open in your
browser, or a file you choose to hand to another program.

- **axDown, axJSON and axPreview** open, show and (axDown and axJSON) save text files. Saving replaces the file on
  disk; undo works only while the window is open. Your part: keep copies of files you cannot lose.
- **axFile** deletes, moves, renames, copies, compresses and extracts files and folders, network shares among them
  when you open one. Deleting sends to the Recycle Bin, but Windows may delete for good what its Recycle Bin cannot
  hold, such as a very large file or one on a network drive, and Undo cannot bring back everything. Open with,
  Properties and the Windows menu hand a file to Windows and to the program you choose, which then acts on its own.
  Your part: check what is selected before a change, and keep backups.
- **axMedia** plays sound and video files through Windows' own media player parts. It changes no file.
- **axStart** starts the programs of your Start menu that you choose. The programs it starts act on their own.
- **axProcess** ends processes, and stops, starts and changes services and startup programs, with your rights.
  Ending a process can lose its unsaved work; stopping a service or a startup program can make Windows or a program
  stop working. Your part: know what a process or service is before you end or change it.
- **axTask** keeps tasks and lists as files in a folder you choose. Your part: keep backups of that folder.
- **axCalc** calculates the expressions you type. Its results come with no warranty: check any result that matters
  before relying on it.
- **axClip** keeps a history of the text you copy, which can include passwords and other private text that a program
  did not mark to be kept out of clipboard histories. It keeps the history in memory, and on disk, encrypted for your
  Windows account, only when you turn on Keep history. Your part: pause recording (Options) before copying something
  private, and clear the history when you need to.
- **axStart** and **axClip** start with Windows and wait for their hotkeys until you turn them off or uninstall Axit.

## Connected apps: through another program or service

These apps are front ends: Axit shows what the other program does and passes your commands to it. The other program
does the work, under its maker's terms. Each needs that program installed by you; Axit does not include it.

- **axClaude** works through **Claude Code**, Anthropic's program, and through it **Anthropic's service**, under your
  own Anthropic account.
  - Axit: starts Claude Code in your folder, shows what it prints, and passes on what you type and answer. It keeps
    no copy of your conversation, except a recording you turn on yourself (Options, Developer).
  - Claude Code and Anthropic: everything you type goes to them, under Anthropic's terms and privacy policy. Claude
    Code can create, change and delete files, run commands, and use your account's usage or money; its answers can
    be wrong.
  - You: what Claude Code may do (its permission questions are yours to answer), checking its changes, its costs, and
    your agreement with Anthropic. Axit is not affiliated with Anthropic, and its author sees nothing you send.
- **axGIT** works through **Git**, and, when you fetch, pull or push, the **server your repository uses** (GitHub or
  another).
  - Axit: runs Git commands on the repository in hand and shows their results.
  - Git and the server: do the work; a push sends your commits to the server, under its terms.
  - You: discarding changes, resetting and similar commands remove work for good; a push publishes. Check before
    you confirm.
- **axTerminal** works through **your own shell** (PowerShell, Command Prompt or Git Bash).
  - Axit: shows what the shell prints and passes on what you type.
  - The shell and the commands you run: do whatever those commands do, with your rights, on your computer and on
    any network they reach.
  - You: every command you type.
- **axPass** works through **KeePassXC's command-line tool**, which does all the encryption.
  - Axit: shows entries and passes your changes and your master password to the tool, on a pipe; it never stores a
    password, and a password it copies is cleared from the clipboard after 30 seconds. Before the first change after
    each unlock it backs up the database file beside it.
  - KeePassXC: reads and writes your database, under its own licence.
  - You: your master password, which no one can recover for you, and backups of the database. A wrong change, a full
    disk, or another program writing the same file at the same time can damage a database.

## Services of the whole bundle

- **The update check** asks GitHub for the latest release when a window starts; turn it off in Options, General. An
  update you choose downloads the release from this repository's releases page, checks its signature, and replaces
  the program for your Windows account only.
- **Install it now**, in a missing program's notice, installs Git, KeePassXC, PowerShell 7 or Claude Code only when
  you choose it, through Windows' package manager or the maker's own installer. The program you install, and its
  installer, are under the maker's terms.
- **AI tools** (Axit menu, AI tools) are off until you turn them on. On, an AI tool on your computer that you pair can
  read what Axit's windows show and open apps; axPass and axClip are never read. What the tool then does with what
  it read is under its maker's terms.
- **Links** in help menus, notices and files open in your browser only when you choose them.

`PRIVACY.md` says in full what Axit contacts and when.

## For every app

- **Back up what matters.** Keep your own backups of your files, repositories, task lists and password databases.
  Axit can make mistakes, and so can the programs it works through.
- **Not for critical use.** Axit is not designed, tested or certified for medical, safety-critical, legal, financial
  or emergency use, nor as an assistive technology on which access to anything essential depends; have another way
  to reach anything you cannot do without.
- **Your use is yours.** You are responsible for using Axit in line with the law, your employer's rules and the terms
  of every program and service you reach with it.

## Other programs and names

The programs and services named above are not part of Axit and are not included in it; what the program file itself
carries from others (the .NET runtime and Windows Forms) is in `THIRD-PARTY-NOTICES.md`. Axit is an independent
project, not affiliated with, sponsored or endorsed by Anthropic, NV Access, Microsoft, GitHub, KeePassXC or any other
company or project it names; their names and marks belong to them and are used only to say what Axit works with.

## Contributions

Contributions are accepted only under the terms in `CONTRIBUTING.md`, which include a licence to the copyright holder
to publish the contribution under these and other terms.

## Governing terms

If any part of these terms is held unenforceable, the rest stays in force, and that part is read as the closest
enforceable term. Nothing in these terms limits a right that applicable consumer law gives you and that cannot be
limited by agreement.
