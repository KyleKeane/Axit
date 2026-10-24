# axPass manual tests

The steps and every expected announcement are in `docs/axpass/nvda-test-plan.md`. KeePassXC must be installed. Run `.\manual_tests\prepare.ps1 axpass` first; it makes the database again with KeePassXC's own tool.

Start with `.\run-axpass.ps1 manual_tests\axpass\test.kdbx`. The master password is `test-master-1`, a test value for this throwaway database only.

## The database

- `Email`: the entry `Gmail`, user name `sam`, a URL, two lines of notes, and a password set when it was made. Sections 2 to 4.
- `Work`: an empty group. Section 4, step 7.
- `Top entry`, at the top. Section 2.
- The Recycle Bin appears with the first Delete; prepare.ps1 makes the database again without it.
