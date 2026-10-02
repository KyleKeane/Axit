using System.Runtime.InteropServices;
using AxClaude;
using Axit.Core;

namespace Axit;

/// <summary>
/// The one entry point of the bundle (docs/axit/SPEC.md AX-1). It decides which app the command line means and hands
/// the rest of the arguments to that app's <c>Run</c>; everything after, including the crash handling and the log,
/// belongs to the app. A message box is used only here, where no window exists yet.
/// </summary>
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        var launch = Dispatch.Decide(args);
        if (launch.Error is not null)
        {
            MessageBox.Show(launch.Error + "\n\n" + Dispatch.Usage, "Axit", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        switch (launch.App)
        {
            case Dispatch.AxClaude:
                SetAppId("Axit.AxClaude");
                AxClaudeApp.Run(launch.Arguments);
                break;
            case Dispatch.AxDown:
                MessageBox.Show("AxDown is not part of this build yet.", "Axit", MessageBoxButtons.OK, MessageBoxIcon.Information);
                break;
            default:
                // --help or --version for the bundle itself (AX-1.4).
                var text = launch.Arguments[0] == "--version" ? $"Axit {BundleInfo.Version}" : $"Axit {BundleInfo.Version}\n\n{Dispatch.Usage}";
                MessageBox.Show(text, "Axit", MessageBoxButtons.OK, MessageBoxIcon.Information);
                break;
        }
    }

    /// <summary>AX-1.6: the taskbar groups windows by this id, so each app gets its own, set before any window exists.</summary>
    private static void SetAppId(string id)
    {
        try
        {
            SetCurrentProcessExplicitAppUserModelID(id);
        }
        catch (Exception)
        {
            // Cosmetic: the apps still run, grouped under one taskbar button.
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);
}
