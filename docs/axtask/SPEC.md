# axTask — Specification

axTask is an app of the Axit bundle (`docs/axit/SPEC.md`): tasks and lists as a folder of small JSON files, for a
blind developer who uses the NVDA screen reader. It is an item app: its window, keys, field types and file format are
the shared ones of `docs/axit/item-apps.md` (cited as IA-n.m and not repeated), and this document holds only what is
axTask's own. Written 2026-10-24 and built the same day on the branch `axtask`, not yet heard under NVDA; it takes the place of axList (`docs/axlist/SPEC.md`, branch
`axlist`, never merged). Requirements are numbered `TK-n.m` and decisions `TK-Dn`. In axTask's code and documents,
"SPEC.md" means this file.

## 1. Summary

A folder of lists: `Home\`, `Work\`, `Work\Course plan\`, each item a file such as `Home\Fix the tap.json`. axTask
shows them as axFile shows a folder (Right into a list, Left back up), with the item's fields in the detail pane
(Tab, or Enter on an item). Above the lists, **All tasks** gathers every task not done from every list, by due date.
Space marks a task done. Saved filters, the smart lists, are small files of their own and appear beside All tasks.

## 2. Goals and non-goals

- **G1 Tasks with dates.** Start and due dates as dates, done as a check, notes, priority, links to other items.
- **G2 One view of everything to do.** All tasks and the smart lists, read from the files every time.
- **G3 The files are the truth.** Every item can be opened in axJSON (Ctrl+J), changed by a script or an AI system,
  and synced by whatever syncs the user's files (bundle B-15).
- **G4 As simple as axFile.** The shape of IA-2, nothing more in the first version.
- **Non-goals for the first version.** Reminders that sound at a time, rules that act on their own (TK-6), recurring
  tasks, sharing. The format leaves room for them.

## 3. Scenarios

1. Start axTask on the folder: "Lists - axTask window", then "Lists, 3 items list, All tasks, 5 not done, 1 of 4".
   Down: "Home, list, 2 items".
2. Right: "Home, 2 items, Fix the tap, due 30 October". Tab: "Fields of Fix the tap, Title, text, Fix the tap". Down:
   "Done, check, not done", then "Due, date, 30 October 2026, Friday".
3. Enter on Due, type `+3`, Enter: "Due: 2 November 2026, Monday". Shift+Tab: "Fix the tap, due 2 November".
4. Space: "Done: Fix the tap". Ctrl+Z: "Undone: Done: Fix the tap".
5. Ctrl+N, type `Call the plumber`, Enter: "Added Call the plumber", with the list's new-item fields (Done, Due).
6. Left: "Lists, Home". Up: "All tasks, 5 not done". Right: the tasks of every list, the soonest due first, each read
   "Call the plumber, in Home".

## 4. axTask's part of the shared shape

- **TK-4.1 The default template.** A list with no `list.json` gives a new item the fields Done (`check`), Start
  (`date`) and Due (`date`), empty.
- **TK-4.2 A task** is an item with a `check` field titled Done. An item without one is a plain item: a list of
  things that are not tasks is a list whose template has no Done (the user's ask: plain lists and checklists, and an
  easy way between them). Changing a list's template does not change its items; "Add Done to every item" and "Remove
  Done from every item" in the List menu switch a whole list between the two.
- **TK-4.3 The row summary** (IA-6): the Title, then "done" when Done is true, then "due" and the Due date when there is
  one: "Fix the tap, done, due 30 October". Overdue reads "overdue since 30 October".
- **TK-4.4 Space in the navigator** toggles Done on a task; on a plain item, the edge earcon and "Not a task".

## 5. All tasks and the smart lists

- **TK-5.1 All tasks** is the first entry of the top level, always there and never a folder: every task under the
  folder whose Done is false, the soonest Due first, tasks without a due date after them in title order. A row adds
  "in Home". Enter or Tab opens the task's fields as anywhere else; a change is written to the task's own file.
- **TK-5.2 A smart list** is a file `<name>.filter.json` in the top folder, shown after All tasks as "Due this week,
  smart list, 3". Its content is the conditions every shown item meets:

```json
{
  "format": "axit-filter 1",
  "match": "all",
  "conditions": [
    { "field": "Done", "is": false },
    { "field": "Due", "before": "+7" },
    { "list": "Work" }
  ],
  "order": [ "Due", "Title" ]
}
```

  The conditions in the first version: a field equals a value (`is`), a date before or after a date or a day count
  from today, the day itself included (`before`, `after`), a field is empty or not (`empty`), the item is in a list or its sub-lists (`list`), a
  text field holds a word (`has`). `match` is `all` or `any`. A condition the app does not know shows the smart list
  as "Due this week, smart list, a condition not understood" and nothing in it.
- **TK-5.3 A new smart list** (List, New smart list) asks its name, writes `<name>.filter.json` with All tasks'
  conditions and opens it in axJSON to change them; Ctrl+J on a smart list opens it again. The detail pane of a smart
  list says what it shows, how many, and its file; the conditions are written in axJSON, not field by field, until
  the need is heard (a filter is written once and read many times).

## 6. Later: rules (automation)

Recorded so the format has room; not built in the first version. A rule is a file `<name>.rule.json` in the top
folder: **when** (a date field reaches a day, an item is marked done, an item is added to a list), **if** (the same
conditions as a smart list), **then** (set a field, move the item to a list, make a new item from a template, sound a
reminder through the bundle's notices). Rules run only while axTask runs, or from axStart's background process when
reminders come; that choice waits for the build.

## 7. axTask's own keys

On top of IA-3:

| Key | Where | What |
| --- | --- | --- |
| Space | navigator | done or not done (TK-4.4) |
| Ctrl+J | anywhere | open the item, the list's `list.json` or the smart list in axJSON |
| Ctrl+E | anywhere | show the file or folder in axFile |
| Ctrl+Shift+D | navigator | hide or show done tasks in this list |
| Ctrl+1 | anywhere | to All tasks |

## 8. Decisions

- **TK-D1 The All tasks entry is not a file.** It is the one smart list nobody can delete or break; every other one is
  a file the user owns.
- **TK-D2 Done is a field by its title**, "Done", not a special key in the format: the format stays the shared one, and
  a script sets it as any other field. The cost: a user who renames Done turns tasks into plain items; the Fields
  of a new item line says so when the template has no Done.
- **TK-D3 No index or cache.** All tasks and the smart lists read every file under the folder each time they open. A
  few thousand small files read in well under a second; an index waits until a real folder shows the need, and then
  it is a plain file too.

## 9. Open questions

- Reminders (TK-6) need a process that runs when the window is closed. axStart's is the candidate, which would make
  axTask's rules a second use of the background process.
- How a link is set (IA-4.2's search over the titles) across lists, and whether All tasks shows the items that wait
  on others.
