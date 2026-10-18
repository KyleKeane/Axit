using Axit.Core;

namespace Axit.Forms;

/// <summary>
/// The "Open recent …" submenu every window has (AxClaude FR-8.1, AxDown AD-4.1, AxGit AG-6.1; the Axit menu's
/// shape, AX-9.5): the paths that still exist as "1. name (path)", with the number as the mnemonic, or one enabled
/// item that says there are none, since a disabled item is skipped by the keyboard and NVDA would read nothing.
/// </summary>
public static class RecentMenu
{
    /// <param name="paths">Newest first; those that do not exist any more are left out.</param>
    /// <param name="exists">Whether a path is still there: <see cref="File.Exists"/> for files, <see cref="Directory.Exists"/> for folders.</param>
    /// <param name="name">The short name shown before the path.</param>
    /// <param name="noneLabel">The one item when nothing is left, with its mnemonic ("&amp;No recent files").</param>
    public static void Fill(ToolStripMenuItem menu, IEnumerable<string> paths, Func<string, bool> exists, Func<string, string> name, string noneLabel, Action<string> open)
    {
        menu.DropDownItems.Clear();
        var targets = paths.Where(exists).ToList();
        if (targets.Count == 0)
        {
            menu.DropDownItems.Add(new ToolStripMenuItem(noneLabel));
            return;
        }

        for (var i = 0; i < targets.Count; i++)
        {
            var target = targets[i];
            menu.DropDownItems.Add(new ToolStripMenuItem(AxitWindows.Label(i, $"{name(target)} ({target})"), null, (_, _) => open(target)));
        }
    }

    /// <summary>A folder's name for a label or a title: the last part of the path, or the path itself for a drive's root.</summary>
    public static string FolderName(string path)
    {
        var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(path));
        return name.Length == 0 ? path : name;
    }
}
