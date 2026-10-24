# Item apps: one shape for axTask and axPass

Written 2026-10-24 at the user's direction, before either app is built. axTask (tasks, `docs/axtask/SPEC.md`) and
axPass (passwords, `docs/axpass/SPEC.md`, with the format in `docs/axpass/kdbx-notes.md`) are both an *item app*: lists of items, each
item a set of typed fields. This document is the part they share, so that the two stay alike and the places they
differ are written down in one table (section 6). Requirements are numbered `IA-n.m` and decisions `IA-Dn`; each app's
spec cites them and does not repeat them. It replaces the Markdown design of axList (branch `axlist`, kept as a
record, never merged; IA-D1).

## 1. Summary

The window is axFile's, made smaller: a navigator on the left and a detail pane on the right. The navigator is lists
and items, moved through as axFile moves through folders and files. The detail pane shows what the navigator's
selection holds: an item's fields on an item, the list's own details on a list. Every field has a title, a type and a
value ("Due, date, 30 October 2026"), and each type has its own simple way to set it. The same fields are a small JSON
file that axJSON opens, so the app's editor is a convenience, never the only way in (axTask; axPass excepted, IA-6).

## 2. The window

- **IA-2.1 Two panes.** The navigator and the detail pane, each the bundle's `AxitList` (type-to-find, the edge
  earcon, AX-8.6), side by side; a status bar below. Nothing else takes the focus.
