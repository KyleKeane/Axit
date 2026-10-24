using System.Reflection;
using Axit.Core;

namespace AxProcess;

/// <summary>The texts axProcess shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>Axit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md PR-6.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2). The first line and the bundle's section come from <c>BundleHelp</c>.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axProcess",
        [
            "Ctrl+F: find an item in the list. F3 and Shift+F3: next or previous match",
        ]) + """
        ## axProcess
          Ctrl+1, Ctrl+2, Ctrl+3: the Processes, Services and Startup view of the list; pressed on the view already shown, the key says its name
          Tab, Shift+Tab: the next or the previous view, round from Startup to Processes
          F5: read the view again; nothing changes by itself
          Ctrl+Q: the summary: the processes, the memory in use, the processor
          Type the start of an item to jump to it, the same letter again to the next; Shift with a letter backwards
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
