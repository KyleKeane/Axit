# Security

## Reporting a problem

Please report a security problem privately, through GitHub: [report a vulnerability](https://github.com/KyleKeane/Axit/security/advisories/new). Do not open a public issue for it. Say which app and version, what an attacker could do, and how to reproduce it; leave out personal data.

You will get an answer as soon as the maintainer can give one. A fix goes out as a new release, which installed copies offer through Check for updates, and the advisory is published with it.

## Supported versions

Only the latest release is supported. Installed copies check for it themselves (Axit menu, Check for updates), so updating is one notice away.

## What Axit does that matters here

- The update check asks GitHub for the latest release of this repository and, when the user agrees, downloads and installs it for the user only, without administrator rights. It is the only network call a window makes on its own, and it can be turned off (Options, General).
- AxClaude runs the user's own Claude Code; AxTerminal runs the user's own shells; AxFile runs the programs the user chooses. None of them run anything the user did not ask for.
- Settings and logs stay in the user's profile (`%APPDATA%\Axit`, `%LOCALAPPDATA%\Axit`). Copy diagnostics puts the versions, paths and the log's end on the clipboard for the user to read before sharing.
