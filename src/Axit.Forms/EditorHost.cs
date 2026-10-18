using Axit.Core;

namespace Axit.Forms;

/// <summary>
/// Starts a text editor app (docs/axit/SPEC.md AX-1.7; docs/axdown/SPEC.md AD-1.4): the log, the crash handling, the
/// command line (one file, <c>--help</c>, <c>--version</c>), the settings, and <see cref="TextEditorForm"/> on the
/// app's <see cref="EditorApp"/>. <c>AxDownApp.Run</c> and <c>AxJSONApp.Run</c> are one call each into here.
/// </summary>
public static class EditorHost
{
    public static void Run(EditorApp app, string usage, string[] args)
    {
        Log.UseApp(app.SettingsName);

        // AD-11: an exception in a UI handler is logged and reported instead of silently ending the process.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ReportCrash(app, e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log.Error("Unhandled exception on a background thread", e.ExceptionObject as Exception);

        Log.Info($"{app.Name} {BundleInfo.Version} starting on .NET {Environment.Version} from {Environment.ProcessPath ?? "(unknown)"} as process {Environment.ProcessId}, arguments: {string.Join(" ", args)}");

        if (args.Length == 1 && args[0] is "--help" or "-h" or "-?" or "/?")
        {
            MessageBox.Show(usage, app.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (args.Length == 1 && args[0] == "--version")
        {
            MessageBox.Show($"{app.Name}, Axit {BundleInfo.Version}", app.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (args.Length > 1 || (args.Length == 1 && args[0].StartsWith("--", StringComparison.Ordinal)))
        {
            var message = args.Length > 1 ? $"{app.Name} opens one file." : $"Unknown option {args[0]}.";
            Log.Error("Bad command line: " + message);
            MessageBox.Show(message + "\n\n" + usage, app.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var path = args.Length == 1 ? Path.GetFullPath(args[0]) : null;
        var settingsPath = AppPaths.SettingsFile(app.SettingsName);
        var settings = EditorSettings.Load(settingsPath, out var settingsError);
        if (settingsError is not null)
        {
            Log.Error("Settings not readable, using defaults: " + settingsError);
        }

        Application.Run(new TextEditorForm(app, path, settings, settingsError));
        Log.Info($"{app.Name} exiting");
    }

    /// <summary>The report goes into the window as a notice; a message box only when there is no window to put it in.</summary>
    private static void ReportCrash(EditorApp app, Exception exception)
    {
        Log.Error("Unhandled exception", exception);
        var message = $"{app.Name} hit an unexpected error and will keep running if it can.\n\n{exception.GetType().Name}: {exception.Message}\n\nDetails were written to {Log.FilePath}";
        var window = Application.OpenForms.OfType<TextEditorForm>().FirstOrDefault();
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

        MessageBox.Show(message, $"{app.Name} error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
