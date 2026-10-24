# axTask manual tests

The steps and every expected announcement are in `docs/axtask/nvda-test-plan.md`. Run `.\manual_tests\prepare.ps1 axtask` first; it puts the folder of lists back as it was.

Start with `.\run-axtask.ps1 manual_tests\axtask\tasks`.

## The files

- `tasks\Home`: `Fix the tap` (a task due 2026-10-30) and `Water plants` (done), and `notes.txt`, not an item, counted as a file not shown. Section 1.
- `tasks\Shopping`: `Milk` (a plain item, no Done) and `Washer` (a task without a due date). Sections 2 and 3.
- `tasks\Course plan`: `Week one`, with a long text of two lines in Notes; the list is in the section Work (its Section field). Sections 1 and 2.
- The Archive list appears with the first Delete; prepare.ps1 removes it again.
