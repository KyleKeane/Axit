# axList NVDA test plan

Run with NVDA before a release, after `run-axlist.ps1 <folder>` on a scratch folder (or `.\run.ps1 -App list <folder>`), never on the folder of your real lists. Make two files in it first: `Shopping.md` with the lines `# Shopping`, `- [ ] Milk`, `- [x] Eggs`, `  - [ ] Free range`, `- Butter`, `1. One`, `2. Two`; and `Home.md` with `- [ ] Fix the tap`. Every step says what NVDA should read; a step that reads differently is a defect. The specification is `docs/axlist/SPEC.md` (LI-n.m).

## 1. Start and the views (LI-1, LI-2, LI-3)

1. Start axList on the folder: "Lists - axList window", then "Lists, 2 list, Home, 1 open task, 1 of 2". Down: "Shopping, 2 open tasks".
2. Enter on Shopping: "Shopping, 6 items. Heading: Shopping" and the title is "Shopping - axList". Down: "Milk, task, not done", then "Eggs, task, done", "Free range, task, not done, level 2", "Butter", "1. One".
3. Escape: "Lists" with Shopping selected. Ctrl+2: back on the item you had. Backspace: the Lists view again.
4. Ctrl+3: "Tasks, 3 open. Fix the tap, in Home". Ctrl+3 again: the same words.
5. NVDA+End: the status bar: the view's name and the folder.
6. Ctrl+S: "Nothing to save here: every change is written at once".

## 2. Changing items (LI-4)

1. In Shopping on Milk, Space: "Done: Milk"; Space: "Not done: Milk". Open Shopping.md in axDown (List, Open in axDown): the line reads `- [ ] Milk`.
2. Enter on Milk: the notice "Change item" with the field "Text" holding Milk; type `Oat milk`, Enter: "Changed Oat milk".
3. Ctrl+N on Oat milk: "New item"; type `Bread`, Enter: "Added Bread", selected, read "Bread, task, not done".
4. Alt+Down on Bread: "Moved down: Bread" (past Eggs and Free range). Alt+Down until "Bottom of the list" with the low sound. Alt+Up back.
5. Alt+Right on Bread: "Indented: Bread, level 2". Alt+Left: "Outdented: Bread".
6. Ctrl+T on Butter: "Butter is a task"; again: "Butter is not a task". On the heading: "Only list items become tasks".
7. Ctrl+N on "1. One", type `Half`, Enter: "Added Half"; the items read "1. One", "2. Half", "3. Two".
8. Delete on Eggs: "Removed Eggs and 1 nested item". Ctrl+Z: "Undone: Removed Eggs and 1 nested item"; Eggs and Free range are back.
9. Ctrl+Shift+D: "Done tasks hidden"; the list's name says "done hidden". Ctrl+Shift+D: "Done tasks shown".
10. In axDown, change Bread to Rye in Shopping.md and save; in axList, Space on Bread: "The list changed on disk; it has been read again", and the item reads Rye. F5: "Refreshed: …".
11. Ctrl+C on an item: "Copied"; paste in axDown: the text. Ctrl+F, type `rye`, Enter: the item selected and read.

## 3. Tasks and lists (LI-3.2, LI-5)

1. Ctrl+3, on "Fix the tap, in Home", Space: "Done: Fix the tap"; the task leaves the list and the next one is selected. Ctrl+Z: "Undone: Done: Fix the tap".
2. Enter on a task: the list that holds it opens on that task.
3. Ctrl+1, Ctrl+N: "New list"; type `Ideas`, Enter: "Made Ideas", the Items view of the new list ("Ideas, 1 item. Heading: Ideas"). Ctrl+1, Ctrl+N, type `Shopping`, Enter: "There is a list called Shopping already" and the notice stays.
4. Ctrl+E on a list: axFile opens on its file.
5. Options, Lists folder: the Windows folder picker; choose another folder: "Folder <name>. Lists, …".

## 4. Text, notices, the Axit menu (LI-6)

1. Ctrl+Plus: "Text size 13" (or the next size); Ctrl+Minus back.
2. F1: the Keyboard shortcuts notice; Escape. Help, User guide: this guide; Escape.
3. Alt+A: the Axit menu; About, Version names axList among the apps.
4. Close axList and start it without a folder: it opens on the list you had, in the folder you had.
