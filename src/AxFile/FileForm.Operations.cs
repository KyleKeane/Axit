using System.Diagnostics;
using AxFile.Core;
using Axit.Core;
using Axit.Forms;

namespace AxFile;

/// <summary>The four operations (SPEC.md AF-4), each one notice or one announcement, and the ways out of the window (AF-1.3, AF-3.2).</summary>
public sealed partial class FileForm
{
    private const int ErrorNoAssociation = 1155;

    /// <summary>Item → New folder… (Ctrl+Shift+N, AF-4.1): the name in a notice, the folder made and selected, "Created <name>".</summary>
    private void NewFolder()
    {
        if (_folder is not { } folder)
        {
            Announce("No folder", true);
            return;
        }

        ShowNotice(new Notice("New folder", string.Empty,
        [
            new OverlayChoice("&Create", () =>
            {
                var name = Overlay.InputText.Trim();
                if (FileOperations.CheckName(name) is { } problem)
                {
                    Announce(problem, true);
                    return;
                }

                Do(async () =>
                {
                    try
                    {
                        await Task.Run(() => FileOperations.CreateFolder(folder, name));
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        Failed("New folder failed", FileOperations.Message(ex));
                        return;
                    }

                    if (await ShowFolderAsync(folder, name, remember: false))
                    {
                        Announce($"Created {name}", true);
                    }
                });
            }, IsDefault: true),
            new OverlayChoice("C&ancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("Name:", "Name", string.Empty, "Type a name"),
        });
    }

    /// <summary>Item → Rename… (F2, AF-4.2): the new name in a notice, the part before the extension selected, "Renamed to <name>".</summary>
    private void Rename()
    {
        if (_folder is not { } folder || CurrentEntry() is not { } entry)
        {
            Announce(_folder is null ? "No folder" : "Nothing selected", true);
            return;
        }

        var stem = entry.IsFolder ? entry.Name.Length : Path.GetFileNameWithoutExtension(entry.Name).Length;
        ShowNotice(new Notice($"Rename {entry.Name}", string.Empty,
        [
            new OverlayChoice("&Rename", () =>
            {
                var name = Overlay.InputText.Trim();
                if (FileOperations.CheckName(name) is { } problem)
                {
                    Announce(problem, true);
                    return;
                }

                Do(async () =>
                {
                    try
                    {
                        await Task.Run(() => FileOperations.Rename(entry.Path, name));
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        Failed("Rename failed", FileOperations.Message(ex));
                        return;
                    }

                    if (await ShowFolderAsync(folder, name, remember: false))
                    {
                        Announce($"Renamed to {name}", true);
                    }
                });
            }, IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("New name:", "New name", entry.Name, "Type a name") { SelectLength = stem },
        });
    }

    /// <summary>Item → Delete… (Delete, AF-4.3): a question, then the shell sends the entry to the Recycle Bin; "Deleted <name>", the next entry selected.</summary>
    private void Delete()
    {
        if (_folder is not { } folder || CurrentEntry() is not { } entry)
        {
            Announce(_folder is null ? "No folder" : "Nothing selected", true);
            return;
        }

        var index = _list.SelectedIndex;
        var next = index + 1 < _entries.Count ? _entries[index + 1].Name : index > 0 ? _entries[index - 1].Name : null;
        ShowNotice(new Notice($"Delete {entry.Name}?", entry.IsFolder ? "It goes to the Recycle Bin with everything in it." : "It goes to the Recycle Bin.",
        [
            new OverlayChoice("&Delete", () => Do(async () =>
            {
                if (await FileOperations.DeleteToRecycleBinAsync(entry.Path) is { } reason)
                {
                    Failed("Delete failed", reason);
                    return;
                }

                if (await ShowFolderAsync(folder, next, remember: false))
                {
                    Announce($"Deleted {entry.Name}", true);
                }
            }), IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ]));
    }

    /// <summary>Item → Copy path (Ctrl+Shift+C, AF-4.4): the entry's full path to the clipboard, "Copied path".</summary>
    private void CopyPath()
    {
        if (CurrentEntry() is not { } entry)
        {
            Announce(_folder is null ? "No folder" : "Nothing selected", true);
            return;
        }

        try
        {
            Clipboard.SetText(entry.Path);
            Announce("Copied path", true);
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.ExternalException or InvalidOperationException)
        {
            Failed("Copy failed", ex.Message);
        }
    }

    // ---- the ways out (AF-1.3, AF-3.2) ----

    /// <summary>Folder → Show current folder in → File Explorer (Ctrl+W): the folder opened in File Explorer, one of the system's own windows.</summary>
    private void ShowFolderInExplorer()
    {
        if (_folder is null)
        {
            Announce("No folder", true);
            return;
        }

        OpenInExplorer(_folder);
    }

    /// <summary>Item → Show in File Explorer (Ctrl+Shift+W): File Explorer on the folder with the entry selected, a folder entry too.</summary>
    private void ShowItemInExplorer()
    {
        if (CurrentEntry() is not { } entry)
        {
            Announce(_folder is null ? "No folder" : "Nothing selected", true);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{entry.Path}\"") { UseShellExecute = false });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            Fail("File Explorer could not be started: " + ex.Message);
        }
    }

    /// <summary>Folder → Show current folder in → AxClaude, AxGit, AxTerminal (AF-1.3): a new window of that app on the folder shown.</summary>
    private void OpenIn(string verb)
    {
        if (_folder is null)
        {
            Announce("No folder", true);
            return;
        }

        AxitMenu.Start(verb, _folder);
    }

    /// <summary>Item → Open in editor (Ctrl+E): the file in AxJSON for .json and AxDown for anything else, whatever Enter would do with it.</summary>
    private void OpenInEditor()
    {
        if (CurrentEntry() is not { } entry)
        {
            Announce(_folder is null ? "No folder" : "Nothing selected", true);
            return;
        }

        if (entry.IsFolder)
        {
            Announce("Not a file", true);
            return;
        }

        var verb = FolderListing.WayOf(entry) == OpenWay.AxJSON ? Dispatch.AxJSON : Dispatch.AxDown;
        AxitMenu.Start(verb, entry.Path);
    }

    /// <summary>Enter on any other file (AF-3.2): the program Windows has for it; when there is none, the way to read it anyway.</summary>
    private void OpenWithWindows(Entry entry)
    {
        try
        {
            Process.Start(new ProcessStartInfo(entry.Path) { UseShellExecute = true })?.Dispose();
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == ErrorNoAssociation)
        {
            Announce($"No program opens {Path.GetExtension(entry.Name)}. Ctrl+E opens it as text", true);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            Failed("Open failed", ex.Message);
        }
    }
}
