namespace AxStart.Core;

/// <summary>What kind of program an entry of the Applications folder is (SPEC.md AS-2.1, AS-3.3).</summary>
public enum ProgramKind
{
    /// <summary>A shortcut to a program file, whose path is known: "C:\Program Files\Notepad++\notepad++.exe".</summary>
    Desktop,

    /// <summary>A packaged app, named by its application user model id ("Microsoft.WindowsCalculator_8wekyb3d8bbwe!App").</summary>
    Store,

    /// <summary>A desktop program the shell names by an id of its own, with no path to show.</summary>
    Other,
}

/// <summary>
/// One program of the shell's Applications folder (SPEC.md AS-2.1): the name the Start menu shows, the parsing name
/// the shell starts it by (<c>shell:AppsFolder\&lt;parsing name&gt;</c>), its kind, and the program file's path when the
/// parsing name holds one.
/// </summary>
public sealed record ProgramEntry(string Name, string ParsingName, ProgramKind Kind, string? Target)
{
    /// <summary>
    /// An entry from what the shell gives for it. A packaged app's parsing name has a '!' between the package and the
    /// app; a desktop program's is a path, either whole or under a known folder written as its id
    /// ("{6D809377-6AF0-444B-8957-A3773F02200E}\Notepad++\notepad++.exe"), which <paramref name="knownFolder"/> turns
    /// into the folder's path (null when it does not know the id). Anything else is an id the shell keeps to itself.
    /// </summary>
    public static ProgramEntry From(string name, string parsingName, Func<Guid, string?> knownFolder)
    {
        if (parsingName.Contains('!'))
        {
            return new ProgramEntry(name, parsingName, ProgramKind.Store, null);
        }

        if (parsingName.Length > 2 && parsingName[1] == ':' && parsingName[2] == '\\')
        {
            return new ProgramEntry(name, parsingName, ProgramKind.Desktop, parsingName);
        }

        if (parsingName.StartsWith('{') && parsingName.IndexOf('}') is var close and > 0
            && Guid.TryParse(parsingName[..(close + 1)], out var id) && knownFolder(id) is { } folder)
        {
            var rest = parsingName[(close + 1)..].TrimStart('\\');
            return new ProgramEntry(name, parsingName, ProgramKind.Desktop, Path.Combine(folder, rest));
        }

        return new ProgramEntry(name, parsingName, ProgramKind.Other, null);
    }

    /// <summary>Ctrl+I (AS-3.3): "Notepad++, desktop program, C:\…\notepad++.exe", "Calculator, Store app", "Chrome, program".</summary>
    public string Information => Kind switch
    {
        ProgramKind.Desktop => $"{Name}, desktop program, {Target}",
        ProgramKind.Store => $"{Name}, Store app",
        _ => $"{Name}, program",
    };
}
