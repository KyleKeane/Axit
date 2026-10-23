# axMedia user guide

axMedia is a player for sound and video files, made for screen reader users. One window holds one queue: the files to play, in order, as a plain list, the one playing marked. Enter plays the file you are on and the rest follow, like an audiobook or an album; Space pauses; a few keys seek, skip, change the speed and say where you are. It plays whatever Windows can decode.

## Install

axMedia is part of Axit. Installing Axit (see the Axit guide in the Axit menu, About) gives you a Start menu entry `Axit axMedia`, a console command `axmedia`, axMedia in the `Open with Axit` submenu when you right-click a sound or video file in File Explorer, and axMedia under Windows' own `Open with` for those files, so that you can make it the program that opens them when you press Enter on one: Settings, Apps, Default apps, axMedia, or right-click a file, Open with, Choose another app, axMedia, Always. Windows lets only you set that, never a program.

## Start

- From axFile: Enter on a sound or video file, once axMedia is its default program in Windows; otherwise the entry's menu (Shift+F10), Open in Axit, axMedia. Either way the file plays with the folder's other sound and video files after it, in name order, and the axMedia window comes to the front. axFile's Ctrl+Enter plays the whole folder here from the first file; Ctrl+Shift+Q adds the entry, or the selected entries, to the end of the queue without taking you out of axFile, "Added 24 to axMedia's queue".
- File Explorer: right-click a sound or video file, open `Open with Axit` and choose axMedia; or double-click it once axMedia is its default program.
- Start menu: `Axit axMedia`. Press the Windows key and type `axm`. The window opens with an empty queue; Ctrl+O adds files.
- Console: `axmedia C:\Music\Dune\03.mp3` (the file with the folder's others after it), `axmedia C:\Music\Dune` (the folder from the first file), `axmedia a.mp3 b.mp3` (those, in order), `axmedia --add C:\Music\More` (added to the end of the queue, nothing starts).

axMedia keeps one window: a file opened from axFile, File Explorer or the console while an axMedia window is open goes into that window, replacing its queue (or added to it, for an add), rather than opening another. A second window is the Axit menu's New window, or Ctrl+N, or `--new` on the console.

The window is named after the file playing: "03 Chapter 3.mp3 - axMedia", or "axMedia" while nothing plays. The status bar (NVDA+End) has what is playing and the time on the left, "Playing 03 Chapter 3.mp3 12:40 / 45:30", and the place and the speed on the right, "3 of 12, speed 1.25".

## The queue

The list is the one control, named with its count, "Queue, 12 items". Arrow keys, Home, End, Page Up and Page Down move through the files; a typed letter jumps to the next name starting with it; nothing is spoken until you ask. The file playing reads "(playing)" after its name. An empty queue shows one line, "Queue is empty".

- **Enter** plays the file you are on, "Playing 03 Chapter 3.mp3"; when it ends the next file plays, to the end of the queue: "End of queue" with the done chime. Files already played stay in the list until you clear it or remove them.
- **Delete** takes the file out of the queue, "Removed 03 Chapter 3.mp3"; the one playing goes on to the next, or stops at the end.
- **Ctrl+I** says the file's place, "03 Chapter 3.mp3, 3 of 12", and, while it plays, "playing, 12:52 of 45:30".
- **Ctrl+F** finds the next file whose name contains the text; F3 and Shift+F3 the next and the previous match.
- **Ctrl+O**, Open files: the Windows file picker on sound and video files, several at once; they are added to the end of the queue, "Added 3". Nothing starts playing by itself; Enter on one does. **Ctrl+Shift+O**, Open folder: the Windows folder picker; the folder's sound and video files are added in name order, "Added 12", or "No sound or video files in Dune". Queue, Open recent lists the files and folders added lately by number, "1. Dune (C:\Music\Dune)", so Alt+Q, R, 1 adds the first again.
- Queue, Clear queue empties the queue and stops the sound, "Queue cleared".
- **Ctrl+W** shows the file playing, or the one you are on, selected in File Explorer; Queue, Show current file in, axFile opens axFile on its folder with the file selected.
- **Ctrl+N** opens a second axMedia window with an empty queue.

## Playing

- **Space**, or **Ctrl+P** from anywhere in the window, pauses, "Paused at 12:40", and resumes, "Playing".
- **Ctrl+Right** and **Ctrl+Left** move 15 seconds forward or back and say the new position, "12:25".
- **Ctrl+Shift+Right** and **Ctrl+Shift+Left** play the next or the previous file of the queue; "First file" and "End of queue" at the ends.
- **Ctrl+Shift+Period** and **Ctrl+Shift+Comma** make it faster or slower by a quarter, between a half and three times: "Speed 1.25". The voice keeps its pitch. Play, Normal speed goes back to 1. The speed stays for the next file and the next window.
- **Ctrl+T** goes to a time: `12:40`, `1:02:10`, or `750` for seconds.
- **Ctrl+Q** says what is playing: "Playing 03 Chapter 3.mp3, 12:52 of 45:30, speed 1.25", or "Nothing playing".
- Play, Say each file as it starts (on by default) is the one thing axMedia says on its own: the next file's name when the queue moves on. Turn it off to hear nothing between chapters.
- Closing the window stops the sound; there is no Stop key, Space pauses.

The volume is Windows' own: your keyboard's volume keys work from anywhere. A video's picture is drawn in an area at the top of the window while it plays; the keys are the same. The formats are whatever Windows decodes: MP3, WAV, WMA, M4A and M4B, AAC and FLAC are built in; OGG, Opus and some video formats need the free codec extensions from the Microsoft Store (Help, Media resources). A file Windows cannot play is said, "Cannot play clip.ogg: this kind of file is not supported", and the next plays.

## Menus

- **Queue**: New window (Ctrl+N), Open files (Ctrl+O), Open folder (Ctrl+Shift+O), Open recent, Play (Enter), Remove (Delete), Clear queue, Information (Ctrl+I), Show current file in (File Explorer Ctrl+W, axFile), Exit.
- **Play**: Pause or resume (Space), Next file, Previous file, Forward 15 seconds, Back 15 seconds, Go to time (Ctrl+T), Faster, Slower, Normal speed, Say each file as it starts, What is playing (Ctrl+Q).
- **Options**: Text (font and size), General (the update check), Developer (the settings file, diagnostics).
- **Axit**: new windows of every app, the files and folders closed lately, the open windows, updates, About.
- **Help**: Keyboard shortcuts (F1), this guide, Media resources.

## Settings

Everything you change is kept in `%APPDATA%\Axit\axmedia.json`: the speed, Say each file as it starts, the recent files and folders, the font and text size, the update check, and one setting with no menu item: `seekSeconds` (what Ctrl+Left and Ctrl+Right move by, 15). Options, Developer, Open settings file opens it in axJSON.

## Updates

axMedia checks for a newer Axit when it starts and offers it in a notice; the Axit menu has Check for updates at any time. One update brings every app of Axit up to date.

## If something goes wrong

axMedia's log is `%LOCALAPPDATA%\Axit\logs\axmedia.log`; Options, Developer, Copy diagnostics copies its end with the versions, for a bug report. "Cannot play …: this kind of file is not supported" means Windows has no decoder for the file; the Microsoft Store has free ones for OGG, Opus and several video formats.

## About axMedia

axMedia is one app of Axit, by Dr. Kyle Keane (www.kylekeane.com), MIT licence. The Axit menu's About names the version and the licence.

## Disclaimer

axMedia decodes with the codecs Windows has and changes nothing in your files.
