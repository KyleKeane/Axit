# AxDown user guide

AxDown is a plain editor for Markdown and text files, made for NVDA and for anyone who reads with a screen reader. It is one app of Axit, one program that holds several apps for screen reader users; the others have their own guides. AxDown opens one file in one window: a menu bar, the text, and a status bar. Nothing is spoken unless you ask for it or a command finishes.

## Install

AxDown comes with Axit. The README in the Axit zip, or on the releases page (https://github.com/KyleKeane/Axit/releases), explains the installation: download `install.cmd` and double-click it. This puts Axit in your Programs folder and adds a Start menu entry, "Axit AxDown" (press the Windows key and type `axd`), an "Open in AxDown" entry in the right-click menu of files, AxDown under "Open with" for `.md`, `.markdown` and `.txt` files, and the `axdown` command for consoles. To make AxDown the program that opens `.md` files when you press Enter on one, use Settings, Apps, Default apps, and choose AxDown for `.md`; Windows does not let a program set that for itself.

## Start

- Start menu: Axit AxDown opens an empty document called Untitled.
- File Explorer: right-click a file and choose "Open in AxDown", or choose AxDown under "Open with".
- Console: `axdown notes.md`, or `axit notes.md`, since a file means AxDown. `axdown new.md` opens an empty document with that name, and the file is created when you save.

Each file opens in its own window; Alt+Tab moves between them.

## Write and read

The text is an ordinary Windows edit field, so NVDA's own keys read it: arrows for characters and lines, Ctrl+Left and Ctrl+Right for words, NVDA's say-all to read on from the caret, and so on. Type to write; Enter starts a new line; Tab types a tab. Ctrl+Z undoes the last change, once. Page Up and Page Down move a screen at a time and reach the first and last line. Ctrl+Up and Ctrl+Down move to the previous and next paragraph, the first line after a blank line, and with Shift they select to there, as in Word.

Long lines wrap at the window's edge, so Down Arrow reads one screen line at a time. Options, Text, Word wrap (Ctrl+Shift+W) turns that off: each line of the file is then one line in the window, however long, and AxDown says "Word wrap off". Your place in the text is kept either way.

## Files

Ctrl+S saves and AxDown says "Saved". A new document asks for a name first, with the Windows file dialog; Ctrl+Shift+S, Save as, asks every time. Ctrl+O opens another file in this window and Ctrl+N starts an empty one; both ask first if you have unsaved changes, as does closing the window: a notice says "Save changes to notes.md?" with Save, Don't save and Cancel; Enter saves, Escape cancels. File, Open recent file lists the last ten files you opened or saved; File, Show current file in, File Explorer (Ctrl+W) opens File Explorer with the file selected.

AxDown writes a file back the way it found it: the same encoding, with or without a byte order mark, and the same kind of line ending. The status bar shows what it found, for example "UTF-8 · CRLF". A new document is UTF-8 without a mark. If the file changed on disk while you had it open, saving asks before writing over it.

The title is the file's name, with a star in front while there are unsaved changes: "*notes.md - AxDown".

## Find and go to a line

Ctrl+F opens Find in the window: type the text, press Enter, and the caret lands on the next match after it, which NVDA reads; "Not found" is spoken when there is none. F3 finds the next match again and Shift+F3 the previous one, without opening Find.

Ctrl+G asks for a line number; the field is named with where you are, for example "Go to line (now line 12, column 4)", so Ctrl+G is also how you ask where you are. Type a number and press Enter, or press Escape to stay. The status bar carries the line, the column and the encoding too; NVDA+End reads it.

## Move by structure

In a Markdown file, the keys NVDA uses on a web page work with Ctrl: Ctrl+H moves to the next heading (a line that starts with one or more `#`) and Ctrl+Shift+H to the previous one; Ctrl+1 to Ctrl+6 to the next heading of that level only; Ctrl+K to the next link, Ctrl+L to the next list, Ctrl+I to the next list item, Ctrl+Q to the next block quote and Ctrl+T to the next table, each with Shift added for the previous one. The caret lands at the start of what was found and AxDown says what it is, as NVDA would: "Install, heading level 2", "GitHub, link", "List with 3 items, one", "Block quote, …", "Table with 3 rows and 2 columns". When there is none, AxDown says so ("No next link") and the caret stays. Lines inside fenced code blocks are skipped. The Navigate menu lists all of them under Next and Previous. They work in the user guide and the other help texts too.

## Text size and fonts

Ctrl+Plus and Ctrl+Minus make the text larger and smaller, and AxDown says the new size. Options, Text, Font opens the Windows font dialog. Both are remembered.

## Menus

- **File**: New, Open, Open recent file, Show current file in (File Explorer, Ctrl+W), Save, Save as, Exit.
- **Edit**: Undo, Cut, Copy, Paste, Select all.
- **Navigate**: Find, Find next, Find previous, Go to line, and the submenus Next and Previous, each with Heading, Heading level 1 to 6, Link, List, List item, Block quote and Table.
- **Options**: Text (Word wrap, Font, Larger text, Smaller text, Windows text size) and Developer (Open settings file, which opens axdown.json in a new AxDown window, and Copy diagnostics).
- **Axit**: the bundle's menu, the same in every Axit window. New window starts another AxDown or AxClaude. Recently closed lists the last ten files and folders any Axit window held, each with a number; Enter opens one in a new window. Open windows lists the other open Axit windows, the one you were in last first, each with a number: Alt+A, O, 1 takes you back to it. Open windows by type has a submenu per app with its windows, the one you are in checked. Enter on a window brings it to the front. Close all windows closes every Axit window, each asking about unsaved changes where there are any. Then Check for updates (which names the version you have), Update to the newest version while one is known, and About, a submenu: Version (the version, the apps, the latest release and where the settings and logs are), Licence, Attribution, Axit guide (how to install, start, update and remove the bundle) and Axit on GitHub, which opens the project's page in your browser.
- **Help**: Keyboard shortcuts (F1), this guide, Markdown resources (Markdown basics and GitHub Flavored Markdown, in your browser).

## Settings

Everything you change is saved in `%APPDATA%\Axit\axdown.json`: the font, the text size, word wrap, the window's place and size, and whether AxDown checks for updates when it starts.

## Updates

New versions of Axit are published on GitHub, and every app of Axit updates together. When AxDown starts, it asks GitHub once whether there is a newer one; if there is, a notice opens with four buttons. Update and restart (Enter) downloads and installs the new version while AxDown stays open, then closes AxDown (asking first about unsaved changes) and starts the new version on the same file; Update and close installs it and closes without starting it again. One progress bar covers the download and the installation, and NVDA reports it the way it reports any progress bar (beeps or percentages, by your NVDA settings). Escape during the download cancels it and keeps the version you have; once the installation has begun it runs to the end, a few seconds. Open release page opens it in the browser; Later keeps the version you have, and Axit, "Update to Axit n" offers it until you take it. Axit, "Check for updates (Axit n)" names the version you have, asks at any time and is always there, so after skipping a version you can look again. Axit, About, Version names the version you have, the latest release on GitHub and where the settings and logs are.

## If something goes wrong

- A file that cannot be opened, is not text, or is larger than 32 MB, is reported in a notice; the window stays open, empty and untitled, so that saving cannot write over the file.
- An error from AxDown is a notice inside the window. Escape closes it. Options, Developer, Copy diagnostics puts the version, the paths and the last log lines on the clipboard for a bug report; the log is in `%LOCALAPPDATA%\Axit\logs\axdown.log`.

## About AxDown

AxDown is part of Axit, made by Dr. Kyle Keane, www.kylekeane.com. It is free under the MIT licence: use it, change it and pass it on, as long as the note that says who made it stays with it (the `LICENSE` file next to the program).

## Disclaimer

AxDown is free software offered under the MIT licence. It is provided "as is" and "as available", without warranty of any kind, express or implied, including but not limited to the implied warranties of merchantability, fitness for a particular purpose, title and non-infringement. The developer has no obligation to provide support, maintenance, updates or corrections. To the fullest extent permitted by law, the developer is not liable for any claim, damages or other liability, whether in contract, tort or otherwise, arising from or in connection with the software or its use, including loss of data or work. You use AxDown at your own risk.
