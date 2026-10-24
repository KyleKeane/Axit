# axPass user guide

axPass opens a KeePass password database (a `.kdbx` file) in a window that NVDA reads well: its groups and entries
on the left, the selected entry's fields on the right. It is part of Axit, a bundle of apps for people who use a
screen reader. It reads and writes the database through KeePassXC's own command-line tool, so KeePassXC must be
installed; axPass never decrypts the file itself.

## Before you start

- Install KeePassXC from https://keepassxc.org/, or in a console: `winget install KeePassXCTeam.KeePassXC`.
- A database made by KeePass, KeePassXC or KeePassium opens in axPass, and the other way round.
- Start menu: `Axit axPass`. Console: `axpass`, or `axpass C:\Vault\mine.kdbx`. In File Explorer, a `.kdbx` file's
  Open with Axit menu has axPass.

## Unlocking and locking

- axPass opens the database you had last, or Ctrl+O chooses one with the Windows file picker; File, New database
  makes one, asking its master password twice.
- The notice "Unlock mine" asks the master password. Its field hides what you type; NVDA calls it protected.
- A wrong password says "The password does not open mine" and asks again.
- Ctrl+L locks: axPass forgets the master password and everything it read, and asks again. It also locks after ten
  minutes without a key or a mouse move anywhere in Windows (Options, Lock after), when you lock Windows (Windows+L) or switch user, when the computer goes to sleep, and when the window closes.

## The window

- **The navigator**, on the left: the groups, then the entries, of the level shown. Right goes into a group, Left or
  Backspace back up; Right on an entry plays the edge sound. Enter opens: a group shows what is in it, an entry moves
  to its fields. Type the start of a name to jump to it.
- **The detail pane**, on the right: the entry's fields, "Title, text, Gmail", "UserName, user name, sam",
  "Password, password, hidden", "URL, text, …", "Notes, long text, …". Tab goes there; Escape, Tab or Shift+Tab come
  back. A row reads its user name once the entry has been read.

The password is never spoken, never shown and never written anywhere by axPass.

## Using a password

- Ctrl+B copies the user name.
- Ctrl+C copies the password. axPass marks the copy private, so Windows clipboard history (Windows+V), the cloud
  clipboard and axClip keep no copy of it, and clears the clipboard after 30 seconds, when it locks and when it closes,
  unless you copied something else meanwhile.
- Ctrl+U opens the URL in your browser, when it is a web address; anything else (a file, a network folder) is not
  opened.

What travels where: the master password and every password go to KeePassXC's tool through a private pipe only. When
you save an entry's notes, URL, user name or title, KeePassXC's tool can only take them on its command line, where
other programs you run, and Windows' process auditing if it is on, can see them. Keep secrets such as recovery codes
in a password field rather than in the notes.

## Changing entries

- Enter on a field changes it: Title, UserName, URL and Notes in a notice; Notes is a field of several lines, where
  Enter makes a new line and Tab then Enter on Save keeps it.
- Enter on Password: type a new one, then type it again; or press Generate (Alt+G) for one of 20 letters, digits and
  signs, then Enter. Ctrl+Z does not bring an old password back.
- Every change is written to the database at once, by KeePassXC's tool; there is nothing to save.
- Backups: before the first change after you unlock, axPass copies the database file into a folder beside it, called
  "mine backups" for mine.kdbx, named with the date and time. The newest 20 are kept. File, Back up now makes one
  whenever you like; File, Show backups in axFile opens the folder. To use one, open it with Ctrl+O and its master
  password; each is the encrypted file itself.
- Ctrl+M moves the entry to another group, chosen from a list; Ctrl+Z moves it back.
- Ctrl+N makes a new entry here, Ctrl+Shift+N a new group. F2 renames an entry in place: type the new title over the old and press Enter, or Escape. A title cannot hold a slash.
- Delete moves an entry to the Recycle Bin group; Ctrl+Z brings it back. A group moves there after a question, and
  only KeePassXC's own window brings a group back. In the Recycle Bin, Delete removes for good after a question.
- Fields other than KeePass's own ones are shown and are changed in KeePassXC's own window. A group is renamed there
  too.

## If something goes wrong

- "KeePassXC is not installed": install it, then start axPass again. If it is installed somewhere unusual, Options,
  KeePassXC's tool tells axPass where `keepassxc-cli.exe` is.
- "Still working on the last command": a large database takes a moment for each command; wait for the last one.
- Options, Developer, Copy diagnostics puts what is needed for a bug report on the clipboard. It never holds a
  password or anything from the database.

## About

axPass is part of Axit by Dr. Kyle Keane, www.kylekeane.com. KeePassXC is a separate project; axPass only runs its
command-line tool. Axit, About has the licence and the version.

## Disclaimer

axPass is provided as it is, without warranty of any kind. Keep a backup of your database.
