using AxDown.Core;
using Axit.Core;

namespace AxDown;

/// <summary>
/// AxDown's entry point inside Axit (docs/axit/SPEC.md AX-1.7; SPEC.md AD-1.4): <c>Axit.exe down [&lt;file&gt;]</c>
/// calls <see cref="Run"/>. The app owns its crash handling, its command line, its settings and its window.
/// </summary>
public static class AxDownApp
{
    public const string Usage = """
        Usage: Axit down [<file>]
               axdown [<file>]  the same, as the installed console command

          <file>            the file to open; a path that does not exist yet is a new document with that name
          --help, --version this text, or the version
        """;

    public static void Run(string[] args)
    {
        Log.UseApp("axdown");

        // AD-11: an exception in a UI handler is logged and reported instead of silently ending the process.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ReportCrash(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log.Error("Unhandled exception on a background thread", e.ExceptionObject as Exception);

        Log.Info($"AxDown {BundleInfo.Version} starting on .NET {Environment.Version} from {Environment.ProcessPath ?? "(unknown)"} as process {Environment.ProcessId}, arguments: {string.Join(" ", args)}");

        if (args.Length == 1 && args[0] is "--help" or "-h" or "-?" or "/?")
        {
            MessageBox.Show(Usage, "AxDown", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (args.Length == 1 && args[0] == "--version")
        {
            MessageBox.Show($"AxDown, Axit {BundleInfo.Version}", "AxDown", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (args.Length > 1 || (args.Length == 1 && args[0].StartsWith("--", StringComparison.Ordinal)))
        {
            var message = args.Length > 1 ? "AxDown opens one file." : $"Unknown option {args[0]}.";
            Log.Error("Bad command line: " + message);
            MessageBox.Show(message + "\n\n" + Usage, "AxDown", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var path = args.Length == 1 ? Path.GetFullPath(args[0]) : null;
        var settings = EditorSettings.Load(EditorSettings.DefaultPath, out var settingsError);
        if (settingsError is not null)
        {
            Log.Error("Settings not readable, using defaults: " + settingsError);
        }

        Application.Run(new EditorForm(path, settings, settingsError));
        Log.Info("AxDown exiting");
    }

    /// <summary>The report goes into the window as a notice; a message box only when there is no window to put it in.</summary>
    private static void ReportCrash(Exception exception)
    {
        Log.Error("Unhandled exception", exception);
        var message = $"AxDown hit an unexpected error and will keep running if it can.\n\n{exception.GetType().Name}: {exception.Message}\n\nDetails were written to {Log.FilePath}";
        var window = Application.OpenForms.OfType<EditorForm>().FirstOrDefault();
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

        MessageBox.Show(message, "AxDown error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
