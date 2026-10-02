# Axit

Axit is one Windows program that holds several apps for people who use a screen reader. It is made for NVDA and works with any screen reader that reads ordinary Windows text fields. Each app has its own window, its own Start menu entry, its own right-click entry in File Explorer and its own console command, and all of them share this one installation and update together.

The apps:

- **AxClaude** runs the Claude Code CLI for you and shows everything Claude says in a plain text window, one line at a time, with a few keys to move around and spoken notices when Claude needs you. Open its guide inside the app: Help, User guide.
- **AxDown**, a plain editor for Markdown and text files, is coming in a later version.

## Install

Download `install.cmd` from the latest release on the releases page (https://github.com/KyleKeane/AxClaude/releases) and double-click it. It fetches the latest Axit and installs it; press a key when it is done. If you have the zip instead, extract it and double-click the `install.cmd` inside. If Windows SmartScreen warns about an unknown publisher, choose More info, then Run anyway.

This puts Axit in your Programs folder (`%LOCALAPPDATA%\Programs\Axit`) and adds, for each app, a Start menu entry (`Axit AxClaude`: press the Windows key and type `axc`), an entry in the right-click menu of File Explorer (`Open in AxClaude` on folders) and a console command (`axclaude`; `axit` alone opens the app that fits the path you give it). Settings, Apps lists Axit with an Uninstall. No administrator rights are needed, and no PowerShell setting has to change.

If you had AxClaude installed before Axit existed, the installer removes it first and AxClaude takes over your settings the first time it starts.

## Start

- Start menu: `Axit AxClaude`.
- Console: `axclaude`, or `axclaude C:\my\project`. `axit C:\my\project` does the same, since a folder means AxClaude.
- File Explorer: right-click a folder and choose `Open in AxClaude`.

## Update

Each app offers new versions itself (Help menu, Update). You can also run `install.cmd` again. Either way the old version is removed completely first and the new one is installed fresh. Your settings stay.

## Remove

Settings, Apps, Installed apps, Axit, Uninstall (or Add or remove programs in the Control Panel). Close the apps first: while one runs, nothing is removed and the window says so. A console window says what was removed; press a key to close it. Your settings stay in `%APPDATA%\Axit` and the logs in `%LOCALAPPDATA%\Axit`. From a console, `"%LOCALAPPDATA%\Programs\Axit\install.cmd" -Uninstall` does the same.

## About

Axit is made by Dr. Kyle Keane, www.kylekeane.com. It is free under the MIT licence: use it, change it and pass it on, as long as the note that says who made it stays with it (the `LICENSE` file next to the program). The source is at https://github.com/KyleKeane/AxClaude.

The software is provided "as is" and "as available", without warranty of any kind, express or implied. The developer has no obligation to provide support, maintenance, updates or corrections, and, to the fullest extent permitted by law, is not liable for any claim, damages or other liability arising from or in connection with the software or its use, including loss of data or work and anything done, said or charged by Claude Code or the services behind it. You use Axit at your own risk. Axit is an independent project, not affiliated with or endorsed by Anthropic.
