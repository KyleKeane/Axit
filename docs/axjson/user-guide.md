# axJSON user guide

axJSON is a small editor for JSON files, part of Axit, made for working with a screen reader: one plain text field with the whole file in it, keys that jump through the file by its structure, and a Format command that lays the file out and repairs the small things that make JSON invalid. It opens one file in one window. The structured view with a field per value is planned for a later version.

## Install

Axit installs all its apps at once; see the Axit guide (Axit menu, About, Axit guide). axJSON is "Axit axJSON" in the Start menu, `axjson` in a console, and "Open with" for `.json` files in File Explorer.

## Start

- Start menu: Axit axJSON opens an empty document.
- Console: `axjson settings.json` opens that file; so does `axit settings.json`, since a `.json` file means axJSON.
- File Explorer: right-click a `.json` file, Open with Axit, axJSON; or Windows' own Open with, axJSON.

## Write and read

The window is one text field named after the file, so NVDA reads it as any text: the arrow keys move, Ctrl+Home and Ctrl+End go to the ends, Page Up and Page Down move a screen, Ctrl+Up and Ctrl+Down move by paragraph. Nothing is spoken that you did not ask for.

Long lines wrap at the window's edge, so Down Arrow reads one screen line at a time. Options, Text, Word wrap (Ctrl+Shift+W) turns that off, and each line of the file is then one line in the window.

## Files

Ctrl+S saves and axJSON says "Saved". A new document asks for a name first, with the Windows file dialog; Ctrl+Shift+S, Save as, asks every time. Ctrl+O opens another file in this window and Ctrl+N starts an empty one; both ask first if you have unsaved changes, as does closing the window: a notice says "Save changes to settings.json?" with Save, Don't save and Cancel; Enter saves, Escape cancels. File, Open recent file lists the last ten files you opened or saved; File, Show current file in, File Explorer (Ctrl+W) opens File Explorer with the file selected.

When another program changes the file while it is open here, for example Claude editing it while you are in axClaude, axJSON notices when you come back to its window: with nothing unsaved it reloads the file, keeps your line, and says "Reloaded, settings.json changed on disk"; with unsaved changes it says "settings.json changed on disk; Save will ask", and Ctrl+S then offers Write over, Reload and lose mine, or Cancel.

axJSON writes a file back the way it found it: the same encoding, with or without a byte order mark, and the same kind of line ending. The status bar shows what it found, for example "UTF-8 · CRLF". If the file changed on disk while you had it open, saving asks before writing over it.

The title is the file's name, with a star in front while there are unsaved changes: "*settings.json - axJSON".

## Find and go to a line

Ctrl+F opens a Find field inside the window; Enter finds the next match, selects it and reads its line; F3 and Shift+F3 repeat forwards and backwards. Ctrl+G opens a Go to line field, named with where you are, "Go to line (now line 12, column 4)"; type a number and press Enter, or Escape to stay.

## Move by structure

The keys follow axDown's: Ctrl and a letter go to the next thing of that kind, Ctrl+Shift and the letter to the previous one, and each jump speaks what it landed on. The caret goes to the key or the value, so Enter and typing act right there.

- Ctrl+K: the next key, spoken with its value, "model: opus", or with its size when the value is an object or an array, "permissions, object with 2 keys".
- Ctrl+J: the next object, "permissions, object with 2 keys"; the whole file is the first one.
- Ctrl+L: the next array, "allow, array with 2 items".
- Ctrl+I: the next item of an array, "Item 2 of 2, Read".
- Ctrl+E: the place where the file stops being JSON, "Error, line 12: expected a comma or a closing brace after model, found a string". When the file is valid you hear "No next error", which is the quickest way to check a file.

Past the last one of a kind you hear "No next key" and the caret stays. The same keys work in the user guide and the other notices. The Navigate menu lists all of them under Next and Previous.

## Format

Edit, Format (Ctrl+Shift+F) writes the file out again with two spaces per level and one key or item per line, keeping the order of the keys and the file's line endings, and says "Formatted". Small mistakes are repaired on the way and named: a comma after the last item, a missing comma between two items, single quotes, a key without quotes, and comments, so "Formatted, repaired: 1 trailing comma, 1 unquoted key". Anything else leaves the file as it is and names the line: "Not valid JSON: line 12, expected a value, found '}'". Format is one step for Undo (Ctrl+Z), and Redo (Ctrl+Y) formats again. The history reaches back to when the file was opened in this window, a run of typing being one step: "Undone", "Redone", "Nothing to undo", "Nothing to redo".

## Text size and fonts

Ctrl+Plus and Ctrl+Minus make the text larger and smaller, and axJSON says the new size. Options, Text, Font opens the Windows font dialog. Both are remembered.

## Menus

- **File**: New, Open, Open recent file, Save, Save as, Show current file in (File Explorer, Ctrl+W; axGIT, a git manager on the repository that holds the file; axTerminal, a shell in the file's folder; axFile, the file's folder with the file selected), Exit.
- **Edit**: Undo, Redo, Cut, Copy, Paste, Select all, Format (Ctrl+Shift+F).
- **Navigate**: Find, Find next, Find previous, Go to line, and the submenus Next and Previous, each with Key, Object, Array, Item and Error.
- **Options**: Text (Word wrap, Font, Larger text, Smaller text, Windows text size), General (Check for updates at start) and Developer (Open settings file, which opens axjson.json in a new axJSON window, and Copy diagnostics).
- **Axit**: the bundle's menu, the same in every Axit window: New window, Recently closed, Open windows, Open windows by type, Close all windows, Check for updates, Update to the newest version while one is known, and About.
- **Help**: Keyboard shortcuts (F1), this guide, JSON resources (the JSON syntax page and Claude Code's settings documentation, in your browser).

## Settings

axJSON remembers the font, the text size, word wrap, the recent files and the window's place in `%APPDATA%\Axit\axjson.json`. Options, Developer, Open settings file opens it in a new axJSON window.

## Updates

New versions of Axit are published on GitHub, and every app of Axit updates together. When axJSON starts, it asks GitHub once whether there is a newer one; if there is, a notice opens with four buttons. Update and restart downloads and installs the new version, then closes axJSON (asking first about unsaved changes) and starts the new version on the same file; Update and close installs it and closes without starting it again. Later keeps the version you have, and Axit, Update to Axit n offers it until you take it. Axit, Check for updates asks at any time. Axit, About, Version names the version you have and the latest release on GitHub.

## If something goes wrong

- A file that cannot be opened, is not text, or is larger than 32 MB, is reported in a notice; the window stays open, empty and untitled, so that saving cannot write over the file.
- An error from axJSON is a notice inside the window. Escape closes it. Options, Developer, Copy diagnostics puts the version, the paths and the last log lines on the clipboard for a bug report; the log is in `%LOCALAPPDATA%\Axit\logs\axjson.log`.

## About axJSON

axJSON is part of Axit, made by Dr. Kyle Keane, www.kylekeane.com. It is free under the MIT licence: use it, change it and pass it on, as long as the note that says who made it stays with it (the `LICENSE` file next to the program).

## Disclaimer

axJSON is provided "as is" and "as available", without warranty of any kind, express or implied, including but not limited to the implied warranties of merchantability, fitness for a particular purpose, title and non-infringement. The developer has no obligation to provide support, maintenance, updates or corrections. To the fullest extent permitted by law, the developer is not liable for any claim, damages or other liability, whether in contract, tort or otherwise, arising from or in connection with the software or its use, including loss of data or work. You use axJSON at your own risk.
