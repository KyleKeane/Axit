using AxFile.Core;
using Axit.Core;
using Axit.Forms;

namespace AxFile;

/// <summary>
/// AxFile's entry point inside Axit (docs/axit/SPEC.md AX-1.7; SPEC.md AF-1.2): <c>Axit.exe file [&lt;folder or file&gt;]</c>
/// calls <see cref="Run"/>, one call into the bundle's <see cref="AppHost"/> with the one path of the command line,
/// the settings and <see cref="FileForm"/>.
/// </summary>
public static class AxFileApp
{
    public const string Name = "AxFile";

    public const string Usage = """
        Usage: Axit file [<folder or file>]
               axfile [<folder or file>]  the same, as the installed console command

          <folder or file>  the folder to list; a file opens its folder with the file selected. Without one, the last folder opened
          --help, --version this text, or the version
        """;

    public static void Run(string[] args) =>
        AppHost.Run(Name, "axfile", Usage, args, arguments =>
        {
            if (!AppHost.OnePath(Name, "folder or file", Usage, arguments, out var path))
            {
                return null;
            }

            // A file manager holds no folder open (AF-1.4): a folder that is a process's current directory cannot
            // be renamed or moved while the process runs, and the apps started from here inherit this one's.
            // The path of the command line is already full (OnePath), so nothing relative is lost.
            try
            {
                Environment.CurrentDirectory = AppContext.BaseDirectory;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                Log.Error("The working directory could not be moved off the user's folders", ex);
            }

            var settings = FileSettings.Load(FileSettings.DefaultPath, out var settingsError);
            return new FileForm(path, settings, settingsError);
        });
}
