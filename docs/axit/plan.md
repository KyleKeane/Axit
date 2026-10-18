# How AxClaude became Axit, and what is still to come

The record of the transition of 2026-10-02, kept short: the shape that was chosen, the decisions, what was built,
and the list that remains. The specifications describe the result (`docs/axit/SPEC.md`, `docs/axclaude/SPEC.md`,
`docs/axdown/SPEC.md`); the commit history holds the steps.

## The shape

One executable, `Axit.exe`, with a verb per app (`claude`, `down`); a path alone picks the app. Each app is a
WinForms class library with one entry point, with its pure code in a `.Core` project and its documents in
`docs/<app>/`. Shared code lives in `Axit.Core` (pure) and `Axit.Forms` (WinForms) and moves there on its second
use, unchanged. One installer, one updater, one version, one changelog, one release workflow. A new app follows
`docs/axit/adding-an-app.md`.

## Decisions taken

1. The GitHub repository was renamed `KyleKeane/Axit` with 2.0.0; the old address redirects.
2. `axit` with no argument starts AxClaude on the current folder.
3. AxClaude 1.7.0 was released first as a bridge: its updater accepts a download whose program file is not
   `AxClaude.exe`, so installed copies could update into Axit.
4. AxDown registers under Open with for `.md`, `.markdown` and `.txt` and appears on every file's right-click menu;
   Windows decides defaults.
5. AxDown's navigation keys are NVDA's browse-mode letters with Ctrl (next) and Ctrl+Shift (previous), headings
   first (AxDown SPEC AD-D7).
6. The program file is `Axit.exe`; the Start menu entries are `Axit AxClaude` and `Axit AxDown`.
7. Keys live in tables, bundle-wide (`BundleKeys`) and per window (`AxClaudeKeys`, `AxDownKeys`), with a collision
   test (bundle SPEC AX-8); the notices and the question area are shared, not copied (AX-6.4).
8. Plain code only (B-10), zero third-party dependencies, and the working app never broken: the transition ran on
   a branch, one step per commit, every commit building and passing the tests, merged after NVDA checks.

## What was built, in order (all on 2026-10-02)

The bundle spec and the document split; the 1.7.0 bridge; `Axit.exe` with AxClaude inside and the tested dispatcher;
the shared pure code in `Axit.Core`; the installer, publish and release scripts for the bundle; the AxDown
specification; `AxDown.Core`; the AxDown window and the shared window pieces in `Axit.Forms`; AxDown's installer
entries and icon; the user's NVDA checks and the 2.0.0 release; AxClaude's key table (2.1.0); heading navigation in
AxDown (2.1.0); the adding-an-app checklist. After 2.1.0: AxDown's window spoken on opening (2.1.1), the update notice
with its four buttons and the progress bar (2.2.0), the in-place install with one bar for download and install and
the restart by the app itself (2.3.0).

## Still to come

- AxClaude's open items are in `docs/axclaude/SPEC.md` §12 To do, the one list for them.
- The text editor window is shared since AxJSON (2026-10-18: `Axit.Forms/TextEditorForm` with `EditorApp`,
  `EditorKeys`, `EditorHost`; AxDown and AxJSON are records over it). The window base `Axit.Forms/AxitForm` (AX-6.5)
  holds the notice flow, the fonts, the window's place, find and the update flow since the same day; the editor
  shell, AxGit's window and AxClaude's `MainForm` all derive from it; the recent lists (`RecentMenu`), the Options
  and Help menus and the File Explorer opener followed the same day. Left: `ControlArea`'s use in AxClaude only; the
  "Show current … in" submenus stay in each window, three lines each. AxClaude's window is partial files, one per
  section, since the same day.
- AxJSON, an editor for JSON files (`docs/axjson/SPEC.md`): raw mode built 2026-10-18 as the editor shell plus JSON's
  navigation keys and Format (AJ-D4, B-14: the shell shared, one app per file type, one file per window, the Axit menu
  for the open windows). Still to come: structured mode, a form of real input fields from templates (AJ-3), and the
  caret's path in the status bar (AJ-2.4).
- AxDown and AxJSON, a file changed outside the editor (asked and built 2026-10-18, AD-3.6): compared when the window
  becomes active again; reloaded with the caret kept when nothing is unsaved, said once otherwise, and Save offers
  Reload and lose mine. No watcher thread, no merging. A watcher for live reload while the window stays active is
  the second step, only if the activation check turns out not enough.

## Later apps (recorded 2026-10-18)

Each would be one more app of the bundle, a window per thing worked on, added the way `adding-an-app.md` says, with no
library dependency (B-10: `git.exe` and the file system through the APIs already used, as Claude is `claude.exe`).

- **AxGit**: built as a first draft the same day (`docs/axgit/SPEC.md`), unreleased until its NVDA pass; the notes
  below were its plan. A git manager for a repository. One window per repository, named after it, opened from the other windows'
  "Show current folder in ▸" and "Show current file in ▸" submenus (the slot the specs already keep for an Axit app
  next to File Explorer) and from the Axit menu, so that it stays tied to the folder or file the user came from and is
  found again in Open windows (AX-9) rather than lost. Inside the window the bundle's own pieces: lists (status as
  the changed files, log, branches), read-only text with the jump keys (a diff, a commit), one field (the commit
  message), notices for questions. A streamlined set to start with: status, diff of a file, stage and unstage,
  commit, log, branch and switch, fetch, pull and push; conflicts and stashes shown and left to the terminal or to
  Claude at first. Everything through `git` commands with their text parsed, nothing reimplemented. In the same
  window as AxClaude or the editors it would be a Git menu three times over and a second set of lists in a window
  built around one thing; and in AxClaude much of it can already be asked of Claude, so AxGit's worth is the quick,
  deterministic read of a repository's state without a turn.
- **AxFile** (name not settled), a file manager: a streamlined tree, not File Explorer's context menu reproduced. A
  list of a folder's entries with the folders first, Enter opens a file in the editor for its type or a folder in the
  list, Backspace goes up, a few operations (new folder, rename, delete to the recycle bin, copy the path) and "Show in
  File Explorer" for everything else. The worry on record: the whole of a file tree's behaviour is endless; the scope
  must stay small.
