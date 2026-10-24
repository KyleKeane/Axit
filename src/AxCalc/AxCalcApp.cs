using AxCalc.Core;
using Axit.Forms;

namespace AxCalc;

/// <summary>
/// axCalc's entry point inside Axit (docs/axit/SPEC.md AX-1.7; SPEC.md AC-1.2): <c>Axit.exe calc</c> calls
/// <see cref="Run"/>, one call into the bundle's <see cref="AppHost"/> with the settings and <see cref="CalcForm"/>.
/// A calculator takes no path; any argument is reported with the usage text.
/// </summary>
public static class AxCalcApp
{
    public const string Name = "axCalc";

    public const string Usage = """
        Usage: Axit calc
               axcalc            the same, as the installed console command

          --help, --version  this text, or the version
        """;

    public static void Run(string[] args) =>
        AppHost.Run(Name, "axcalc", Usage, args, arguments =>
        {
            if (arguments.Length > 0)
            {
                AppHost.BadCommandLine(Name, $"axCalc takes no arguments: {string.Join(" ", arguments)}", Usage);
                return null;
            }

            var settings = CalcSettings.Load(CalcSettings.DefaultPath, out var settingsError);
            return new CalcForm(settings, settingsError);
        });
}
