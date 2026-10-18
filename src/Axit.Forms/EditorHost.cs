using Axit.Core;

namespace Axit.Forms;

/// <summary>
/// Starts a text editor app (docs/axit/SPEC.md AX-1.7; docs/axdown/SPEC.md AD-1.4): <see cref="AppHost.Run"/> with
/// the one file of the command line, the app's settings and <see cref="TextEditorForm"/> on the app's
/// <see cref="EditorApp"/>. <c>AxDownApp.Run</c> and <c>AxJSONApp.Run</c> are one call each into here.
/// </summary>
public static class EditorHost
{
    public static void Run(EditorApp app, string usage, string[] args) =>
        AppHost.Run(app.Name, app.SettingsName, usage, args, arguments =>
        {
            if (!AppHost.OnePath(app.Name, "file", usage, arguments, out var path))
            {
                return null;
            }

            var settings = EditorSettings.Load(AppPaths.SettingsFile(app.SettingsName), out var settingsError);
            return new TextEditorForm(app, path, settings, settingsError);
        });
}
