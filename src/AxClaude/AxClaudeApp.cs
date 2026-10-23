using AxClaude.Core;
using Axit.Core;
using Axit.Forms;

namespace AxClaude;

/// <summary>
/// axClaude's entry point inside Axit (docs/axit/SPEC.md AX-1.7): <c>Axit.exe claude [arguments]</c> calls
/// <see cref="Run"/>, one call into the bundle's <see cref="AppHost"/> with axClaude's own command line
/// (<see cref="StartupOptions"/>), the take-over of the 1.x settings file, the settings and <see cref="MainForm"/>.
/// </summary>
public static class AxClaudeApp
{
    public static void Run(string[] args) =>
        AppHost.Run("axClaude", "axclaude", StartupOptions.Usage, args, arguments =>
        {
            StartupOptions options;
            try
            {
                options = StartupOptions.Parse(arguments);
            }
            catch (ArgumentException ex)
            {
                AppHost.BadCommandLine("axClaude", ex.Message, usage: null);
                return null;
            }

            // AX-5.2: the first start under Axit takes over the settings of axClaude 1.x.
            try
            {
                AppSettings.TakeOverOldFile();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.Error("The 1.x settings file could not be copied; starting with defaults", ex);
            }

            var settings = AppSettings.Load(AppSettings.DefaultPath, out var settingsError);
            return new MainForm(options, settings, settingsError);
        });
}
