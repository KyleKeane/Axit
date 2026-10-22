# AxStart user guide

AxStart is a program launcher, made for screen reader users: one key from anywhere brings up a search field, you type part of a program's name, Down and Up move through the programs that match while the caret stays in the field, and Enter starts the one you are on. The programs are the ones the Start menu has, the desktop programs and the Store apps (Settings, Calculator) alike. There are no web results, no files and no suggestions.

## Install and start

AxStart is part of Axit. Installing Axit (see the Axit guide in the Axit menu, About) starts AxStart in the background and makes it start again each time you sign in to Windows. It has no window and no taskbar button until you press its hotkey, **Ctrl+Shift+Space**.

You can also start it from the Start menu, `Axit AxStart`, or the console, `axstart`: the search field comes up, and AxStart keeps running afterwards. Starting it again while it runs just brings up the field.

## Finding and starting a program

1. Press **Ctrl+Shift+Space** from any program. NVDA says "AxStart window, Search programs, edit".
2. Type part of the name, for example `calc`. After a short pause AxStart says the first match and how many there are: "Calculator, 1 of 2". "No programs match xyz" when nothing does.
3. **Down** and **Up** move to the next and the previous match, each said with its place: "Notepad++, 2 of 3". **Page Down** and **Page Up** move a page of matches, **Ctrl+Home** and **Ctrl+End** to the first and the last. At either end a low note sounds and nothing moves.
4. **Enter** starts the match you are on. AxStart goes away and the program comes up in front.

The caret never leaves the field: you can type more, Backspace, Delete, Left and Right at any moment, and the matches follow what is typed, the first one selected again.

How matching works: every word you type must be in the name, in any order, with case and accents ignored; `win power` finds "Windows PowerShell". Names that start with what you typed come first, then names with a word that starts with it, then the rest. Among those, the programs you start most often from AxStart come first.

## More keys in the field

- **Ctrl+Shift+Enter** starts the match as administrator; Windows asks first. A Store app cannot be started that way.
- **Ctrl+I** says the match, its kind and where its program file is: "Notepad++, desktop program, C:\Program Files\Notepad++\notepad++.exe" or "Calculator, Store app".
- **Ctrl+O** opens the program file's folder in AxFile, with the file selected.
- **F5** reads the list of programs again. AxStart also reads it each time it comes up, so a program installed a moment ago is there.
- **Escape** hides AxStart at once and puts you back where you were, whatever you had typed; the field is empty the next time. **Alt+F4** hides it too, and so does switching to another program.
- Pressing the hotkey while AxStart shows hides it again.

## Options

- **Options, Hotkey**: press the key you want to bring AxStart up, with Ctrl, Alt or the Windows key in it, then Enter. "Hotkey Ctrl+Alt+K" when it is set; "Ctrl+Alt+K is taken by another program" when another program holds it, and the old key stays. If the hotkey is taken when AxStart starts, AxStart shows itself once with a notice saying so, so that you can choose another.
- **Options, General, Start with Windows** (on): AxStart starts in the background when you sign in.
- **Options, General, Say the first match while typing** (on): turn it off and AxStart says nothing while you type; Ctrl+I says the match on demand.
- **Options, Text**: the font and its size, as in every Axit window.

## Ending AxStart

AxStart, **Quit AxStart** ends it until the next sign-in or the next time you start it. Hiding the window does not end it.

## Menus

- **AxStart**: Start (Enter), Start as administrator (Ctrl+Shift+Enter), Open file location (Ctrl+O), Information (Ctrl+I), Refresh programs (F5), Hide (Escape), Quit AxStart.
- **Options**: Hotkey, Text, General (Start with Windows, Say the first match while typing, Check for updates at start), Developer.
- **Axit**: the other Axit apps, the open windows, Check for updates and About, as in every Axit window.
- **Help**: Keyboard shortcuts (F1), User guide, Windows resources.

## Updates

AxStart offers a new Axit release as every Axit window does. An update stops AxStart and starts it again in the background.

## If something goes wrong

- **The hotkey does nothing**: AxStart may not be running. Start it from the Start menu, `Axit AxStart`. If the field comes up with a notice that the hotkey is taken, choose another in Options, Hotkey.
- **A program is missing**: AxStart lists what the Start menu's All apps list has. Press F5 to read it again.
- **A program will not start**: AxStart says why, in Windows' words, and stays open.
- The log is in `%LOCALAPPDATA%\Axit\logs\axstart.log`; it never holds the names of the programs you start.

## About

AxStart is an app of the Axit bundle by Dr. Kyle Keane (www.kylekeane.com), released under the MIT licence. Axit menu, About has the version, the licence and the full disclaimer.
