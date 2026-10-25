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
        if (StartedAsOpenWithLink(args))
        {
            return;
        }

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
    /// AX-2.3: Windows' Open with lists one program per program file, so the installer gives each app with file types a
    /// hard link of Axit.exe under its own name (axDown.exe) for its ProgID. Started under such a name, the process
    /// starts Axit.exe beside it with the same arguments and ends, so that every window runs as Axit.exe: the hand-over
    /// to an open window, the window lists and the AI tools tell this program by its path. The new process may take the
    /// foreground, which this one was given by the shell.
    /// </summary>
    private static bool StartedAsOpenWithLink(string[] args)
    {
        if (Environment.ProcessPath is not { } path
            || string.Equals(Path.GetFileName(path), "Axit.exe", StringComparison.OrdinalIgnoreCase)
            || !File.Exists(Path.Combine(AppContext.BaseDirectory, "Axit.exe")))
        {
            return false;
        }

        var start = new System.Diagnostics.ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "Axit.exe"))
        {
            UseShellExecute = false,
            WorkingDirectory = Environment.CurrentDirectory,
        };
        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        using var started = System.Diagnostics.Process.Start(start);
        if (started is not null)
        {
            // S-50 (security review, 2026-10-25): the right to the foreground for that process alone, not for any
            // program of the user's that asks in the same moment (ASFW_ANY until then).
            AllowSetForegroundWindow(started.Id);
        }

        return true;
    }

    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(int processId);

    /// <summary>
    /// AX-2.8: an update installed in place renames the program file that was running to Axit.old.exe, since a
    /// running file cannot be overwritten or deleted, and an app's Open with link the same way (axDown.old.exe). The next
    /// start removes them; one still running waits for the next. Only in the installation folder and only the names the
    /// installer makes (S-51, security review 2026-10-25): Axit.exe run from a folder of downloads would otherwise delete
    /// another program's setup.old.exe.
    /// </summary>
    private static void DeleteOldProgramFiles()
    {
        if (!string.Equals(Path.GetFullPath(AppContext.BaseDirectory).TrimEnd('\\'), Path.GetFullPath(AppPaths.InstallFolder).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            var patterns = new[] { "Axit.old*.exe" }.Concat(Dispatch.Apps.Where(app => app.OpenWith.Count > 0).Select(app => app.Name + ".old.*.exe"));
            foreach (var old in patterns.SelectMany(pattern => Directory.GetFiles(AppContext.BaseDirectory, pattern)).Distinct(StringComparer.OrdinalIgnoreCase))
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
