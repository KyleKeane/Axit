# axGIT NVDA test plan

The files for this plan are in `manual_tests/axgit`, made and reset by `.\manual_tests\prepare.ps1 axgit`; its `_main.md` says which folder or file each section uses.

Run with NVDA before a release, on a repository with a remote (the Axit repository itself does), after `run-axgit.ps1 C:\path\to\repo` (or `.\run.ps1 -App git <repo>`). Make a scratch change first: edit a file, create a new one. Every step says what NVDA should read; a step that reads differently is a defect. The specification is `docs/axgit/SPEC.md` (AG-n.m).

## 1. Start (AG-1, AG-2)

1. Start axGIT on the repository: NVDA reads "<folder name> - axGIT window", then "Changes, 2 files list, Modified <file>, unstaged, 1 of 2" (or similar). Nothing else is spoken.
2. Down Arrow: the next item, nothing else. Tab: "Details, edit, read only", empty. Shift+Tab: the list, the same item as before. F6: the details again; Ctrl+Tab: the list.
3. NVDA+End: the status bar: "main · no upstream · 2 changed, 0 staged" in a repository without a remote (or the branch's state: "up to date", "2 ahead"), then the path.
4. Ctrl+1 while the Changes list shows: "Changes, 2 files". Ctrl+2: "Log, 200 commits list, <hash> <date> <author>: <subject>, 1 of 200". Ctrl+3: "Branches, N list, <branch>, current, …". Ctrl+1: back to Changes on the item you had.
4a. In the Log view (Ctrl+2), Space: "Changes view first: Ctrl+1"; Delete: the same; Alt+C, D (Show diff): the same; the view stays. Ctrl+1, then Alt+B, S (Switch to branch): "Branches view first: Ctrl+3".
5. Start axGIT on a folder that is not a repository: "Not a git repository" notice naming the folder; Enter closes it; the title is "No repository - axGIT"; Ctrl+O opens the folder picker. With a repository open, Ctrl+O on a folder outside any repository: the same notice, and the repository stays open, its title unchanged.
6. Alt+F4 closes the window without a question.

## 2. From and to the other windows (AG-1.4)

1. In axClaude on the repository, Alt+S, I: "Show current folder in submenu", Down: "File Explorer Ctrl+W", Down: "axGIT"; Enter: a new axGIT window on the repository, focused.
2. Alt+A, O: the open windows, the axClaude window first; Enter goes back to it. Alt+A, O again, the axGIT window first now.
3. In axDown on a file of the repository, Alt+F, I, G: axGIT on the repository that holds the file.
4. In axGIT, Alt+R, I: "File Explorer Ctrl+W" and "axClaude"; Enter on axClaude: a new axClaude on the repository.
5. In the Changes list on a `.md` file, Ctrl+E: axDown opens it; on a `.json` file, axJSON.

## 3. Changes (AG-3)

1. On an unstaged file, Space: "Staged <path>"; Up or Down and back: the item reads "…, staged". Space again: "Unstaged <path>". NVDA+End: the staged count changed.
2. Enter on a changed file: "Diff of <path>, N lines" and the focus in the details on its first line ("diff --git …"); Down Arrow reads the lines; a line with a plus is an addition. Ctrl+Tab: the list, the same item.
3. Enter on the untracked file: the details hold "Untracked file, N lines" and the file's text.
4. Ctrl+Shift+A: "Staged all"; every item reads "…, staged". Ctrl+Shift+U: "Unstaged all".
5. Ctrl+Enter with nothing staged: "Nothing staged. Space stages a file", no notice. Space on a file, Ctrl+Enter: the notice "Commit 1 staged file" with the field "Message"; Enter on the empty field: the notice stays and says "Type the commit message"; Space then Ctrl+Enter at once: "Working…" while the stage runs; type "Test commit", Enter: "Committed <hash>"; the Changes list no longer has the file; Ctrl+2: the commit is first in the log.
6. Edit a tracked file, F5: "Refreshed: Changes, 1 file"; on it, Delete: the notice "Discard the changes to <path>?" with Discard and Cancel; Escape: nothing happens; Delete, Enter: "Discarded <path>", the item is gone. On the untracked file, Delete: "Untracked file: delete it in File Explorer".
7. Ctrl+S anywhere: "Nothing to save here. Space stages, Ctrl+Enter commits".

## 4. Log and branches (AG-4, AG-5)

1. Ctrl+2, Enter on the first commit: "Commit <hash>, N lines", the focus in the details on "commit <full hash>"; Down Arrow reads the author, the date, the message and the files. Ctrl+Tab: the list.
2. Ctrl+3: the branches; the current one reads "…, current". Enter on it: "Already on <name>". Enter on another: "Switched to <name>"; NVDA+End: the status bar names it; Ctrl+1: the Changes of that branch. Switch back.
3. Ctrl+N: the notice "New branch" with the field "Name"; type "scratch-test", Enter: "Switched to scratch-test"; Ctrl+3 lists it as current. Switch back to the branch you had (Enter on it) and delete the test branch in a terminal (`git branch -d scratch-test`).
4. Ctrl+Shift+F: the status bar says "Working: fetch…" (NVDA+End), then "Fetched". Ctrl+Shift+L: "Pulled" or "Already up to date". Ctrl+Shift+H on a branch with nothing to push: "Pushed" or git's "Everything up-to-date" read as "Pushed". With the network off, Ctrl+Shift+F: the notice "Fetch failed" with git's message.
5. Press Ctrl+Shift+F and at once Ctrl+Shift+L: "Working…" for the second; the first finishes on its own.

## 5. Text, notices, the Axit menu (AG-2.4, AG-6)

1. In the details after a diff, Ctrl+F: the Find notice; type a word from the diff, Enter: the match selected in the details and read; F3: the next, and after the last match the first again; a word with no match: "Not found". Ctrl+Home, Page Down: a screen down; Ctrl+Down: the next paragraph.
2. Ctrl+Plus: "Text size 13" (or the next size); Ctrl+Minus back. Options, Text, Windows text size: "Windows text size".
3. F1: the Keyboard shortcuts notice; Escape closes it. Help, User guide: this guide; Escape.
4. Alt+A: the Axit menu with New window, Recently closed, Open windows, Open windows by type, Close all windows, Check for updates, About. About, Version: "Axit <version>" and axGIT named among the apps.
5. Options, Developer, Copy diagnostics: "Copied diagnostics"; paste somewhere: the versions, the repository and the log's end.
6. Close the window and start axGIT without a folder: it opens on the same repository (the last one); Repository, Open recent repository lists it as "1. <name> (<path>)".
