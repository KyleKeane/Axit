# axProcess user guide

axProcess shows what runs on your computer, made for screen reader users: the running programs with how much memory and processor time they use, the Windows services, and the programs that start when you sign in. One window, one list, three views. Nothing changes while you read: the list is read again only when you press F5 or after something you did.

## Install

axProcess is part of Axit. Installing Axit (see the Axit guide in the Axit menu, About) gives you a Start menu entry `Axit axProcess` and a console command `axprocess`. Any Axit window starts one too: Alt+A, New window, axProcess.

## Start

The window is called "This computer - axProcess". It opens on the Processes view; the list fills after about a second, the time it takes to measure the processor, and axProcess then says its name and the first item.

## The views

- **Processes** (Ctrl+1): every running program, "chrome, 350 MB, 3% CPU, process 9120": its name, the memory it uses, its share of the processor over the last second, and its process ID. The largest first; View, Sort processes by, changes the order to processor or name.
- **Services** (Ctrl+2): every Windows service, "Windows Update (wuauserv), running, manual": its name, its short name, whether it runs, and how it starts.
- **Startup** (Ctrl+3): what starts when you sign in, "OneDrive, enabled, current user's Run key": its name, whether Task Manager has turned it off, and where it is set.

Type the start of a name to jump to it, as in File Explorer: letters typed within a second of each other build one search, the same letter again goes to the next name starting with it, and Shift with a letter goes backwards. Tab and Shift+Tab move to the next and the previous view, round from Startup back to Processes. Pressing a view's key while the view already shows says its name again. At the first or the last item, Up, Down, Home and End play a short low sound, since there is nowhere to move. **F5** reads the view again and says "Refreshed". **Ctrl+Q** says the summary: how many processes run, how much memory is in use, and how busy the processor was.

## Acting on an item

- **Enter** shows the item's details in a notice, with buttons for what can be done to it. Escape closes it.
- **Delete** on a process ends it, after a question: "End notepad (process 4410)? Anything unsaved in it is lost." Process, End process tree ends it with the programs it started.
- **Ctrl+E** opens axFile on the program's file, selected in its folder.
- **Shift+F10**, or the Applications key, opens the menu of the view you are in: Process, Service or Startup.
- **Ctrl+C** copies the line. **Ctrl+F** finds an item by its text; F3 finds the next.
- A service's details offer **Start**, **Stop** and **Restart**. Windows lets only an administrator start and stop most services, and axProcess runs as you, so expect "access is denied" for those; axProcess says so plainly.

The Startup view is for reading: axProcess does not turn startup programs on or off yet.

## Menus

- **Computer**: Refresh (F5), Summary (Ctrl+Q), Copy line (Ctrl+C), Exit.
- **Process**: Details (Enter), End process (Delete), End process tree, Show program in axFile (Ctrl+E).
- **Service**: Details (Enter), Start, Stop, Restart.
- **Startup**: Details (Enter), Show program in axFile (Ctrl+E).

Only the menu of the view you are in can be opened; the other two are unavailable, so that you never end a process from the services or the reverse.
- **View**: Processes (Ctrl+1), Services (Ctrl+2), Startup (Ctrl+3), Sort processes by.
- **Options**: Text (font and size), General (the update check), Developer (the settings file, diagnostics).
- **Axit**: new windows of every app, the files and folders closed lately, the open windows, updates, About.
- **Help**: Keyboard shortcuts (F1), this guide, Windows resources.

## Settings

axProcess keeps the font, the text size, the update check, the window's place and the order of the Processes view in `%APPDATA%\Axit\axprocess.json`. Options, Developer, Open settings file opens it in axJSON.

## Updates

axProcess checks for a newer Axit when it starts and offers it in a notice; the Axit menu has Check for updates at any time. One update brings every app of Axit up to date.

## If something goes wrong

axProcess's log is `%LOCALAPPDATA%\Axit\logs\axprocess.log`; it names every process you ended and every service you started or stopped. Options, Developer, Copy diagnostics copies its end with the versions, for a bug report.

## About axProcess

axProcess is one app of Axit, by Dr. Kyle Keane (www.kylekeane.com), MIT licence. The Axit menu's About names the version and the licence.

## Disclaimer

Ending a process loses anything unsaved in it, and ending a part of Windows can make the computer unstable until you sign in again. axProcess asks before it ends anything; read the question.
