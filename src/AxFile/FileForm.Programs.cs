using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using AxFile.Core;
using Axit.Core;
using Axit.Forms;

namespace AxFile;

/// <summary>axFile's window, the entry handed to other programs: the user's own programs (AF-4.11) and Windows' own ways with the entry (AF-4.7).</summary>
public sealed partial class FileForm
{
    // ---- the user's own programs (AF-4.11): a list in the settings, each run on the entries as its arguments ----

    /// <summary>
    /// Open with ▸ (AF-4.7, AF-4.11): Windows default and Choose a program…, then the user's own programs as
    /// "1. name (path)" with Add a program… and Remove a program ▸ under them; refilled each time it opens.
    /// </summary>
    private void FillOpenWith(ToolStripMenuItem menu)
    {
        menu.DropDownItems.Clear();
        menu.DropDownItems.Add(new ToolStripMenuItem("Windows &default", null, (_, _) => OpenSelectedWithWindows()));
        menu.DropDownItems.Add(new ToolStripMenuItem("&Choose a program...", null, (_, _) => OpenWith()));
        menu.DropDownItems.Add(new ToolStripSeparator());
        var programs = _settings.Programs;
        if (programs.Count == 0)
        {
            menu.DropDownItems.Add(new ToolStripMenuItem("&No programs of your own yet"));
        }

        for (var i = 0; i < programs.Count; i++)
        {
            var program = programs[i];
            menu.DropDownItems.Add(new ToolStripMenuItem(AxitWindows.Label(i, $"{program.Name} ({program.Path})"), null, (_, _) => RunProgram(program)));
        }

        menu.DropDownItems.Add(new ToolStripMenuItem("&Add a program...", null, (_, _) => AddProgram()));
        var remove = new ToolStripMenuItem("&Remove a program");
        if (programs.Count == 0)
        {
            remove.DropDownItems.Add(new ToolStripMenuItem("&No programs of your own yet"));
        }

        for (var i = 0; i < programs.Count; i++)
        {
            var program = programs[i];
            remove.DropDownItems.Add(new ToolStripMenuItem(AxitWindows.Label(i, program.Name), null, (_, _) => RemoveProgram(program)));
        }

        menu.DropDownItems.Add(remove);
    }

    /// <summary>Open with a custom program ▸ one program: run on the selected entries, or the current one, as its arguments; "Started <name>".</summary>
    private void RunProgram(UserProgram program)
    {
        var targets = Targets();
        if (targets.Count == 0)
        {
            return;
        }

        try
        {
            Process.Start(UserPrograms.StartInfo(program, targets.Select(target => target.Path).ToList()))?.Dispose();
            Announce($"Started {program.Name}", true);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
        {
            Failed($"{program.Name} failed", ex.Message);
        }
    }

    /// <summary>Add a program…: the Windows file picker on programs and scripts, then the notice "Add a program" with the field "Name" holding the file's name; "Added <name>".</summary>
    private void AddProgram()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Choose a program or script",
            Filter = "Programs and scripts (*.exe;*.cmd;*.bat;*.ps1;*.py)|*.exe;*.cmd;*.bat;*.ps1;*.py|All files (*.*)|*.*",
            CheckFileExists = true,
        };
        var chosen = ShowSystemDialog(dialog) == DialogResult.OK ? dialog.FileName : null;
        _list.Select();
        if (chosen is null)
        {
            return;
        }

        ShowNotice(new Notice("Add a program", $"The name the menu shows for {chosen}.",
        [
            new OverlayChoice("&Add", () =>
            {
                var name = Overlay.InputText.Trim();
                if (name.Length == 0)
                {
                    Announce("Type a name", true);
                    return;
                }

                _settings.Programs.Add(new UserProgram(name, chosen));
                SaveSettings();
                Announce($"Added {name}", true);
            }, IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("Name:", "Name", UserPrograms.DefaultName(chosen), "Type a name"),
        });
    }

    /// <summary>Remove a program ▸ one program: taken off the list; "Removed <name>".</summary>
    private void RemoveProgram(UserProgram program)
    {
        _settings.Programs.Remove(program);
        SaveSettings();
        Announce($"Removed {program.Name}", true);
    }

    // ---- Windows' own ways with the entry (AF-4.7): the system's dialogs and menu, like the folder picker (AX-7.2) ----

    /// <summary>Item → Open with → Choose a program…: the Windows dialog that picks a program for the file, and runs it.</summary>
    private void OpenWith()
    {
        if (!TryEntry(out var entry))
        {
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
        if (!TryEntry(out var entry))
        {
            return;
        }

        if (Shell.Properties(Handle, entry.Path) is { } reason)
        {
            Failed("Properties failed", reason);
        }
    }

    /// <summary>Shift+F10, the Applications key and a right click in the folder list (AF-4.6, AF-D13): the menu bar's Item menu, opened as Alt and I open it (the window base's <c>OpenBarMenu</c>).</summary>
    private void ShowItemMenu() => OpenBarMenu('I');

    /// <summary>Item → Windows menu…: the shell's own menu for the entry, under it in the list, once the Item menu has closed.</summary>
    private void ShowWindowsMenu()
    {
        if (!TryEntry(out var entry))
        {
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
            catch (Exception ex) when (ex is COMException or InvalidCastException or Win32Exception)
            {
                Log.Error("The Windows menu failed", ex);
                Failed("Windows menu failed", ex.Message);
            }
        });
    }
}
