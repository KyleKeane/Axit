# axTask user guide

axTask keeps tasks and lists as a folder of small files, one file per item, and shows them the way axFile shows a
folder. It is part of Axit, a bundle of apps for people who use a screen reader; it was built for and with NVDA.

## Start

- Start menu: `Axit axTask`. Console: `axtask`, or `axtask C:\my\tasks` for a folder of your own.
- The first time, axTask uses the folder `Tasks` in your Documents folder. Folder, Top folder chooses another.
- axTask opens on the list you had open last.

## The window

Two panes side by side, and a status bar.

- **The navigator**, on the left, where the focus starts. Its top level is in sections, each under a header line:
  **Smart lists** (All tasks and your saved filters), **User lists**, your own sections, and **Archived lists**.
  Ctrl+Up and Ctrl+Down go to the section before or after. Up and Down move; type the start of a name to jump to it.
- **The detail pane**, on the right. It shows what the navigator's selection holds, without saying anything until
  you go there with Tab:
  - on an item, its fields: "Due, date, 30 October 2026, Friday";
  - on a list, the list's own details, and a line naming the fields a new item gets;
  - on a smart list, what it shows.

## Moving around

- Right goes into a list. Left or Backspace goes back to the sections, with the list you came from selected. A list
  holds items, not other lists: your own sections group the lists instead.
- Right on an item does nothing; you hear the edge sound, as in axFile on a file.
- Enter opens: on a list it shows the list's items; on an item it moves you to the item's fields, as Tab does.
- Tab goes to the detail pane. Escape, Tab or Shift+Tab come back to the navigator.
- Ctrl+1 goes to All tasks from anywhere.
- At either end of a pane, Up or Down plays the edge sound.

## Sections of your own

- On a list, Ctrl+M (List, Move to section) chooses its section: User lists, one of your sections, or a new one.
- The same is the Section field, first in a list's details: Tab on the list, Enter on Section, type the name.
- A new list (Ctrl+Shift+N) joins the section of the line selected.
- A section exists while a list is in it; to rename one, move its lists.

## Tasks and plain lists

- A task is an item with a **Done** check. Space marks it done or not done, in the navigator or on the Done field.
- A list without Done is a plain list, for things that are not tasks. List, Add Done to every item makes a plain list
  a checklist; List, Remove Done from every item makes it plain again.
- A row reads its title and, for a task, "done", "due 30 October" or "overdue since 22 October".
- Ctrl+Shift+D hides the done tasks of a list, and shows them again.

## Changing things

In the navigator:

- Ctrl+N adds an item to the list shown. It gets the fields the list gives new items: Done, Start and Due unless
  you choose others.
- Ctrl+Shift+N makes a new list.
- F2 renames the item or the list in place: a box over the line holds the name; type the new one and press Enter,
  or Escape to keep the old. Links to it from other items are kept.
- Delete moves the item or the list into the Archive: an archived list is under **Archived lists**, archived items
  under its line "Archived items". Ctrl+Z brings it back.
  To delete something for good, show it in axFile with Ctrl+E and delete it there.

In the detail pane:

- Enter changes the field. A notice opens with the value:
  - a date takes 2026-10-30, 30 October 2026, today, tomorrow, +3 for three days on, or a weekday such as friday;
  - a date and time takes a date, a space and a time: tomorrow 14:00;
  - a choice shows a list of its options; Up and Down move, Enter chooses;
  - a long text is a field of several lines: Enter makes a new line, Tab and then Enter on Save keeps it;
  - a link takes the title of the item it points to, in any list.
- Ctrl+N adds a field after the selected one: its title, then its type from a list; a choice then asks its options.
- F2 renames the field in place, Delete removes it, Alt+Up and Alt+Down move it. The Title field stays.
- Item, Use these fields for new items gives the list's new items the fields of the item selected.

Every change is written to the file at once, and Ctrl+Z takes back the last ones, one at a time. If another program
changed the file since axTask showed it, axTask reads it again and says so instead of writing over it.

## Smart lists

All tasks is every task not done, in every list but the Archive, the soonest due first. List, New smart list makes
one of your own: a small file whose conditions start as All tasks' and open in axJSON to change. For example, the
tasks of the Work list due within a week:

```json
{
  "format": "axit-filter 1",
  "match": "all",
  "conditions": [
    { "field": "Done", "is": false },
    { "list": "Work" },
    { "field": "Due", "before": "+7" }
  ],
  "order": [ "Due", "Title" ]
}
```

A condition is one of: a field `is` a value; a date field `before` or `after` a date or a count of days from today
(the day itself included); a field `empty` or not; the item in a `list` or the lists inside it; a text field `has` a
word. `match` is `all` or `any`.

## The files

Each item is a JSON file named after its title, a list of fields each with a title, a type and a value:

```json
{
  "format": "axit-item 1",
  "fields": [
    { "title": "Title", "type": "text", "value": "Fix the tap" },
    { "title": "Done", "type": "check", "value": false },
    { "title": "Due", "type": "date", "value": "2026-10-30" }
  ]
}
```

A list is a folder in the top folder; its details are `list.json` in it, its section among them. Ctrl+J opens the item, the list's details or a smart list in
axJSON; Ctrl+E shows it in axFile. Anything axTask does not know in a file is kept as it is.

## Keys

F1 lists every key. Shift+F10 or the Applications key opens the menu of the selection.

## If something goes wrong

- "The item changed on disk; it has been read again": another program changed the file. Look at it again and repeat
  the change.
- A file that is not an item is not shown; the list's name counts such files.
- Options, Developer, Copy diagnostics puts what is needed for a bug report on the clipboard.

## About

axTask is part of Axit by Dr. Kyle Keane, www.kylekeane.com. Axit, About has the licence and the version.

## Disclaimer

axTask is provided as it is, without warranty of any kind. Keep a copy of files that matter.
