# AxDown user guide

AxDown is a plain editor for Markdown and text files, made for NVDA and for anyone who reads with a screen reader. It is one app of Axit, one program that holds several apps for screen reader users; the others have their own guides. AxDown opens one file in one window: a menu bar, the text, and a status bar. Nothing is spoken unless you ask for it or a command finishes.

## Install

AxDown comes with Axit. The README in the Axit zip, or on the releases page (https://github.com/KyleKeane/AxClaude/releases), explains the installation: download `install.cmd` and double-click it. This puts Axit in your Programs folder and adds a Start menu entry, "Axit AxDown" (press the Windows key and type `axd`), an "Open in AxDown" entry in the right-click menu of files, AxDown under "Open with" for `.md`, `.markdown` and `.txt` files, and the `axdown` command for consoles. To make AxDown the program that opens `.md` files when you press Enter on one, use Settings, Apps, Default apps, and choose AxDown for `.md`; Windows does not let a program set that for itself.

## Start

- Start menu: Axit AxDown opens an empty document called Untitled.
- File Explorer: right-click a file and choose "Open in AxDown", or choose AxDown under "Open with".
- Console: `axdown notes.md`, or `axit notes.md`, since a file means AxDown. `axdown new.md` opens an empty document with that name, and the file is created when you save.

Each file opens in its own window; Alt+Tab moves between them.

## Write and read

The text is an ordinary Windows edit field, so NVDA's own keys read it: arrows for characters and lines, Ctrl+Left and Ctrl+Right for words, NVDA's say-all to read on from the caret, and so on. Type to write; Enter starts a new line; Tab types a tab. Ctrl+Z undoes the last change, once. Page Up and Page Down move a screen at a time and reach the first and last line.

Long lines wrap at the window's edge, so Down Arrow reads one screen line at a time. View, Word wrap (Ctrl+Shift+W) turns that off: each line of the file is then one line in the window, however long, and AxDown says "Word wrap off". Your place in the text is kept either way.

## Files

Ctrl+S saves and AxDown says "Saved". A new document asks for a name first, with the Windows file dialog; Ctrl+Shift+S, Save as, asks every time. Ctrl+O opens another file in this window and Ctrl+N starts an empty one; both ask first if you have unsaved changes, as does closing the window: a notice says "Save changes to notes.md?" with Save, Don't save and Cancel; Enter saves, Escape cancels.

AxDown writes a file back the way it found it: the same encoding, with or without a byte order mark, and the same kind of line ending. The status bar shows what it found, for example "UTF-8 · CRLF". A new document is UTF-8 without a mark. If the file changed on disk while you had it open, saving asks before writing over it.

The title is the file's name, with a star in front while there are unsaved changes: "*notes.md - AxDown".

## Find and go to a line

Ctrl+F opens Find in the window: type the text, press Enter, and the caret lands on the next match after it, which NVDA reads; "Not found" is spoken when there is none. F3 finds the next match again and Shift+F3 the previous one, without opening Find.

Ctrl+G asks for a line number; the field is named with where you are, for example "Go to line (now line 12, column 4)", so Ctrl+G is also how you ask where you are. Type a number and press Enter, or press Escape to stay. The status bar carries the line, the column and the encoding too; NVDA+End reads it.

## Text size and fonts

Ctrl+Plus and Ctrl+Minus make the text larger and smaller, and AxDown says the new size. View, Font opens the Windows font dialog. Both are remembered.

## Menus

- **File**: New, Open, Save, Save as, Exit.
- **Edit**: Undo, Cut, Copy, Paste, Select all, Find, Find next, Find previous, Go to line.
- **View**: Word wrap, Larger text, Smaller text, Font.
- **Help**: Keyboard shortcuts (F1), this guide, the version you have, the latest release on GitHub, Update or Check for updates, Copy diagnostics, About.

## Settings

Everything you change is saved in `%APPDATA%\Axit\axdown.json`: the font, the text size, word wrap, the window's place and size, and whether AxDown checks for updates when it starts.

## Updates

New versions of Axit are published on GitHub, and every app of Axit updates together. When AxDown starts, it asks GitHub once whether there is a newer one; if there is, a notice opens: Enter updates, Escape keeps the version you have. Update now downloads the new version, closes AxDown, installs it and starts it again on the same file. Help, "Check for updates" asks at any time.

## If something goes wrong

- A file that cannot be opened, or is larger than 32 MB, is reported in a notice; the window stays open and empty.
- An error from AxDown is a notice inside the window. Escape closes it. Help, Copy diagnostics puts the version, the paths and the last log lines on the clipboard for a bug report; the log is in `%LOCALAPPDATA%\Axit\logs\axdown.log`.

## About AxDown

AxDown is part of Axit, made by Dr. Kyle Keane, www.kylekeane.com. It is free under the MIT licence: use it, change it and pass it on, as long as the note that says who made it stays with it (the `LICENSE` file next to the program).

## Disclaimer

AxDown is free software offered under the MIT licence. It is provided "as is" and "as available", without warranty of any kind, express or implied, including but not limited to the implied warranties of merchantability, fitness for a particular purpose, title and non-infringement. The developer has no obligation to provide support, maintenance, updates or corrections. To the fullest extent permitted by law, the developer is not liable for any claim, damages or other liability, whether in contract, tort or otherwise, arising from or in connection with the software or its use, including loss of data or work. You use AxDown at your own risk.
