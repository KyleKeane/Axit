# axPass NVDA test plan

Run with NVDA before a release, with KeePassXC installed, on the database of `manual_tests/axpass` (made and reset by
`.\manual_tests\prepare.ps1 axpass`, whose master password is `test-master-1`), after
`.\run-axpass.ps1 manual_tests\axpass\test.kdbx`, never on your own database. The database holds the groups `Email`
(`Gmail`, user name `sam`, a URL and two lines of notes) and `Work`, and the entry `Top entry`. Every step says what NVDA
should read; a step that reads differently is a defect. The specifications are `docs/axit/item-apps.md` (IA-n.m) and
`docs/axpass/SPEC.md` (PW-n.m).

## 1. Unlocking (PW-4)

1. Start axPass: "test - axPass window", then the notice "Unlock test" with the field "Type the master password to unlock test, protected".
   Type `wrong`, Enter: "The password does not open test." and the notice again.
2. Type `test-master-1`, Enter: "Unlocking test", then "test, 3 items, Email, group, 1 item".
3. Ctrl+L: "Locked" and the unlock notice. Escape: "test, locked: Enter unlocks". Enter: the notice again; unlock.

## 2. Moving and reading (IA-2, IA-3, PW-6)

1. Right on Email: "Email, 1 item, Gmail". Right on Gmail: the edge sound. Left: back on Email.
2. Enter on Gmail (or Tab): "Fields of Gmail", "Title, text, Gmail". Down: "UserName, user name, sam", "Password,
   password, hidden", "URL, text, https://mail.example.com", "Notes, long text, line one, and 1 more line".
3. Ctrl+Q on Password: "Password, password, hidden". Escape; Ctrl+Q in the navigator: every field, the password
   "hidden".
4. After the entry has been read, the navigator's row reads "Gmail, sam".

## 3. Copying (PW-5.4, PW-7)

1. Ctrl+B on Gmail: "Copied the user name of Gmail"; paste it in axDown: `sam`.
2. Ctrl+C: "Copied the password of Gmail; the clipboard clears in 30 seconds". Paste at once: the password; after 30
   seconds, paste again: nothing.
3. Ctrl+U: the browser opens the URL.

## 4. Changing (IA-5, PW-6)

1. In the fields, Enter on UserName, type `sam2`, Enter: "UserName: sam2". Ctrl+Z: "Undone: UserName: sam2"; it reads
   sam again.
2. Enter on Password: the notice "New password for Gmail", its field protected. Alt+G: "Generated a password of 20
   characters; Enter sets it"; Enter: "Password set for Gmail".
3. Enter on Password, type `one`, Enter, type `two`, Enter: "The two passwords differ: nothing changed".
4. Ctrl+N on Gmail's field list: "An entry has KeePass's own fields here …".
5. F2 in the navigator on Gmail, type `Gmail personal`: "Renamed Gmail personal". Type `a/b` instead: "A name cannot
   hold a slash …".
6. Ctrl+N in Email, type `Bank`: "Added Bank; Tab, then Enter on Password sets its password". Delete on Bank: "Moved
   Bank to the Recycle Bin; Ctrl+Z brings it back". Ctrl+Z: Bank is back.
7. Delete on the group Work: the question "Move the group"; Cancel is the default.
8. Ctrl+M on Gmail personal: the list "Group" on "The top of the database"; Down to Work, Enter: "Moved Gmail personal
   to Work". Ctrl+Z: "Undone: Moved Gmail personal to Work", back in Email.
9. Every password field says what to do: "Type the master password to unlock test", "Type a new password for
   Gmail", "Re-enter the password for Gmail".
10. After the first change, File, Show backups in axFile: the folder "test backups" with one copy named with the time.
    File, Back up now: "Backed up: test …kdbx, in test backups".

## 5. The rest

1. Shift+F10 in each pane: its menu (Group, Entry or Field) opens and NVDA reads its first item.
2. F1: the keys; Help, User guide: the guide. Alt+A, About, Version names axPass.
3. Options, Lock after, 5 minutes: "axPass locks after 5 minutes without a key"; wait five minutes away: locked.
4. Close and start again: the unlock notice for the same database.
