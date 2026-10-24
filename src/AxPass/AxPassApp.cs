using AxPass.Core;
using Axit.Forms;

namespace AxPass;

/// <summary>
/// axPass's entry point inside Axit (docs/axit/SPEC.md AX-1.7): <c>Axit.exe pass [&lt;database&gt;]</c> calls
/// <see cref="Run"/>, one call into the bundle's <see cref="AppHost"/> with the one path of the command line, the
/// settings and <see cref="PassForm"/>.
/// </summary>
public static class AxPassApp
{
    public const string Name = "axPass";

    public const string Usage = """
        Usage: Axit pass [<database>]
               axpass [<database>]  the same, as the installed console command

          <database>        a KeePass database (.kdbx); without one, the database of the last run
          --help, --version this text, or the version
        """;

    public static void Run(string[] args) =>
        AppHost.Run(Name, "axpass", Usage, args, arguments =>
        {
            if (!AppHost.OnePath(Name, "database", Usage, arguments, out var path))
            {
                return null;
            }

            var settings = PassSettings.Load(PassSettings.DefaultPath, out var settingsError);
            return new PassForm(path, settings, settingsError);
        });
}