- **IA-2.2 The navigator.** One level at a time, as axFile: the top level is the lists (and the app's own entries
  above them, such as axTask's All tasks), a list's level is its sub-lists, then its items. A row reads its name and
  kind: "Home, list, 4 items", "Fix the tap, due 30 October" (the app's summary, IA-6). The list box is named after the
  level ("Lists", "Home") with its count.
- **IA-2.3 The detail pane.** Filled silently whenever the navigator's selection changes: on an item, its fields; on a
  list, the list's details (section 4.3). Nothing is spoken until the user goes there. It is named "Fields of Fix the
  tap" or "Details of Home".
- **IA-2.3a Sections.** A level may be in sections, each under a header row ("User lists, section, 2"); Ctrl+Up and
  Ctrl+Down go from header to header. A header is no list: Right and Enter on it play the edge sound.
- **IA-2.4 One announcement per move between levels**, axFile's words: "Home, 4 items, Fix the tap".

## 3. Keys

Window-specific keys, in one table per app (AX-8); the ones here are the same in every item app.

| Key | In the navigator | In the detail pane |
| --- | --- | --- |
| Up, Down, Home, End | move; the edge earcon at an end | move through the fields |
| Ctrl+Up, Ctrl+Down | the section before or after, on a level in sections (axTask's top level, TK-3a); the edge earcon past the ends | — |
| Right | on a list: open it. On an item: nothing, the edge earcon (axFile's Right on a file) | nothing |
| Left, Backspace | up a level | nothing |
| Enter | open: a list opens its items; an item moves to its fields, as Tab (IA-D2) | change the field (section 5) |
| Tab, Shift+Tab | to the detail pane | back to the navigator |
| Escape | — | back to the navigator |
| Ctrl+N | new item in this list | new field |
| Ctrl+Shift+N | new list here | — |
| F2 | rename in place (the Title field; bundle AX-8.7) | rename the field in place |
| Delete | delete the item or list (IA-D4) | remove the field (never Title) |
| Alt+Up, Alt+Down | — | move the field |
| Ctrl+Z | undo the last change | the same |
| Ctrl+Q | read the selection in full: every field, one after another | read the field in full |
| F5 | read the folder again | — |

- **IA-3.1** Entering the detail pane reads its name and the field: "Fields of Fix the tap, Due, date, 30 October
  2026". Leaving it says the navigator's selection.
- **IA-3.2** A level shows its sub-lists first, then its items, each by name in natural order, as axFile shows folders and files (the Archive last at the top, IA-D4). An item's fields are in the order the user gave them (Alt+Up and Alt+Down move one); nothing else sorts behind the user's back.

## 4. The format

### 4.1 An item

A JSON file holding a list of fields. The first field is the Title.

```json
{
  "format": "axit-item 1",
  "fields": [
    { "title": "Title", "type": "text", "value": "Fix the tap" },
    { "title": "Done", "type": "check", "value": false },
    { "title": "Start", "type": "date", "value": "2026-10-24" },
    { "title": "Due", "type": "date", "value": "2026-10-30" },
    { "title": "Priority", "type": "choice", "value": "High", "options": ["High", "Normal", "Low"] },
    { "title": "Notes", "type": "longtext", "value": "Washer under the sink." },
    { "title": "Waits on", "type": "link", "value": "../Shopping/Washer.json" }
  ]
}
```

- **IA-4.1** Keys the app does not know, in the file or in a field, are kept as they are when the file is written
  again. A field of a type the app does not know reads as text and is changed only in axJSON.
- **IA-4.2** An empty value is `""` (or `false` for a check); a field is never left out to mean empty.

### 4.2 The types

| Type | Value in the file | Read as | Set by (section 5) |
| --- | --- | --- | --- |
| `text` | a string, one line | the text | a text field |
| `longtext` | a string with line breaks | the first line and the count of the others, Ctrl+Q for all | a field of several lines: Enter makes a new line, Tab then Enter on Save keeps it |
| `number` | a JSON number | the number | a text field that takes only a number |
| `date` | `"YYYY-MM-DD"` | "30 October 2026, Friday" | a text field: a date, `today`, `tomorrow`, `+3` (days) |
| `datetime` | `"YYYY-MM-DDTHH:MM"` | "30 October 2026, 14:00" | the same, a time after the date |
| `check` | `true` or `false` | "done" or "not done" | Space toggles it, no field |
| `choice` | one of `options` | the option | a list of the options, the current one selected |
| `link` | a path to another item, relative to this file | the other item's title, or "missing: name" | the other item's title, typed |

axPass adds `username` and `password` (section 6).

### 4.3 A list

A folder. Its details are `list.json` in it, the same shape as an item, never shown in the navigator:

```json
{
  "format": "axit-list 1",
  "fields": [
    { "title": "Notes", "type": "longtext", "value": "" }
  ],
  "new item": [
    { "title": "Done", "type": "check", "value": false },
    { "title": "Due", "type": "date", "value": "" }
  ]
}
```

- **IA-4.3** `new item` is the list's template: a new item gets a Title and these fields. The detail pane on a list
  shows the list's fields, then one line "Fields of a new item: Done, Due" that Enter opens to change the template.
- **IA-4.4** A folder without `list.json` is a list with no details and an empty template (axTask fills its own
  default, its spec). The file is written the first time the user changes a detail.

### 4.4 File names

- **IA-4.5** An item's file is named after its Title, with the characters Windows forbids replaced by a space, and
  `.json`: `Fix the tap.json`. Two items with one title are `Fix the tap.json` and `Fix the tap - 2.json`, axFile's
  rule. Changing the Title renames the file, and the links to it are changed with it (IA-D3).
- **IA-4.6** A file that is not JSON, or not an item, is not shown; the status bar counts them ("2 files not shown").

## 5. Changing a field

- **IA-5.1** Enter on a field opens a notice in the window (bundle rule 2) titled with the field, its value in an edit
  field and selected (a date as 2026-10-30, a link as the other item's title). Enter keeps it, Escape leaves it as it
  was. A value the type refuses ("30 Febuary") says why and the notice stays.
- **IA-5.2** A check changes with Space or Enter, at once: "Done: done".
- **IA-5.3** New field (Ctrl+N in the pane): its title in a text field, then its type from a list of the types (a
  choice then asks its options, with a comma between them), added after the selected field.
- **IA-5.4** Every change is one announcement ("Due: 30 October 2026, Friday") and one step for Ctrl+Z.
- **IA-5.5** Before every write the file is read again; if it changed since it was shown (axJSON, a script, a sync),
  the change is refused with "The item changed on disk; it has been read again" (axList's LI-D5).

## 6. What differs between the apps

| | axTask | axPass |
| --- | --- | --- |
| Where the items are | a folder of JSON files; a list is a folder (IA-4) | one encrypted KeePass file (`.kdbx`), through KeePassXC's command-line tool; a list is a KeePass group |
| The JSON shape | the file on disk | the same shape, in memory only; never written to disk |
| Open in axJSON | yes, Ctrl+J on an item or a list | no: a password in a plain file defeats the encryption |
| Extra field types | none | `username`, `password` (read as "password, hidden"; Ctrl+Q never reads it) |
| Extra navigator entries | All tasks, the smart lists (saved filters) | none; search by title (Ctrl+F) |
| Row summary | the Title, "done" and the Due date | the Title and the user name |
| Space in the navigator | marks the item done or not done | nothing |
| Its own keys | (its spec) | copy user name, copy password with the clipboard cleared after 30 seconds, generate a password, lock |
| Opens with | a folder | a `.kdbx` file and the master password, asked in a notice whose field hides what is typed |
| New field | any type (IA-5.3) | not in the first version: KeePass's standard fields, custom attributes read only (PW-D3) |
| Templates | `list.json`'s `new item` | KeePass's standard fields (Title, UserName, Password, URL, Notes) plus the user's own |
| Delete | to the top folder's Archive list; Ctrl+Z brings it back | to KeePass's own Recycle Bin group |

Everything not in this table is the same in both apps, and a change to one of those things changes this document and
both apps in the same commit.

## 7. Decisions

- **IA-D1 One item per JSON file, not Markdown lists** (the user, 2026-10-24). axList kept lists as Markdown, and the
  user found it more than was needed and too close to axFile and axDown. Typed fields (a start and a due date as
  dates) need a format that says each field's type; JSON says it, axJSON already edits it, and an AI system or a
  script reads it as easily. One file per item keeps the navigator the same shape as axFile's and lets axJSON open one
  item at a time (the user's choice over one file per list).
- **IA-D2 Enter opens** (the user, 2026-10-24). In axFile, Enter on a folder opens it and Enter on a file opens the
  file; here Enter on a list opens it and Enter on an item opens its fields. The worry on record: Enter does two
  things. It is the same word, open, in both places, and the announcement says which happened ("Fields of Fix the
  tap" against "Home, 4 items"). Right stays the key that only goes into lists.
- **IA-D3 Links are paths** relative to the item's file, so that a folder moved or synced whole keeps them. The app
  changes them when it renames or moves an item; a link changed by another program to a file that is not there reads
  "Waits on, link, missing: Washer".
- **IA-D4 Delete archives.** Delete moves an item or a list into the app's archive: for axTask the list `Archive` in
  the top folder (made by the first Delete, shown last at the top, left out of All tasks and the smart lists), for
  axPass KeePass's Recycle Bin group. Ctrl+Z brings it back; in the archive, Delete says that axFile (Ctrl+E) deletes
  for good. A task done stays where it is, marked done, until the user archives it (the user's thought of
  2026-10-24: "delete could just remove them and add them into an archive"). The Recycle Bin was the first draft;
  the archive is a list like the others, read by ear and by axFile, with no shell call behind it.
- **IA-D5 Built once, shared on the second use** (bundle rule 4). axTask is built first with the window and the format
  in its own projects. When axPass is built, the window, the field editors and the format move unchanged to
  `Axit.Core` and `Axit.Forms`, one piece per commit, and the two apps keep only what section 6 lists.

## 8. Open questions

- A field in the detail pane that the app needs (axTask's Done and Due) may be removed by the user; the app then treats
  the item as having none. Whether such fields should be protected is left until it is heard in use.
- Whether a list's sub-lists come before or after its items in the navigator (axFile puts folders first; the
  first build does the same).
