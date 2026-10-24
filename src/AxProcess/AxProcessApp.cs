using AxProcess.Core;
using Axit.Forms;

namespace AxProcess;

/// <summary>
/// axProcess's entry point inside Axit (docs/axit/SPEC.md AX-1.7; SPEC.md PR-1.2): <c>Axit.exe process</c> calls
/// <see cref="Run"/>, one call into the bundle's <see cref="AppHost"/> with the settings and <see cref="ProcessForm"/>.
/// It takes no path; any argument is reported with the usage text.
/// </summary>
public static class AxProcessApp
{
    public const string Name = "axProcess";

    public const string Usage = """
        Usage: Axit process
               axprocess         the same, as the installed console command

          --help, --version  this text, or the version
        """;

    public static void Run(string[] args) =>
        AppHost.Run(Name, "axprocess", Usage, args, arguments =>
        {
            if (arguments.Length > 0)
            {
                AppHost.BadCommandLine(Name, $"axProcess takes no arguments: {string.Join(" ", arguments)}", Usage);
                return null;
            }

            var settings = ProcessSettings.Load(ProcessSettings.DefaultPath, out var settingsError);
            return new ProcessForm(settings, settingsError);
        });
}
