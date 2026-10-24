using System.Reflection;
using Axit.Core;

namespace AxProcess;

/// <summary>The texts axProcess shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>Axit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md PR-6.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2).</summary>
    public const string Shortcuts = """
        In this text: h and Shift+H move by heading, 1 to 6 by level. Ctrl+F finds, F3 and Shift+F3 the next and the previous match

        ## In every window of Axit
          F1: this list
          Ctrl+Shift+P: the command palette: type part of a command's name, Down and Up through the matches, Enter runs it
          Ctrl+F: find an item in the list. F3 and Shift+F3: next or previous match
          Ctrl+Plus, Ctrl+Minus: bigger or smaller text
          Alt: the menu. Alt+F4: close axProcess
          Alt+A: the Axit menu: the command palette, a new window of any app, the files and folders closed lately, the open Axit windows, most recent first (Alt+A, O, 1 is the one you were in last) and by type, Close all windows, Check for updates, and About

        ## axProcess
          Ctrl+1, Ctrl+2, Ctrl+3: the Processes, Services and Startup view of the list; pressed on the view already shown, the key says its name
          Tab, Shift+Tab: the next or the previous view, round from Startup to Processes
          F5: read the view again; nothing changes by itself
          Ctrl+Q: the summary: the processes, the memory in use, the processor
          In the list
            Enter: the item's details, with what can be done to it: end a process, start or stop a service
            Delete: end the process, after a question
            Shift+F10 or the Applications key: the menu of the view, Process, Service or Startup
            Up or Home on the first item, Down or End on the last: the edge sound, since nothing moves
          Ctrl+E: show the program file in axFile
          Ctrl+C: copy the line
          Ctrl+S: nothing to save here; axProcess says so
          View, Sort processes by: memory, processor or name

        ## Notices
          The details and the questions appear inside the window, in place of the list.
          Tab moves between the text and the buttons. Enter chooses the main button. Escape closes the notice
        """;

    /// <summary>docs/axprocess/user-guide.md, embedded by AxProcess.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axprocess/user-guide.md");
}
