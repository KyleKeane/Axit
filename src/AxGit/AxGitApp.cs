using AxGit.Core;
using Axit.Forms;

namespace AxGit;

/// <summary>
/// AxGit's entry point inside Axit (docs/axit/SPEC.md AX-1.7; SPEC.md AG-1.3): <c>Axit.exe git [&lt;folder or file&gt;]</c>
/// calls <see cref="Run"/>, one call into the bundle's <see cref="AppHost"/> with the one path of the command line,
/// the settings and <see cref="GitForm"/>.
/// </summary>
public static class AxGitApp
{
    public const string Name = "AxGit";

    public const string Usage = """
        Usage: Axit git [<folder or file>]
               axgit [<folder or file>]  the same, as the installed console command

          <folder or file>  AxGit opens the git repository that holds it; without one, the last repository opened
          --help, --version this text, or the version
        """;

    public static void Run(string[] args) =>
        AppHost.Run(Name, "axgit", Usage, args, arguments =>
        {
            if (!AppHost.OnePath(Name, "folder or file", Usage, arguments, out var path))
            {
                return null;
            }

            var settings = GitSettings.Load(GitSettings.DefaultPath, out var settingsError);
            return new GitForm(path, settings, settingsError);
        });
}
