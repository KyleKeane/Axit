# AxGit user guide

AxGit is a git manager for one repository, made for screen reader users. One window shows one repository: a list of its changed files, its log or its branches, a text with the details of what you pick, and a status bar with the branch. Everything it does, it does with the git program you already have, so its messages are git's own.

## Install

AxGit is part of Axit. Installing Axit (see the Axit guide in the Axit menu, About) gives you a Start menu entry `Axit AxGit`, a console command `axgit`, and `Open in AxGit` when you right-click a folder in File Explorer. You also need git itself, from https://git-scm.com; if it is missing, AxGit says so when it starts.

## Start

- From AxClaude: Session, Show current folder in, AxGit (Alt+S, I, G). AxGit opens on the repository that holds the project folder.
- From AxDown or AxJSON: File, Show current file in, AxGit (Alt+F, I, G). AxGit opens on the repository that holds the file.
- From the Start menu, `Axit AxGit`, or the console: `axgit C:\my\project`. Without a folder, AxGit opens the last repository you had, or asks for one with Repository, Open repository (Ctrl+O).
- To go back: Alt+A, O lists the open Axit windows, most recent first, so Alt+A, O, 1 is the window you came from. From AxGit, Repository, Show current repository in, AxClaude opens a new AxClaude on the repository, and File Explorer (Ctrl+W) shows the folder.

The window is named after the repository's folder: "myproject - AxGit".

## The list and its views

The list at the top has three views, each named with what it holds:

- **Changes** (Ctrl+1): one item per changed file, "Modified src/Foo.cs, unstaged". The last words say whether the file is staged for the next commit, unstaged, or both; a new file not yet in git reads "Untracked", a file in conflict after a merge reads "Conflict".
- **Log** (Ctrl+2): the last 200 commits, "3a1f2c0 2026-10-18 Kyle: Release 2.11.0".
- **Branches** (Ctrl+3): your branches, "main, current, origin/main, 1 ahead".

Arrow keys move through the items and nothing happens until you press a key. Pressing a view's key while the view already shows says its name again, so you always know where you are. F5 refreshes the view after you changed something outside AxGit.

Under the list is the details text, which Tab, Ctrl+Tab or F6 reach. It holds the last thing you asked to see: a diff, a commit, or what git answered. Arrow keys read it line by line; Ctrl+F finds text in it; Page Up and Page Down move a screen; Ctrl+Up and Ctrl+Down move by paragraph.

## Changes

- **Space** stages the file you are on, or unstages it when it is staged; AxGit says "Staged src/Foo.cs". Stage all (Ctrl+Shift+A) and Unstage all (Ctrl+Shift+U) do it for every file.
- **Enter** shows the file's diff in the details and puts you on its first line, so Down Arrow reads it. Lines starting with a plus were added, with a minus removed. Ctrl+Tab goes back to the list. For a new file, the details show the file itself.
- **Ctrl+E** opens the file in AxDown, or AxJSON for a `.json` file.
- **Delete** discards the changes to a file after a question; they cannot be brought back. A new, untracked file is not deleted from AxGit.
- **Ctrl+Enter** commits what is staged: a notice asks for the message, Enter commits, and AxGit says "Committed" with the commit's short hash. With nothing staged it says so instead.

## Log

Enter on a commit shows its message and the list of files it changed, in the details.

## Branches

Enter on a branch switches to it. Branches, New branch (Ctrl+N) asks for a name and switches to the new branch. Fetch (Ctrl+Shift+F), Pull (Ctrl+Shift+L) and Push (Ctrl+Shift+P) talk to the remote; AxGit says "Fetched", "Pulled" or "Pushed", and when git refuses, its whole answer comes as a notice you can read. The status bar says "Working" while one of them runs; the window stays usable.

AxGit does not merge, rebase or resolve conflicts. For those, use the terminal or ask Claude in AxClaude; AxGit shows you the result.

## Menus

- **Repository**: Open repository (Ctrl+O), Open recent repository, Refresh (F5), Show current repository in (File Explorer Ctrl+W, AxClaude), Exit.
- **Changes**: Stage or unstage (Space), Stage all, Unstage all, Show diff (Enter), Open file in editor (Ctrl+E), Discard changes (Delete), Commit (Ctrl+Enter).
- **Branches**: Switch to branch (Enter), New branch (Ctrl+N), Fetch, Pull, Push.
- **View**: Changes, Log, Branches.
- **Options**: Text (font and size), General (the update check), Developer (the settings file, diagnostics).
- **Axit**: new windows of every app, the files and folders closed lately, the open windows, updates, About.
- **Help**: Keyboard shortcuts (F1), this guide, Git resources.

## Settings

Everything you change is kept in `%APPDATA%\Axit\axgit.json`: the font and text size, the update check and the recent repositories. Options, Developer, Open settings file opens it in AxJSON.

## Updates

AxGit checks for a newer Axit when it starts and offers it in a notice; the Axit menu has Check for updates at any time. One update brings every app of Axit up to date.

## If something goes wrong

AxGit's log is `%LOCALAPPDATA%\Axit\logs\axgit.log`; Options, Developer, Copy diagnostics copies its end with the versions, for a bug report. "Git was not found" means git is not on your PATH: install it from https://git-scm.com and start AxGit again. "Not a git repository" means the folder you gave is not inside one; Open repository (Ctrl+O) takes another.

## About AxGit

AxGit is one app of Axit, by Dr. Kyle Keane (www.kylekeane.com), MIT licence. The Axit menu's About names the version and the licence.

## Disclaimer

AxGit runs git for you and shows what it answers; a command that changes the repository (discard, commit, switch, push) does what git does, and git's own safeguards apply. Keep a terminal for what AxGit does not offer.
