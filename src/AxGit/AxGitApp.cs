using AxGit.Core;
using Axit.Core;

namespace AxGit;

/// <summary>
/// AxGit's entry point inside Axit (docs/axit/SPEC.md AX-1.7; SPEC.md AG-1.3): <c>Axit.exe git [&lt;folder or file&gt;]</c>
/// calls <see cref="Run"/>, which starts the log, the crash handling, reads the command line and the settings, and
/// runs <see cref="GitForm"/>.
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

    public static void Run(string[] args)
    {
        Log.UseApp("axgit");

        // An exception in a UI handler is logged and reported instead of silently ending the process.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ReportCrash(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log.Error("Unhandled exception on a background thread", e.ExceptionObject as Exception);

        Log.Info($"{Name} {BundleInfo.Version} starting on .NET {Environment.Version} from {Environment.ProcessPath ?? "(unknown)"} as process {Environment.ProcessId}, arguments: {string.Join(" ", args)}");

        if (args.Length == 1 && args[0] is "--help" or "-h" or "-?" or "/?")
        {
            MessageBox.Show(Usage, Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (args.Length == 1 && args[0] == "--version")
        {
            MessageBox.Show($"{Name}, Axit {BundleInfo.Version}", Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (args.Length > 1 || (args.Length == 1 && args[0].StartsWith("--", StringComparison.Ordinal)))
        {
            var message = args.Length > 1 ? $"{Name} opens one repository." : $"Unknown option {args[0]}.";
            Log.Error("Bad command line: " + message);
            MessageBox.Show(message + "\n\n" + Usage, Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var path = args.Length == 1 ? Path.GetFullPath(args[0]) : null;
        var settings = GitSettings.Load(GitSettings.DefaultPath, out var settingsError);
        if (settingsError is not null)
        {
            Log.Error("Settings not readable, using defaults: " + settingsError);
        }

        Application.Run(new GitForm(path, settings, settingsError));
        Log.Info($"{Name} exiting");
    }

    /// <summary>The report goes into the window as a notice; a message box only when there is no window to put it in.</summary>
    private static void ReportCrash(Exception exception)
    {
        Log.Error("Unhandled exception", exception);
        var message = $"{Name} hit an unexpected error and will keep running if it can.\n\n{exception.GetType().Name}: {exception.Message}\n\nDetails were written to {Log.FilePath}";
        var window = Application.OpenForms.OfType<GitForm>().FirstOrDefault();
        if (window is { IsDisposed: false, IsHandleCreated: true })
        {
            try
            {
                window.ShowError("Unexpected error", message);
                return;
            }
            catch (Exception ex)
            {
                Log.Error("The error could not be shown in the window", ex);
            }
        }

        MessageBox.Show(message, $"{Name} error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
