using AxTerminal.Core;
using Axit.Forms;

namespace AxTerminal;

/// <summary>
/// AxTerminal's entry point inside Axit (docs/axit/SPEC.md AX-1.7; SPEC.md AT-1.5): <c>Axit.exe terminal [&lt;folder&gt;]</c>
/// calls <see cref="Run"/>, one call into the bundle's <see cref="AppHost"/> with the one folder of the command line,
/// the settings and <see cref="TerminalForm"/>.
/// </summary>
public static class AxTerminalApp
{
    public const string Name = "AxTerminal";

    public const string Usage = """
        Usage: Axit terminal [<folder>]
               axterminal [<folder>]  the same, as the installed console command

          <folder>          the folder the shell starts in; without one, the current folder
          --help, --version this text, or the version
        """;

    public static void Run(string[] args) =>
        AppHost.Run(Name, "axterminal", Usage, args, arguments =>
        {
            if (!AppHost.OnePath(Name, "folder", Usage, arguments, out var path))
            {
                return null;
            }

            var settings = TerminalSettings.Load(TerminalSettings.DefaultPath, out var settingsError);
            return new TerminalForm(path, settings, settingsError);
        });
}
