using AxMedia.Core;
using Axit.Core;
using Axit.Forms;

namespace AxMedia;

/// <summary>
/// axMedia's entry point inside Axit (docs/axit/SPEC.md AX-1.7; SPEC.md AM-1.1, AM-1.2): <c>Axit.exe media [&lt;file or
/// folder&gt;...] [--add] [--new]</c> calls <see cref="Run"/>, one call into the bundle's <see cref="AppHost"/>. With
/// paths and an open axMedia window, the paths are handed to that window (<see cref="AppMessage"/>) and no window of
/// this process opens; otherwise <see cref="MediaForm"/> opens on them.
/// </summary>
public static class AxMediaApp
{
    public const string Name = "axMedia";

    public const string Usage = """
        Usage: Axit media [<file or folder>...] [--add] [--new]
               axmedia [<file or folder>...]  the same, as the installed console command

          <file>            played, with its folder's other sound and video files after it
          <folder>          its sound and video files, played from the first
          several paths     queued in that order; the first plays
          --add             the paths are added to the end of the queue and nothing starts
          --new             a window of its own, even while an axMedia window is open (the open window takes the paths otherwise)
          --help, --version this text, or the version
        """;

    public static void Run(string[] args) =>
        AppHost.Run(Name, "axmedia", Usage, args, arguments =>
        {
            var paths = new List<string>();
            var add = false;
            var fresh = false;
            foreach (var argument in arguments)
            {
                switch (argument)
                {
                    case "--add":
                        add = true;
                        break;
                    case "--new":
                        fresh = true;
                        break;
                    case var option when option.StartsWith("--", StringComparison.Ordinal):
                        AppHost.BadCommandLine(Name, $"Unknown option {option}.", Usage);
                        return null;
                    default:
                        if (!File.Exists(argument) && !Directory.Exists(argument))
                        {
                            AppHost.BadCommandLine(Name, $"There is no file or folder at {argument}.", Usage);
                            return null;
                        }

                        paths.Add(Path.GetFullPath(argument));
                        break;
                }
            }

            var verb = add ? AppMessage.Add : AppMessage.Play;
            if (paths.Count > 0 && !fresh && AppMessage.Send(Name, verb, paths))
            {
                // AM-1.2: one player; the open window has the paths and this process ends.
                return null;
            }

            var settings = MediaSettings.Load(MediaSettings.DefaultPath, out var settingsError);
            return new MediaForm(paths.Count > 0 ? new HandOver(verb, paths) : null, settings, settingsError);
        });
}
