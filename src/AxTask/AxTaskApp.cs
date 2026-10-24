using AxTask.Core;
using Axit.Forms;

namespace AxTask;

/// <summary>
/// axTask's entry point inside Axit (docs/axit/SPEC.md AX-1.7): <c>Axit.exe task [&lt;folder&gt;]</c> calls
/// <see cref="Run"/>, one call into the bundle's <see cref="AppHost"/> with the one path of the command line, the
/// settings and <see cref="TaskForm"/>.
/// </summary>
public static class AxTaskApp
{
    public const string Name = "axTask";

    public const string Usage = """
        Usage: Axit task [<folder or item>]
               axtask [<folder or item>]  the same, as the installed console command

          <folder or item>  the top folder of lists, or an item's .json file, opened in its list; without one, the
                            folder and the list of the last run
          --help, --version this text, or the version
        """;

    public static void Run(string[] args) =>
        AppHost.Run(Name, "axtask", Usage, args, arguments =>
        {
            if (!AppHost.OnePath(Name, "folder or item", Usage, arguments, out var path))
            {
                return null;
            }

            var settings = TaskSettings.Load(TaskSettings.DefaultPath, out var settingsError);
            return new TaskForm(path, settings, settingsError);
        });
}
