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

    // ---- Windows' own ways with the entry (AF-4.7): the system's dialogs and menu, like the folder picker (AX-7.2) ----

    /// <summary>Item → Open with…: the Windows dialog that picks a program for the file, and runs it.</summary>
    private void OpenWith()
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

        if (Shell.OpenWith(Handle, entry.Path) is { } reason)
        {
            Failed("Open with failed", reason);
        }
    }

    /// <summary>Item → Properties (Alt+Enter): the Windows Properties sheet of the entry, a file or a folder.</summary>
    private void ShowProperties()
    {
        if (CurrentEntry() is not { } entry)
        {
            Announce(_folder is null ? "No folder" : "Nothing selected", true);
            return;
        }

        if (Shell.Properties(Handle, entry.Path) is { } reason)
        {
            Failed("Properties failed", reason);
        }
    }

    /// <summary>Item → More from Windows…: the shell's own menu for the entry, under it in the list, once the Item menu has closed.</summary>
    private void ShowWindowsMenu()
    {
        if (CurrentEntry() is not { } entry)
        {
            Announce(_folder is null ? "No folder" : "Nothing selected", true);
            return;
        }

        var bounds = _list.GetItemRectangle(_list.SelectedIndex);
        var at = _list.PointToScreen(new Point(bounds.Left, bounds.Bottom));
        // Posted, so that the Item menu has closed and the list has the focus back before the shell's menu takes over.
        BeginInvoke(() =>
        {
            if (IsDisposed)
            {
                return;
            }

            try
            {
                if (Shell.ShowMenu(Handle, entry.Path, at) is { } reason)
                {
                    Failed("Windows menu failed", reason);
                }
            }
            catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException or InvalidCastException or System.ComponentModel.Win32Exception)
            {
                Log.Error("The Windows menu failed", ex);
                Failed("Windows menu failed", ex.Message);
            }
        });
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
