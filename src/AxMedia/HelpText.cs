using System.Reflection;
using Axit.Core;

namespace AxMedia;

/// <summary>The texts axMedia shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>Axit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AM-5.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, then axMedia's own. The first line and the bundle's section come from <c>BundleHelp</c>.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axMedia",
        [
            "Ctrl+F: find a file whose name contains the text. F3 and Shift+F3: the next or the previous match",
        ]) + """
        ## The queue
          Arrow keys, Home, End, Page Up, Page Down: move through the files; type the start of a name to jump to it, Shift with a letter backwards. The one playing reads "(playing)" after its name
          Enter: play this file; the rest of the queue follows in order
          Delete: take this file out of the queue; the one playing goes on to the next
          Ctrl+I: the file's place, "3 of 12", and its position while it plays
          Ctrl+O: add files with the Windows file picker, several at once. Ctrl+Shift+O: add a folder's sound and video files. Queue, Open recent lists the files and folders added lately by number
          Queue, Clear queue: the queue emptied and the sound stopped
          Ctrl+N: a new axMedia window
          Ctrl+W: show the file in File Explorer. Queue, Show current file in, axFile: the file in its folder in axFile
          Ctrl+S: nothing to save here; axMedia says so

        ## Playing
          Space, or Ctrl+P from anywhere in the window: pause, or resume
          Ctrl+Right, Ctrl+Left: 15 seconds forward or back. Ctrl+T: go to a time, 12:40 or 1:02:10 or seconds
          Ctrl+Shift+Right, Ctrl+Shift+Left: the next or the previous file of the queue
          Ctrl+Shift+Period, Ctrl+Shift+Comma: faster or slower by a quarter, the voice's pitch kept; Play, Normal speed goes back to 1
          Ctrl+Q: what is playing, where it is, and the speed
          Play, Say each file as it starts: the next file's name said when the queue moves on, the one thing axMedia says on its own
          The volume is Windows' own: your keyboard's volume keys

        ## Notices
          axMedia's questions, errors and help texts appear inside the window, in place of the list.
          Tab moves between the text, a field and the buttons. Enter chooses the main button. Escape closes the notice. In a notice's text, h, 1 to 6 and Ctrl+F work as in this one
        """;

    /// <summary>docs/axmedia/user-guide.md, embedded by AxMedia.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axmedia/user-guide.md");
}
