# axPass — Specification

axPass is an app of the Axit bundle (`docs/axit/SPEC.md`): a KeePass password database (`.kdbx`) read and changed in
a window NVDA reads well, for a blind developer who uses the NVDA screen reader. It is an item app: its window, keys
and field types are the shared ones of `docs/axit/item-apps.md` (cited as IA-n.m and not repeated), and section 6
there is the one table of where axPass and axTask differ. This document holds only what is axPass's own. Written
2026-10-24 at the user's ask ("start building both apps"), before the engine has been run on this computer (the
format and the tool are in `docs/axpass/kdbx-notes.md`). Requirements are numbered `PW-n.m` and decisions `PW-Dn`. In
axPass's code and documents, "SPEC.md" means this file.

## 1. Summary

One database per window. The navigator is its groups and entries, moved through as axTask moves through lists and
items: Right into a group, Left back up, Enter or Tab to an entry's fields. An entry's fields are KeePass's own: Title,
UserName, Password, URL and Notes, read "UserName, user name, kyle" and "Password, password, hidden". Ctrl+B copies
the user name and Ctrl+C the password, which the clipboard loses again after 30 seconds. Ctrl+L locks: the window
forgets the master password and shows only the notice that asks for it.

## 2. Goals and non-goals

- **G1 Read and use passwords by ear.** Find an entry (type-to-find, Ctrl+F), copy its user name and password, open
  its URL.
- **G2 Change them.** Add, rename, edit and archive entries and groups; generate a password.
- **G3 Never reimplement the format.** KeePassXC's command-line tool reads and writes the file (PW-D1); axPass never
  decrypts it itself.
- **G4 Keep the secret secret.** The password is never spoken, never shown, never written to a file by axPass, and
  never passed on a command line.
- **Non-goals for the first version.** Key files and hardware keys, attachments, one-time codes (TOTP), entry history,
  custom attributes changed in the app (they are read and shown), merge, a browser extension, auto-type.

## 3. Scenarios

1. `axpass C:\Vault\mine.kdbx`: the notice "Unlock mine", the field "Master password" whose text NVDA reads as
   stars. Type it, Enter: "mine, 3 groups, Email, group, 4 entries".
2. Right on Email: "Email, 4 entries, Gmail". Ctrl+B: "Copied the user name of Gmail". Ctrl+C: "Copied the password
   of Gmail; the clipboard clears in 30 seconds".
3. Tab: "Fields of Gmail, Title, text, Gmail". Down: "UserName, user name, kyle", "Password, password, hidden".
   Enter on Password: the notice "Change Password" with a hidden field and a Generate button.
4. Ctrl+L: "Locked"; the unlock notice in place of the panes.

## 4. Opening, the password, locking

- **PW-4.1** axPass opens the `.kdbx` file of its command line, or the one it had last, or asks with the Windows
  file picker (File, Open, Ctrl+O). A new database is File, New: a file name, then the master password twice.
- **PW-4.2** The master password is asked in a notice whose field hides what is typed (the shared `OverlayInput`
  gains a hidden form for it). A wrong password says "The password does not open mine" and asks again.
- **PW-4.3** Lock (Ctrl+L, and after ten minutes without a key, a setting): the panes are cleared, the password
  dropped, the unlock notice shown. Closing the window locks it.
- **PW-4.4** axPass never keeps a copy of the database or of any entry on disk.

## 5. The engine (PW-D1)

- **PW-5.1** `keepassxc-cli.exe`, found in KeePassXC's install folder, on the PATH, or at a path the user sets
  (Options). Without it, the notice "KeePassXC is not installed" names its download page and the command that
  installs it (`winget install KeePassXCTeam.KeePassXC`).
