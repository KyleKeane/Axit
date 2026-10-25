using System.Runtime.InteropServices;

namespace Axit.Core;

/// <summary>
/// Whether opening a file runs it (security review S-46, 2026-10-25): a program, a script, a shortcut, an installer and
/// the other kinds Windows itself treats as dangerous to open (<c>AssocIsDangerous</c>, the list File Explorer and the
/// attachment manager use). A viewer that shows a stranger's file never hands such a file to Windows to open.
/// </summary>
public static class DangerousFiles
{
    /// <summary>True when the file's kind runs when opened; a file without an extension is not one Windows runs by opening.</summary>
    public static bool RunsWhenOpened(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Length > 1 && OperatingSystem.IsWindows() && AssocIsDangerous(extension);
    }

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AssocIsDangerous(string association);
}
