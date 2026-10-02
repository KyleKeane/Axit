using AxClaude.Core;
using Axit.Core;

namespace AxClaude;

/// <summary>
/// AxClaude's entry point inside Axit (docs/axit/SPEC.md AX-1.7): <c>Axit.exe claude [arguments]</c> calls
/// <see cref="Run"/>. The app owns its crash handling, its command line, its settings and its window; the
/// executable's Program owns only the dispatch.
/// </summary>
public static class AxClaudeApp
{
    public static void Run(string[] args)
    {
        // FR-13.1: an exception in a UI handler is logged and reported instead of silently ending the process.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ReportCrash(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log.Error("Unhandled exception on a background thread", e.ExceptionObject as Exception);

        // The program path tells a development build from the installed one when both write to the same log.
        Log.Info($"AxClaude {BundleInfo.Version} starting on .NET {Environment.Version} from {Environment.ProcessPath ?? "(unknown)"} as process {Environment.ProcessId}, arguments: {string.Join(" ", args)}");

        if (args.Length == 1 && args[0] is "--help" or "-h" or "-?" or "/?")
        {
            MessageBox.Show(StartupOptions.Usage, "AxClaude", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (args.Length == 1 && args[0] == "--version")
        {
            MessageBox.Show($"AxClaude {BundleInfo.Version}", "AxClaude", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        StartupOptions options;
        try
        {
            options = StartupOptions.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Log.Error("Bad command line: " + ex.Message);
            MessageBox.Show(ex.Message, "AxClaude", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var settings = AppSettings.Load(AppSettings.DefaultPath, out var settingsError);
        if (settingsError is not null)
        {
            Log.Error("Settings not readable, using defaults: " + settingsError);
        }

        Application.Run(new MainForm(options, settings, settingsError));
        Log.Info("AxClaude exiting");
    }

    /// <summary>FR-13.4: the report goes into the window as a notice; a message box only when there is no window to put it in.</summary>
    private static void ReportCrash(Exception exception)
    {
        Log.Error("Unhandled exception", exception);
        var message = $"AxClaude hit an unexpected error and will keep running if it can.\n\n{exception.GetType().Name}: {exception.Message}\n\nDetails were written to {Log.FilePath}";
        var window = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
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

        MessageBox.Show(message, "AxClaude error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
