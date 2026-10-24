using AxList.Core;
using Axit.Forms;

namespace AxList;

/// <summary>
/// axList's entry point inside Axit (docs/axit/SPEC.md AX-1.7; SPEC.md LI-1.3): <c>Axit.exe list [&lt;file or
/// folder&gt;]</c> calls <see cref="Run"/>, one call into the bundle's <see cref="AppHost"/> with the one path of the
/// command line, the settings and <see cref="ListForm"/>.
/// </summary>
public static class AxListApp
{
    public const string Name = "axList";

    public const string Usage = """
        Usage: Axit list [<file or folder>]
               axlist [<file or folder>]  the same, as the installed console command

          <file or folder>  a list's .md file, opened with its folder as the folder of lists, or a folder of lists;
                            without one, the folder and the list of the last run
          --help, --version this text, or the version
        """;

    public static void Run(string[] args) =>
        AppHost.Run(Name, "axlist", Usage, args, arguments =>
        {
            if (!AppHost.OnePath(Name, "file or folder", Usage, arguments, out var path))
            {
                return null;
            }

            var settings = ListSettings.Load(ListSettings.DefaultPath, out var settingsError);
            return new ListForm(path, settings, settingsError);
        });
}
