# axPreview user guide

axPreview is a viewer for one text file, made for screen reader users: the file's text in a plain read-only field, to read and not to change, with every reading key axDown has: the arrows, Page Up and Page Down, Ctrl+Up and Ctrl+Down by paragraph, find, Go to line, `l` for the line, and for a Markdown file the keys that jump between headings, links, lists, list items, block quotes and tables. Ctrl+E opens the same file in its editor for the one time in ten you want to change something.

## Install

axPreview is part of Axit. Installing Axit (see the Axit guide in the Axit menu, About) gives you a Start menu entry `Axit axPreview`, a console command `axpreview`, axPreview in the `Open with Axit` submenu when you right-click any file in File Explorer, and axPreview under Windows' own `Open with` for every kind of file, so that you can make it the program that opens a kind when you press Enter on one: Settings, Apps, Default apps, axPreview, or right-click a file, Open with, Choose another app, axPreview, Always. Windows lets only you set that, never a program.

## Start

- From axFile: Shift+Enter on a file, or the entry's menu (Shift+F10), Preview. The axPreview window opens on the file and comes to the front; Alt+A, O, 1 goes back to axFile. Enter on a file in axFile opens it in the program you have made its default in Windows, which can be axPreview.
- File Explorer: right-click a file, open `Open with Axit` and choose axPreview; or double-click it once axPreview is its default program.
- Start menu: `Axit axPreview`. Press the Windows key and type `axp`. The window opens empty, "Untitled"; Ctrl+O opens a file.
- Console: `axpreview C:\notes\README.md`.

axPreview keeps one window: a file opened from axFile, File Explorer or the console while an axPreview window is open goes into that window rather than opening another; File, Open recent lists the last ten. A second window is the Axit menu's New window, Ctrl+N, or `--new` on the console.

The window is named after the file: "README.md - axPreview". NVDA reads the title, then "README.md, read only, edit, multi line" and the first line. The status bar (NVDA+End) has the file's name on the left and "Line 12, Column 4 · UTF-8 · CRLF" on the right.

## Reading

The text is the one control. Arrow keys, Home, End, Ctrl+Home, Ctrl+End, Ctrl+Left and Ctrl+Right move as in any text; Shift with any of them selects; Ctrl+A selects all and Ctrl+C copies. Typing changes nothing. Nothing is spoken until you ask.

- **Page Up** and **Page Down** move one screen, or to the first or last line. **Ctrl+Up** and **Ctrl+Down** go to the previous or next paragraph.
- **l** says where you are, "Line 12 of 40". **Ctrl+G** goes to a line: the field under the text says where you are now, type the number, Enter.
- **Ctrl+F** finds text; F3 and Shift+F3 the next and the previous match, the match selected and read.
- **Ctrl+H** and **Ctrl+Shift+H** go to the next and the previous Markdown heading, spoken as "Install, heading level 2"; **Ctrl+1** to **Ctrl+6** to the next heading of that level only, Ctrl+Shift with the number back. **Ctrl+K** links, **Ctrl+L** lists, **Ctrl+I** list items, **Ctrl+Q** block quotes, **Ctrl+T** tables, each with Shift for the previous one; "No next heading" when there is none. They work in a plain text file too, where there are none, and in every notice text. Navigate, Next and Previous list them.
- **F5** reads the file again, the caret kept on its line, "Reloaded". Coming back to the window after another program changed the file does the same by itself: "Reloaded, README.md changed on disk".
- **Ctrl+E** opens the file in axDown, or in axJSON for a `.json` file, a new editor window; File, Edit in axDown is the same item, named after the file's kind.
- **Ctrl+W** shows the file in File Explorer; File, Show current file in also offers axClaude and axTerminal (in its folder), axGIT (the repository that holds it) and axFile (the file selected in its folder).
- **Ctrl+O** opens another file with the Windows file picker; File, Open recent lists the last ten, "1. README.md (C:\notes\README.md)".
- **Ctrl+Shift+W** turns word wrap off and on (Options, Text); the caret stays where it was.
- **Ctrl+S** says "Nothing to save here": nothing is ever changed, so nothing is ever lost.

Any file whose first bytes read as text opens, up to 32 MB: the editors' kinds always, a log, a script, a file without an extension. A file that is not text gives the notice "Not a text file" and the window keeps what it had; axFile's Enter opens such a file in the program Windows has for it. A file over 32 MB gives "Too large".

## Menus

- **File**: New window (Ctrl+N), Open (Ctrl+O), Open recent, Reload (F5), Edit in axDown or Edit in axJSON (Ctrl+E), Show current file in (File Explorer Ctrl+W, axClaude, axTerminal, axGIT, axFile), Exit.
- **Navigate**: Find (Ctrl+F), Find next (F3), Find previous (Shift+F3), Go to line (Ctrl+G), Next and Previous (heading, heading level 1 to 6, link, list, list item, block quote, table).
- **Options**: Text (word wrap, font and size), General (the update check), Developer (the settings file, diagnostics).
- **Axit**: new windows of every app, the files and folders closed lately, the open windows, updates, About.
- **Help**: Keyboard shortcuts (F1), this guide, Markdown resources.

## Settings

Everything you change is kept in `%APPDATA%\Axit\axpreview.json`: word wrap, the recent files, the font and text size, the update check. Options, Developer, Open settings file opens it in axJSON.

## Updates

axPreview checks for a newer Axit when it starts and offers it in a notice; the Axit menu has Check for updates at any time. One update brings every app of Axit up to date.

## If something goes wrong

axPreview's log is `%LOCALAPPDATA%\Axit\logs\axpreview.log`; Options, Developer, Copy diagnostics copies its end with the versions, for a bug report. A file that could not be read is reported in a notice with Windows' message.

## About axPreview

axPreview is one app of Axit, by Dr. Kyle Keane (www.kylekeane.com), MIT licence. The Axit menu's About names the version and the licence.

## Disclaimer

axPreview reads your files and never writes them.
