using Axit.Core;
using Axit.Forms;

namespace AxClaude;

internal sealed partial class MainForm
{
    // ---- project folder ----

    /// <summary>Session → Show current folder in → AxGit (FR-8.2): a new AxGit window on the repository that holds the project folder.</summary>
    private void OpenInAxGit()
    {
        if (_folder is null)
        {
            Announce("No project folder", true);
            return;
        }

        AxitMenu.Start(Dispatch.AxGit, _folder);
    }

    /// <summary>Session → Show current folder in → AxTerminal: a new AxTerminal window with a shell in the project folder (docs/axterminal/SPEC.md AT-1.5).</summary>
    private void OpenInAxTerminal()
    {
        if (_folder is null)
        {
            Announce("No project folder", true);
            return;
        }

        AxitMenu.Start(Dispatch.AxTerminal, _folder);
    }

    /// <summary>Session → Show current folder in → AxFile: a new AxFile window listing the project folder (docs/axfile/SPEC.md AF-1.3).</summary>
    private void OpenInAxFile()
    {
        if (_folder is null)
        {
            Announce("No project folder", true);
            return;
        }

        AxitMenu.Start(Dispatch.AxFile, _folder);
    }

    /// <summary>Session → Show current folder in → File Explorer, Ctrl+W (FR-8.2): the folder in File Explorer, one of the system's own windows (D23).</summary>
    private void ShowInExplorer()
    {
        if (_folder is null)
        {
            Announce("No project folder", true);
            return;
        }

        if (!Directory.Exists(_folder))
        {
            Announce("Folder missing", true);
            return;
        }

        OpenInExplorer(_folder);
    }

    /// <summary>The bundle's folder picker (one of the few separate windows, D23), on the folder or the user's profile; null when it was cancelled.</summary>
    private string? PickFolder(string? start) =>
        ChooseFolder("Choose the project folder for Claude", start ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

    /// <summary>Makes the folder the project folder: the title and the status bar follow, and the default arguments apply there again (FR-9.1).</summary>
    private void SetFolder(string folder)
    {
        if (_folder is { } old && !string.Equals(old, folder, StringComparison.OrdinalIgnoreCase))
        {
            // AX-9.4: the folder left behind joins the bundle's Recently closed list.
            RecentlyClosed.Remember("AxClaude", old);
        }

        _folder = folder;
        _nothingToContinue = false;
        UpdateFolderUi();
    }

    /// <summary>Session → Open folder… (FR-8.3): the picker, then Claude restarts in the chosen folder.</summary>
    private void OpenFolder()
    {
        if (PickFolder(_folder) is { } folder)
        {
            SwitchFolder(folder);
        }
    }

    /// <summary>The project folder, for the bundle's Recently closed list (AX-9.4).</summary>
    protected override string? FolderInHand => _folder;

    /// <summary>Restarts Claude in another folder (Open folder, Open recent folder), with a system line and an announcement.</summary>
    protected override void SwitchFolder(string folder)
    {
        if (!Directory.Exists(folder))
        {
            Fail("The folder no longer exists: " + folder);
            return;
        }

        SetFolder(folder);
        Announce($"Folder {PathName.Of(folder)}", false);
        // Another folder is another conversation (FR-8.3): the window is cleared, and what Claude prints there, a
        // replay with its blocks when the folder has one, arrives as at the first start of the app.
        Relaunch($"Project folder changed to {folder}", clearConversation: true);
    }
}
