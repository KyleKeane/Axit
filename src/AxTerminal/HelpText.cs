using System.Reflection;
using Axit.Core;

namespace AxTerminal;

/// <summary>The texts the window shows as notices: the keyboard shortcuts (F1) and the user guide.</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AT-6.2): a first line naming the text's own keys, then one Markdown heading per group, so that the heading letters walk it (bundle AX-7.2).</summary>
    public const string Shortcuts = """
        In this text: h and Shift+H move by heading, 1 to 6 by level. Ctrl+F finds, F3 and Shift+F3 the next and the previous match

        ## Anywhere
          Ctrl+1: go to the command field. Ctrl+2: go to the terminal. Both say where you are, also when you are there already
          Ctrl+3: go to the latest output, in the terminal
          Tab, Shift+Tab, Ctrl+Tab or F6: switch between the command field (or the recent commands list in its place) and the terminal
          Ctrl+Shift+C: interrupt, that is send Ctrl+C to the shell
          Shift+Escape: send Escape to the shell
          Ctrl+Up and Ctrl+Down in the field: the previous and the next command, then what you typed or last changed
          Ctrl+R: the recent commands of this window as a list; Enter puts one back in the field
          Ctrl+Q: say what is running, for how long, and the prompt; the shell too while idle
          Ctrl+P: say the current folder, the full path
          Ctrl+F: find text in the terminal. F3 and Shift+F3: next or previous match
          Ctrl+S: export the transcript as a Markdown file
          Ctrl+W: show the current folder in File Explorer
          Ctrl+N: new session: choose the folder and the shell (Windows PowerShell, Command Prompt, PowerShell 7 and Git Bash when they are installed, or a custom command line; Switch shell offers to install the two that are not); Session, Switch shell changes the shell in the same folder and keeps the text
          Ctrl+Shift+R: restart the shell in its current folder, after a confirmation while a command runs
          Ctrl+L: clear the transcript (the shell's own cls clears only its screen)
          Ctrl+Alt and a letter from the Terminal list below: read every line of that kind from the caret to the end of the terminal in one go, without moving; Ctrl+Alt+P reads the paragraphs whole. Control stops NVDA
          Ctrl+Shift+K: bookmark the terminal line the caret is on, or take the bookmark away
          Ctrl+Space: background music while a command runs, on or off, and says which
          Ctrl+Plus, Ctrl+Minus: bigger or smaller text
          F1: this list
          Ctrl+Shift+P: the command palette: type part of a command's name, Down and Up through the matches, Enter runs it
          Alt+A: the Axit menu: the command palette, a new window of any app, the files and folders closed lately, the open Axit windows, most recent first (Alt+A, O, 1 is the one you were in last) and by type, Close all windows, the AI tools, Check for updates, and About
          Alt+F4: close axTerminal (asks first while a command runs)

        ## Command field
          Enter: send the command (an empty field sends a plain Enter to the shell). A command of several lines goes line by line, with one more Enter at the end for PowerShell
          Ctrl+Enter (Ctrl+Return): send the text without recording it: no Input and Output lines, not in the recent commands, not in the log. For passphrases and tokens a program asks for
          Shift+Enter: new line
          Page Up, Page Down: one screen of the field up or down, or the first or last line
          Escape: does nothing; a reminder says that Shift+Escape sends Escape and Ctrl+Shift+C interrupts
          Ctrl+Up, Ctrl+Down: the previous or next paragraph of the text; with Shift, select to there

        ## Terminal
          Arrow keys, Home, End, Ctrl+Home, Ctrl+End: move as in any text
          Page Up, Page Down: one screen up or down
          Ctrl+Up, Ctrl+Down: the previous or next paragraph (the first line after a blank line); with Shift, select to there
          l: say the line number, like "Line 12 of 340"
          i and Shift+I: next and previous "# Input" line (a command you sent)
          o and Shift+O: next and previous "# Output" line (what the shell printed for it)
          h and Shift+H: next and previous heading. 1 to 6: heading of that level (Input and Output lines are level 1)
            Both work in the user guide and the other help texts too, by their Markdown headings
          p and Shift+P: next and previous paragraph (the first line after a blank line)
          s and Shift+S: next and previous system line
          m: bookmark this line (a "Bookmark 1" line appears above it), or take the bookmark away again
          k and Shift+K: next and previous bookmark, read together with the line it marks
          Enter: copy the line into the command field and go there, to run or edit it
          Backspace: back to the line you jumped from
          Shift+arrows: select; Ctrl+C: copy the selection ("Copied"); Ctrl+A: select all
          Escape: clear the selection, else go to the command field

        ## Notices
          axTerminal's questions, errors and help texts appear inside the window, in place of the terminal.
          Tab moves between the text, a field and the buttons. Enter chooses the main button. Escape closes the notice. In a notice's text, h, 1 to 6 and Ctrl+F work as in this one
        """;

    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axterminal/user-guide.md");
}