- **PW-5.2** One process per command, the database's path and the entry's path as arguments, the master password as
  the first line of standard input, `-q` so the prompt goes nowhere (read from the tool's source, kdbx-notes 7). The
  commands: `ls -R -f` for the groups and entries, `show` (without `-s`, so the password stays hidden) for an entry's
  fields, `clip` for the copies, `add`, `edit`, `mkdir`, `mv`, `rm`, `generate`, `db-create`.
- **PW-5.3** Every command runs off the UI thread (bundle rule 3); the panes say "Reading mine…" while the list is read.
- **PW-5.4** A copy is `clip <entry> 30` with `-a UserName` or the password: KeePassXC puts it on the clipboard and
  clears it itself after 30 seconds, so axPass never holds the password to copy it.

## 6. axPass's part of the shared shape

- **PW-6.1 The fields** are KeePass's standard ones in this order: Title (`text`), UserName (`username`), Password
  (`password`), URL (`text`), Notes (`longtext`); then the entry's custom attributes, read only (Enter: "Change it in
  KeePassXC"). New field (Ctrl+N in the detail pane) says the same.
- **PW-6.2 The two extra types.** `username` reads and changes as `text`. `password` reads "hidden"; Ctrl+Q on it
  says "hidden: Ctrl+C copies it"; Enter changes it in a hidden field, with Generate (Ctrl+G) filling it from
  `generate` (20 characters, upper and lower case, digits, signs), and asks it twice.
- **PW-6.3 The row summary** (IA-6): the title and the user name, "Gmail, kyle".
- **PW-6.4 Delete** moves the entry or the group to KeePass's Recycle Bin group (`rm` or `rmdir`, which make the group
  on first use); Ctrl+Z moves an entry back with `mv`; a group cannot come back that way (the tool moves entries only), so
  Delete on a group asks first and says that KeePassXC's own window brings it back. In the Recycle Bin, Delete removes
  for good after a question, since the tool asks none. A title cannot hold a slash (the tool's paths have no escape).

## 7. Keys

On top of IA-3, in `AxPassKeys`:

| Key | Where | What |
| --- | --- | --- |
| Ctrl+B | anywhere | copy the user name of the selected entry |
| Ctrl+C | anywhere | copy the password; cleared after 30 seconds |
| Ctrl+U | anywhere | open the URL in the browser |
| Ctrl+G | in a password's notice | generate a password |
| Ctrl+L | anywhere | lock |
| Ctrl+O | anywhere | open another database |

## 8. Decisions

- **PW-D1 KeePassXC's command-line tool is the engine** (plan, B-10): Argon2, ChaCha20 and the format stay KeePassXC's.
  The cost: KeePassXC must be installed, with the Visual C++ runtime its installer brings (kdbx-notes 7).
- **PW-D2 The master password lives in the window's memory while unlocked**, as a .NET string, which cannot be wiped
  on demand; it goes when the window locks or closes and the garbage collector runs. Said plainly here rather than
  promised away. It reaches the tool through a pipe, never a command line, a file or an environment variable.
- **PW-D3 KeePass's standard fields only, in the first version**: the tool has no command that changes a custom
  attribute (its manual, kdbx-notes 6), and axPass never edits the file itself.
- **PW-D4 The clipboard is cleared by the tool** (`clip` with a timeout), not by axPass.

## 9. Open questions, to settle by running the tool

- Settled by running the tool (kdbx-notes 8): the output of `ls -R -f` and `show`, `edit -p` reading the new
  password twice after the master password, the pipe, `rm` and `rmdir` moving to the Recycle Bin and deleting for
  good inside it, about 100 ms a command with AES-KDF.
- Still open: a title holding a slash; custom attributes in `show`; `clip` from a window with no console; a large
  database with Argon2, where a process per command may be slow enough to want KeePassXC's `open` mode.

## 10. Status

Specified, not built. The build starts when the tool runs on this computer (it needs the Visual C++ runtime, which
needs administrator rights) and section 9 is settled. With it, the shared pieces of axTask's window move to
`Axit.Core` and `Axit.Forms`, one per commit (IA-D5).
