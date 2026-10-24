# AxPass.Core, the pure code

Everything of axPass that needs no window, unit tested in `tests/AxPass.Tests` (with one test that runs KeePassXC's
tool on a throwaway database when KeePassXC is installed, and passes without running where it is not, as on the build
servers). The spec is `docs/axpass/SPEC.md` (PW-n.m); what the tool prints and takes is `docs/axpass/kdbx-notes.md`
section 8. The window is `src/AxPass`, with its own `CLAUDE.md`. The item format and the field types are `Axit.Core`'s.

## Commands

```
.\test.ps1 AxPass
```

## Where things are

- `KeePassCli` and `CliResult` (PW-5): `Find` (the path set, KeePassXC's install folder, the PATH), `Run` (one command,
  the arguments in `ArgumentList`, the input lines on standard input, a timeout), `StartLasting` (`clip`, left running),
  `On` (the command, `-q`, the options, the database, the paths).
- `PassTree` and `PassNode`: `ls -R -f` read into groups and entries (`Parse`), a group's `Level` (groups first, the
  Recycle Bin last at the top), `CountIn`, `HasGroup`, `HasEntry`, `InRecycleBin`, `Join`, `RefusedName` (a slash),
  `DatabaseName`.
- `EntryFields` (PW-6): `show` read into an `ItemDocument` of KeePass's fields (the password's value never read, its
  type `password`), other attributes after them; `EditOptions` (the `edit` options that make one document into
  another); `IsStandard`, `ValueOf`.
- `PassSettings`: the database open last, the tool's path when set, the minutes before the lock.

## Gotchas

- `show` prints a note of several lines over several lines; a new field starts only at a name `show` prints itself.
- The test data uses `sam` as a user name and `test-master-1` as a password, never a real name or secret.
