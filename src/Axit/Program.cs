using System.Runtime.InteropServices;
using AxClaude;
using AxPass;
using AxCalc;
using AxClip;
using AxProcess;
using AxTask;
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
        // First of all: a test run's variables read and taken out of the environment, so that nothing this process
        // starts inherits them (security audit S-11).
        AutomationEnvironment.Capture();
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

        if (Runs.TryGetValue(launch.App, out var run))
        {
            // AX-1.6: the taskbar's id is the app's project name, Axit.AxClaude and so on.
            SetAppId("Axit." + Dispatch.Of(launch.App).Project);
            run(launch.Arguments);
        }
        else if (launch.App == Dispatch.Mcp)
        {
            // No window: MCP on standard input and output until the client closes it (docs/axit/mcp.md MC-1.1).
            Axit.Forms.McpBroker.Run();
        }
        else if (launch.App == Dispatch.Listing)
        {
            // No window: the apps for the installer, which reads standard output (AX-1.4). A program without a
            // console writes to the handle it was given; started from a console with nothing to catch it, nothing shows.
            using var output = new StreamWriter(Console.OpenStandardOutput(), new System.Text.UTF8Encoding(false));
            output.WriteLine(Dispatch.ListingJson());
        }
        else
        {
            // --help or --version for the bundle itself (AX-1.4).
            var text = launch.Arguments[0] == "--version" ? $"Axit {BundleInfo.Version}" : $"Axit {BundleInfo.Version}\n\n{Dispatch.Usage}";
            MessageBox.Show(text, "Axit", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    /// <summary>
    /// Every app's entry point by its verb (AX-1.7), one line per app of <see cref="Dispatch.Apps"/>; a new app adds
    /// its line here (tools/new-app.ps1 does), and <c>InventoryTests</c> fails on an app without one.
    /// </summary>
    private static readonly Dictionary<string, Action<string[]>> Runs = new()
    {
        [Dispatch.AxClaude] = AxClaudeApp.Run,
        [Dispatch.AxDown] = AxDownApp.Run,
        [Dispatch.AxJSON] = AxJSONApp.Run,
        [Dispatch.AxGit] = AxGitApp.Run,
        [Dispatch.AxTerminal] = AxTerminalApp.Run,
        [Dispatch.AxFile] = AxFileApp.Run,
        [Dispatch.AxMedia] = AxMediaApp.Run,
        [Dispatch.AxPreview] = AxPreviewApp.Run,
        [Dispatch.AxStart] = AxStartApp.Run,
        [Dispatch.AxProcess] = AxProcessApp.Run,
        [Dispatch.AxTask] = AxTaskApp.Run,
        [Dispatch.AxPass] = AxPassApp.Run,
        [Dispatch.AxCalc] = AxCalcApp.Run,
        [Dispatch.AxClip] = AxClipApp.Run,
    };

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
