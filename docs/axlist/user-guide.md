# axList user guide

axList keeps lists of things to remember and to do, made for screen reader users. Each list is a plain Markdown file in a folder of lists, so you can also read and change it in axDown, any editor, a script or an AI system, and axList reads it the same way. Every change is written to the file at once: there is no Save.

## Install

axList is part of Axit. Installing Axit (see the Axit guide in the Axit menu, About) gives you a Start menu entry `Axit axList`, a console command `axlist`, and axList in the `Open with Axit` submenu when you right-click a `.md` file in File Explorer.

## Start

axList opens on the list you had last, or on the folder of lists. The folder is `Documents\Lists` until you choose another with Options, Lists folder. `axlist Shopping.md` opens that list with its folder as the folder of lists.

## Three views

- **Lists** (Ctrl+1): the lists of the folder, "Shopping, 4 open tasks". Enter opens one. Ctrl+N makes a new list after asking for its name.
- **Items** (Ctrl+2): the open list, one line per item: "Milk, task, not done", "Eggs, task, done", "1. Preheat the oven", "Butter", "Heading: Shopping". A nested item ends in "level 2". Escape or Backspace goes back to the lists.
- **Tasks** (Ctrl+3): every task not yet done, from every list: "Fix the tap, in Home". Space checks one off; Enter opens its list on it.

Pressing a view's key while it shows says its name again. Tab and Shift+Tab move to the next and the previous view. In any view, type the start of an item to jump to it; the same letter again goes to the next one, and Shift with a letter goes backwards. At the first or the last item, Up, Down, Home and End play a short low sound. F5 reads everything again, for a list you changed in another program.

## Changing a list

In the Items view:

- **Space** checks a task off or back on: "Done: Milk".
- **Enter** changes the item's text in a notice; Enter there writes it.
- **Ctrl+N** adds a new item after the one you are on, of the same kind: a task after a task, the next number after a numbered item.
- **Delete** removes the item and anything nested under it.
- **Alt+Up** and **Alt+Down** move the item, with what is nested under it, past the one before or after it. At the top or the bottom you hear a short low sound and "Top of the list".
- **Alt+Right** nests the item under the one before it; **Alt+Left** takes it out a level.
- **Ctrl+T** makes an item a task, or a task a plain item again.
- **Ctrl+Shift+D** hides the done tasks, and shows them again.
- **Ctrl+Z** undoes the last change, up to ten steps, as long as the window is open.
- **Ctrl+C** copies the item's text; **Ctrl+F** finds an item; F3 finds the next.

If another program changed the list since axList read it, axList reads it again and says so instead of changing the wrong line.

## The file

A list file is Markdown:

```
# Shopping
- [ ] Bread
- [x] Eggs
  - [ ] Free range
- Butter
1. Preheat the oven
```

A line with `- [ ]` is a task, `- [x]` a done task, `-` (or `*`, `+`) a plain item, `1.` a numbered item, `#` a heading; two spaces more nest an item under the one above. Any other line is kept as it is and shown as text. axList adds nothing hidden to the file.

## Menus

- **Folder**: Lists folder, Refresh (F5), Exit.
- **List**: Open (Enter), New list (Ctrl+N), Show in axFile (Ctrl+E), Open in axDown, Copy name (Ctrl+C).
- **Item**: Check or uncheck (Space), Change (Enter), New item (Ctrl+N), Make a task or not (Ctrl+T), Remove (Delete), Move up, Move down, Indent, Outdent, Undo (Ctrl+Z), Copy text (Ctrl+C), Show in axFile, Edit in axDown.
- **Task**: Check off (Space), Go to its list (Enter), Undo, Copy text.

Only the menu of the view you are in can be opened, so an item's commands are never at hand among the lists. Shift+F10, or the Applications key, opens it.
- **View**: Lists (Ctrl+1), Items (Ctrl+2), Tasks (Ctrl+3), Hide done tasks (Ctrl+Shift+D).
- **Options**: Text (font and size), General (the update check), Developer (the settings file, diagnostics).
- **Axit**: new windows of every app, the files and folders closed lately, the open windows, updates, About.
- **Help**: Keyboard shortcuts (F1), this guide, List resources.

To rename or delete a list, use axFile: Ctrl+E shows the list's file there.

## Settings

axList keeps the font, the text size, the update check, the window's place, the folder of lists, the list open last and whether done tasks are hidden in `%APPDATA%\Axit\axlist.json`. Options, Developer, Open settings file opens it in axJSON.

## Updates

axList checks for a newer Axit when it starts and offers it in a notice; the Axit menu has Check for updates at any time. One update brings every app of Axit up to date.

## If something goes wrong

axList's log is `%LOCALAPPDATA%\Axit\logs\axlist.log`; it never holds the text of your lists. Options, Developer, Copy diagnostics copies its end with the versions, for a bug report.

## About axList

axList is one app of Axit, by Dr. Kyle Keane (www.kylekeane.com), MIT licence. The Axit menu's About names the version and the licence.

## Disclaimer

axList writes to your list files as you work. Keep a copy of a list that matters, as you would of any file; Ctrl+Z undoes the last steps while the window is open.
