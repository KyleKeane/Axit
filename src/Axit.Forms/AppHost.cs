using Axit.Core;

namespace Axit.Forms;

/// <summary>
/// Starts an app's window (docs/axit/SPEC.md AX-1.7): the log, the crash handling, the start line in the log,
/// <c>--help</c> and <c>--version</c>, then the window the app makes from its arguments, and the exit line. Every
/// app's <c>Run</c> is one call into <see cref="Run"/>; what differs, the usage text and how the arguments become a
/// window, comes as parameters. The message boxes here are the only ones allowed in the bundle: no window exists
/// yet when they show (AX-7.2).
/// </summary>
public static class AppHost
{
    /// <param name="window">Makes the window from the arguments once <c>--help</c> and <c>--version</c> are answered; null when the app reported a bad command line itself (<see cref="BadCommandLine"/>).</param>
    public static void Run(string name, string logName, string usage, string[] args, Func<string[], Form?> window)
    {
        Log.UseApp(logName);

        // An exception in a UI handler is logged and reported instead of silently ending the process.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ReportCrash(name, e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log.Error("Unhandled exception on a background thread", e.ExceptionObject as Exception);

        // The program path tells a development build from the installed one when both write to the same log.
        Log.Info($"{name} {BundleInfo.Version} starting on .NET {Environment.Version} from {Environment.ProcessPath ?? "(unknown)"} as process {Environment.ProcessId}, arguments: {string.Join(" ", args)}");

        if (args.Length == 1 && Dispatch.IsHelpFlag(args[0]))
        {
            MessageBox.Show(usage, name, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (args.Length == 1 && args[0] == "--version")
        {
            MessageBox.Show($"{name}, Axit {BundleInfo.Version}", name, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (window(args) is not { } form)
        {
            return;
        }

        Application.Run(form);
        Log.Info($"{name} exiting");
    }

    /// <summary>
    /// The one path an app takes on its command line (a file, a folder): the full path, or null for none. False,
    /// with the command line reported, when there is more than one argument or an option the app does not know.
    /// </summary>
    public static bool OnePath(string name, string what, string usage, string[] args, out string? path)
    {
        path = null;
        if (args.Length > 1 || (args.Length == 1 && args[0].StartsWith("--", StringComparison.Ordinal)))
        {
            BadCommandLine(name, args.Length > 1 ? $"{name} opens one {what}." : $"Unknown option {args[0]}.", usage);
            return false;
        }

        path = args.Length == 1 ? Path.GetFullPath(args[0]) : null;
        return true;
    }

    /// <summary>A command line the app cannot start on: logged and shown, with the usage text when there is one.</summary>
    public static void BadCommandLine(string name, string message, string? usage)
    {
        Log.Error("Bad command line: " + message);
        MessageBox.Show(usage is null ? message : message + "\n\n" + usage, name, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    /// <summary>The report goes into the window as a notice; a message box only when there is no window to put it in.</summary>
    private static void ReportCrash(string name, Exception exception)
    {
        Log.Error("Unhandled exception", exception);
        var message = $"{name} hit an unexpected error and will keep running if it can.\n\n{exception.GetType().Name}: {exception.Message}\n\nDetails were written to {Log.FilePath}";
        var window = Application.OpenForms.OfType<AxitForm>().FirstOrDefault();
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

        MessageBox.Show(message, $"{name} error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
