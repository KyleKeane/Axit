# axClip user guide

axClip keeps a history of the text you copy, made for screen reader users. It runs in the background; one key from anywhere, Windows+Alt+V unless you choose another, brings up a search field over everything you copied, the newest first. Enter pastes the item into the program you were in.

## Install

axClip is part of Axit. Installing Axit (see the Axit guide in the Axit menu, About) gives you a Start menu entry `Axit axClip` and a console command `axclip`, and starts axClip in the background; it then starts with Windows, unless you turn that off in its Options, General.

## Using it

1. Copy text anywhere, as you always do (Ctrl+C). axClip keeps it quietly; nothing is said.
2. Press Windows+Alt+V. axClip comes up with the caret in its search field and the newest item selected.
3. Down and Up move through the items; each is read with its place, "git status, 2 of 12". Ctrl+Home and Ctrl+End go to the first and the last, Page Down and Page Up a screen at a time.
4. Type part of what you want: only the items with every word you typed stay, and after a short pause the first is read.
5. **Enter** pastes the item into the program you were in. axClip hides, that program comes back to the front, and the text is pasted.

Other keys while axClip shows:

- **Shift+Enter** puts the item on the clipboard without pasting, for a program whose paste is another key.
- **Ctrl+Q** shows the item's whole text in a notice, to read with the arrow keys. Escape closes it.
- **Delete** removes the item; **Ctrl+Shift+Delete** clears the whole history, after a question.
- **Ctrl+P** pauses recording, for when you are about to copy something private; press it again to record again.
- **Escape**, or the hotkey again, hides axClip.

An item reads as its first line, cut at 120 characters, and says how many lines it has when there are more.

## Privacy

The history is kept in memory only: it is gone when axClip ends, unless you turn on Options, Keep history between runs, which keeps it in axClip's settings file. Password managers that mark a copy as private (most do) are respected: such a copy is never kept. axClip keeps only text, never pictures or files, and its log never holds what you copied.

## Menus

- **axClip**: Paste (Enter), Copy without pasting (Shift+Enter), Read in full (Ctrl+Q), Remove (Delete), Clear history (Ctrl+Shift+Delete), Hide (Escape), Quit axClip.
- **Options**: Hotkey, Pause recording (Ctrl+P), Keep history between runs, Text (font and size), General (the update check, Start with Windows, Say the first match while typing), Developer (the settings file, diagnostics).
- **Axit**: new windows of every app, the files and folders closed lately, the open windows, updates, About.
- **Help**: Keyboard shortcuts (F1), this guide, Windows resources.

## The hotkey

Options, Hotkey: press the new key with Ctrl, Alt or the Windows key, then Enter. When another program already holds the key, axClip says so and keeps the one before.

## Settings

axClip keeps the font, the text size, the update check, the hotkey, Start with Windows, Say the first match while typing and Keep history between runs in `%APPDATA%\Axit\axclip.json`, and the history there too while you keep it. Options, Developer, Open settings file opens it in axJSON.

## Updates

axClip checks for a newer Axit when it starts and offers it in a notice; the Axit menu has Check for updates at any time. One update brings every app of Axit up to date; axClip is started again after it, and its history starts empty unless you keep it.

## If something goes wrong

axClip's log is `%LOCALAPPDATA%\Axit\logs\axclip.log`. If Enter does not paste, the program you were in did not come back to the front in time, or it runs as administrator, where Windows does not let axClip press keys: the text is on the clipboard, so press Ctrl+V there yourself.

## About axClip

axClip is one app of Axit, by Dr. Kyle Keane (www.kylekeane.com), MIT licence. The Axit menu's About names the version and the licence.

## Disclaimer

A clipboard history holds whatever you copy. Pause recording before you copy something private that no password manager marks, and leave Keep history between runs off if others use this computer.
