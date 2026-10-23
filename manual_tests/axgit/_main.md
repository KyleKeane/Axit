# axGIT manual tests

The steps and every expected announcement are in `docs/axgit/nvda-test-plan.md`. Run `.\manual_tests\prepare.ps1 axgit` first, and again after commits, switches or discards, to have the repository as new.

Start with `.\run-axgit.ps1 manual_tests\axgit\scratch-repository-with-changes`.

## scratch-repository-with-changes

A repository of its own, apart from Axit's, with five commits on `main`, a second branch and a remote. Its Changes list holds:

- `tracked-change-me.txt`: modified, unstaged. Space stages it; Enter shows its diff with one added line.
- `tracked-discard-my-change.txt`: modified, unstaged. Delete discards the change (section 3, step 6).
- `staged-new-file.txt`: a new file, staged already. Ctrl+Enter commits it.
- `untracked-new-file.txt`: untracked. Enter shows its text; Delete says to delete it in File Explorer.

Ctrl+2: the log, five commits, newest "Commit 5 of the history". Ctrl+3: the branches, `main` current and `second-branch-to-switch-to`; Enter on the second switches to it.

## scratch-remote-for-fetch-pull-push.git

The repository's remote, a folder on this disk, so fetch, pull and push work with the network off (section 4, step 4). Leave it alone; axGIT uses it through the repository. For the "Fetch failed" step, rename this folder first, then run prepare again.

## not-a-repository

Section 1, step 5: start axGIT on it; the "Not a git repository" notice.

## Not in a folder

Section 2, from the other windows: open axClaude, axDown or axFile on the scratch repository, then their Show current folder in, axGIT.
