using System.Runtime.InteropServices;
using AxClaude;
using AxProcess;
using AxDown;
using AxFile;
using AxGit;
using Axit.Core;
using AxJSON;
using AxMedia;
using AxPreview;
using AxStart;
using AxTerminal;

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
        // AX-7.7: the windows are light or dark as Windows is; a high contrast theme takes over as it always has.
        Application.SetColorMode(SystemColorMode.System);
        DeleteOldProgramFiles();

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
                SetAppId("Axit.AxDown");
                AxDownApp.Run(launch.Arguments);
                break;
            case Dispatch.AxJSON:
                SetAppId("Axit.AxJSON");
                AxJSONApp.Run(launch.Arguments);
                break;
            case Dispatch.AxGit:
                SetAppId("Axit.AxGit");
                AxGitApp.Run(launch.Arguments);
                break;
            case Dispatch.AxTerminal:
                SetAppId("Axit.AxTerminal");
                AxTerminalApp.Run(launch.Arguments);
                break;
            case Dispatch.AxFile:
                SetAppId("Axit.AxFile");
                AxFileApp.Run(launch.Arguments);
                break;
            case Dispatch.AxMedia:
                SetAppId("Axit.AxMedia");
                AxMediaApp.Run(launch.Arguments);
                break;
            case Dispatch.AxPreview:
                SetAppId("Axit.AxPreview");
                AxPreviewApp.Run(launch.Arguments);
                break;
            case Dispatch.AxStart:
                SetAppId("Axit.AxStart");
                AxStartApp.Run(launch.Arguments);
                break;
            case Dispatch.AxProcess:
                SetAppId("Axit.AxProcess");
                AxProcessApp.Run(launch.Arguments);
                break;
            default:
                // --help or --version for the bundle itself (AX-1.4).
                var text = launch.Arguments[0] == "--version" ? $"Axit {BundleInfo.Version}" : $"Axit {BundleInfo.Version}\n\n{Dispatch.Usage}";
                MessageBox.Show(text, "Axit", MessageBoxButtons.OK, MessageBoxIcon.Information);
                break;
        }
    }

    /// <summary>
    /// AX-2.8: an update installed in place renames the program file that was running to Axit.old.exe, since a
    /// running file cannot be overwritten or deleted. The next start removes it; one still running waits for the next.
    /// </summary>
    private static void DeleteOldProgramFiles()
    {
        try
        {
            foreach (var old in Directory.GetFiles(AppContext.BaseDirectory, "Axit.old*.exe"))
            {
                try
                {
                    File.Delete(old);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
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
