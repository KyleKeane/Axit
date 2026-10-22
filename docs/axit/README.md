# Axit

Axit is one Windows program that holds several apps for people who use a screen reader. It is made for NVDA and works with any screen reader that reads ordinary Windows text fields. Each app has its own window, its own Start menu entry, its place in File Explorer's Open with Axit submenu and its own console command, and all of them share this one installation and update together.

The apps:

- **AxClaude** runs the Claude Code CLI for you and shows everything Claude says in a plain text window, one line at a time, with a few keys to move around and spoken notices when Claude needs you.
- **AxDown** is a plain editor for Markdown and text files: one file in one window, written back exactly as it was found, with find, go to line, and questions asked inside the window.
- **AxJSON** (beta) is the same editor for JSON files, with keys that jump from key to key, object to object and array to array, a key that lands on the first mistake, and a Format command that lays the file out and repairs the small things.
- **AxGit** (first draft) is a git manager for one repository: a list of the changed files, the log or the branches, the details of what you pick as text, and the daily commands, stage, commit, switch, fetch, pull and push, each one announcement. It opens from the other windows on the repository of the folder or file you are in.
- **AxTerminal** (first draft) is a terminal: a shell (PowerShell, or Command Prompt) in AxClaude's kind of window, where every command and what it printed stand under their own Input and Output lines, the keys of AxClaude find them again, and you hear when a command is done and whether it failed. It opens from the other windows on the folder you are in.
- **AxFile** (first draft) is a file manager for one folder: a plain list of its entries, folders first, where Enter opens a folder in the list and a file in the program you have made its default in Windows, as in File Explorer. New folder, new text file, rename, delete to the Recycle Bin, cut, copy and paste, move to a new folder, zip and unzip, copy the path, with Space to select several; every operation is a line of the Actions list under the folder list, to read again, undo (Ctrl+Z) or jump back to (Enter). It opens from the other windows on the folder or file you are in, and hands sound and video to AxMedia and reading to AxPreview.
- **AxMedia** is a player for sound and video files with one queue: Enter on a file in AxFile or File Explorer (once AxMedia is its default program) plays it with its folder's other files after it; Space pauses, Ctrl+Left and Ctrl+Right seek, Ctrl+Shift+Left and Ctrl+Shift+Right change file, Ctrl+Shift+Comma and Ctrl+Shift+Period change the speed with the pitch kept, Ctrl+Q says what is playing. One window takes everything you open; a video's picture shows at the top of it.
- **AxPreview** is a read-only viewer for one text file: the text in a plain field with every reading key AxDown has, including the Markdown navigation keys, `l` for the line, Go to line and find; Ctrl+E opens the file in its editor. Shift+Enter in AxFile reads a file here; one window takes everything you open.

Each app's guide is inside it: Help, User guide.

## Install

Download `install.cmd` from the latest release on the releases page (https://github.com/KyleKeane/Axit/releases) and double-click it. It fetches the latest Axit and installs it; press a key when it is done. If you have the zip instead, extract it and double-click the `install.cmd` inside. If Windows SmartScreen warns about an unknown publisher, choose More info, then Run anyway.

This puts Axit in your Programs folder (`%LOCALAPPDATA%\Programs\Axit`) and adds, for each app, a Start menu entry (`Axit AxClaude`, `Axit AxDown`, `Axit AxJSON`, `Axit AxGit`, `Axit AxTerminal`, `Axit AxFile`, `Axit AxMedia`, `Axit AxPreview`: press the Windows key and type `axc`, `axd`, `axj`, `axg`, `axt`, `axf`, `axm` or `axp`), a place in the `Open with Axit` submenu of File Explorer's right-click menu (AxClaude, AxGit, AxTerminal and AxFile on folders, AxDown, AxFile and AxPreview on every file, AxJSON on `.json` files, AxMedia on sound and video files) and a console command (`axclaude`, `axdown`, `axjson`, `axgit`, `axterminal`, `axfile`, `axmedia`, `axpreview`; `axit` alone opens the app that fits the path you give it). Settings, Apps lists Axit with an Uninstall. No administrator rights are needed, and no PowerShell setting has to change.

The install also registers the apps that open files as programs Windows knows: AxDown for `.md`, `.markdown` and `.txt`, AxJSON for `.json`, AxMedia for the sound and video kinds, AxPreview for `.txt`, `.md`, `.markdown`, `.log` and `.json`. They appear under Windows' own `Open with`, and you can make one the program that opens a kind when you press Enter on a file, in AxFile or in File Explorer: Settings, Apps, Default apps, the app, then the kind; or right-click a file, Open with, Choose another app, the app, Always. Windows does not let a program set that for itself, so Axit never does.

If you had AxClaude installed before Axit existed, the installer removes it first and AxClaude takes over your settings the first time it starts.

## Start

- Start menu: `Axit AxClaude`, `Axit AxDown`, `Axit AxJSON`, `Axit AxGit`, `Axit AxTerminal`, `Axit AxFile`, `Axit AxMedia`, `Axit AxPreview`.
- Console: `axclaude`, or `axclaude C:\my\project`; `axdown notes.md`; `axterminal C:\my\project`; `axfile C:\Music`; `axmedia C:\Music\Dune` or `axmedia 03.mp3`; `axpreview README.md`. `axit C:\my\project` and `axit notes.md` do the same as the first two, since a folder means AxClaude and a file AxDown, a `.json` file AxJSON, a sound or video file AxMedia.
- File Explorer: right-click a folder, open `Open with Axit` and choose AxClaude, AxGit, AxTerminal or AxFile; right-click a file, open `Open with Axit` and choose AxDown, AxFile or AxPreview, AxJSON on a `.json` file, AxMedia on a sound or video file. On Windows 11 the submenu is in the full menu (Shift+F10, or Show more options).
- From any Axit window: the Axit menu (Alt+A) starts a new AxClaude or AxDown, reopens a file or folder you closed lately, lists the open Axit windows, the one you were in last first and by type, closes them all, checks for updates, and has About with the version, the licence, the attribution, this guide and the project's page on GitHub.

## Update

Each app offers new versions itself (Axit menu, Check for updates and Update to). You can also run `install.cmd` again. Either way the old version is removed completely first and the new one is installed fresh. Your settings stay.

## Remove

Settings, Apps, Installed apps, Axit, Uninstall (or Add or remove programs in the Control Panel). Close the apps first: while one runs, nothing is removed and the window says so. A console window says what was removed; press a key to close it. Your settings stay in `%APPDATA%\Axit` and the logs in `%LOCALAPPDATA%\Axit`. From a console, `"%LOCALAPPDATA%\Programs\Axit\install.cmd" -Uninstall` does the same.

## About

Axit is made by Dr. Kyle Keane, www.kylekeane.com. It is free under the MIT licence: use it, change it and pass it on, as long as the note that says who made it stays with it (the `LICENSE` file next to the program). The source is at https://github.com/KyleKeane/Axit.

The software is provided "as is" and "as available", without warranty of any kind, express or implied. The developer has no obligation to provide support, maintenance, updates or corrections, and, to the fullest extent permitted by law, is not liable for any claim, damages or other liability arising from or in connection with the software or its use, including loss of data or work and anything done, said or charged by Claude Code or the services behind it. You use Axit at your own risk. Axit is an independent project, not affiliated with or endorsed by Anthropic.
