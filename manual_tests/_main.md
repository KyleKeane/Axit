# Manual tests

Files and folders for the NVDA test passes, one folder per app. Each app folder has its own `_main.md`: which folder or file each section of the app's test plan uses, and what to do there. The test plans themselves, with every expected announcement, stay in `docs/<app>/nvda-test-plan.md`.

## Before a pass

1. Close the Axit windows that have files from here open.
2. In PowerShell, from the repository: `.\manual_tests\prepare.ps1`. It takes a few seconds.
3. It empties every app folder except its `_main.md` and makes all the files again. Run it again after any test that renamed, deleted or moved something, and the folder is as new. `.\manual_tests\prepare.ps1 axfile` does one app alone.

Git ignores everything the script makes, so a test pass never changes the repository.

## The app folders

- `axfile`: navigation with levels, type-ahead, hidden entries, filter and sort, selection, file operations, undo, the Actions list, the hand-overs to axMedia and axPreview.
- `axmedia`: a first queue, a replacement queue, two folders to add, one long file with the time spoken every 5 seconds, two short files for the end of the queue, a video, files that cannot play.
- `axpreview`: a readme with headings, links, a list, a quote and a table; plain text; a log; JSON; a binary file; a file over 32 MB.
- `axdown`: the four encodings, a long Markdown file for every navigation key, a file to change on disk, a file that is not text.
- `axjson`: a valid file, a broken file, a one-line file to format, a formatted file.
- `axgit`: a scratch repository with history, a second branch, changes of every kind and its own local remote; a folder that is not a repository.
- `axterminal`: a start folder with a subfolder and small scripts: a question, many lines, a long run, a failure.
- `axclaude`: a folder Claude has never seen, an empty folder, a project folder.

## The sounds

The sound files are made with Windows' own speech voice. Each one says what it is at its start, for instance "Queue folder A, track 2 of 3", and then the time every 5 seconds, "5 seconds", "10 seconds", so you can hear which file plays and whether a seek landed. They are WAV files: Windows has no MP3 encoder to make MP3 files with. The video is a short one copied from Windows' own setup, when this Windows has it.

## Order

The apps are independent. A full pass before a release: axFile, then axMedia and axPreview (axFile hands over to both), then axDown, axJSON, axGIT, axTerminal and axClaude.
