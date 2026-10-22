# AxClaude manual tests

The steps and every expected announcement are in `docs/axclaude/nvda-test-plan.md`. Most of it is a conversation with Claude, so the folders are few. Run `.\manual_tests\prepare.ps1 axclaude` first; it makes new folder names each time.

## never-used-with-claude-<date and time>

Claude Code remembers each folder it was trusted in, so this folder has a new name each time the script runs, and Claude asks its trust question again. Sections 1, 2 and 12, step 1: `.\run-axclaude.ps1 manual_tests\axclaude\never-used-with-claude-<the name>`. `hello.txt` is there for Claude to read.

## empty-folder-nothing-to-continue-<date and time>

Section 7, step 7: Session, Open folder, pick it; "Nothing to continue, new conversation".

## project-for-conversations

The folder for the rest of the plan: `notes.md` to ask Claude to change ("add a line to notes.md"), `plain.txt` for AxDown's file-changed-on-disk step. After two exchanges here, section 7, step 8 (the earlier exchanges replayed) works on this folder.

## Not in a folder

- Section 10, Claude Code missing: `.\run-axclaude.ps1 manual_tests\axclaude\project-for-conversations --claude C:\nowhere\claude.exe`.
- Section 13, the Axit menu: open AxDown on `manual_tests\axdown\notes.md` beside AxClaude.
