using System.Diagnostics;

namespace AxFile.Core;

/// <summary>A program or script of the user's own in Open with a custom program ▸ (SPEC.md AF-4.11): the name the menu shows and the file to run.</summary>
public sealed record UserProgram(string Name, string Path);

/// <summary>How a custom program is named when it is added and started on the entries (AF-4.11). Pure: the start itself is the window's.</summary>
public static class UserPrograms
{
    /// <summary>The name a program gets when it is added: its file name without the extension ("notepad++", "tidy").</summary>
    public static string DefaultName(string path) => System.IO.Path.GetFileNameWithoutExtension(path);

    /// <summary>Whether the file is a PowerShell script, which Windows opens in an editor rather than running it.</summary>
    public static bool IsPowerShellScript(string path) =>
        string.Equals(System.IO.Path.GetExtension(path), ".ps1", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// How the program is run on <paramref name="targets"/>, every path as one argument, in the first one's folder: a
    /// PowerShell script through <c>powershell.exe -NoProfile -ExecutionPolicy Bypass -File</c>; anything else (a
    /// program, a batch file, a script with a program registered for its type) the way the shell starts it.
    /// </summary>
    public static ProcessStartInfo StartInfo(UserProgram program, IReadOnlyList<string> targets)
    {
        var folder = targets.Count > 0 ? System.IO.Path.GetDirectoryName(targets[0]) ?? string.Empty : string.Empty;
        var script = IsPowerShellScript(program.Path);
        var info = new ProcessStartInfo(script ? "powershell.exe" : program.Path) { UseShellExecute = true, WorkingDirectory = folder };
        if (script)
        {
            info.ArgumentList.Add("-NoProfile");
            info.ArgumentList.Add("-ExecutionPolicy");
            info.ArgumentList.Add("Bypass");
            info.ArgumentList.Add("-File");
            info.ArgumentList.Add(program.Path);
        }

        foreach (var target in targets)
        {
            info.ArgumentList.Add(target);
        }

        return info;
    }
}
