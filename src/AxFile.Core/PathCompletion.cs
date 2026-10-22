namespace AxFile.Core;

/// <summary>One completion of a typed path: the text the field takes ("C:\Windows\", with a backslash after a folder) and the label said with it ("Windows, folder").</summary>
public sealed record Completion(string Text, string Label);

/// <summary>
/// Completion in the Go to folder field (SPEC.md AF-3.3, AF-D12): the typed text split into a folder and the start of a
/// name, and the entries of that folder whose names start with it, in the list's order. Pure apart from the read.
/// </summary>
public static class PathCompletion
{
    /// <summary>
    /// The folder a typed text completes in and the start of a name: "C:\Us" is C:\ and "Us"; environment variables are
    /// expanded; a text without a backslash is a name in <paramref name="shown"/>, the folder shown, and a relative
    /// folder is taken from there too. A null folder means the drives, for an empty text or at This PC.
    /// </summary>
    public static (string? Folder, string Start) Split(string typed, string? shown)
    {
        var text = Environment.ExpandEnvironmentVariables(typed.Trim().Trim('"')).Replace('/', '\\');
        var here = shown is null || FolderListing.IsThisPC(shown) ? null : shown;
        var cut = text.LastIndexOf('\\');
        if (cut < 0)
        {
            // "C:" alone is the start of a drive's root, as is any text at This PC.
            return (text.Length == 2 && text[1] == ':' ? null : here, text);
        }

        var folder = text[..(cut + 1)];
        if (!Path.IsPathRooted(folder))
        {
            if (here is null)
            {
                return (null, text);
            }

            folder = Path.Combine(here, folder);
        }

        return (folder, text[(cut + 1)..]);
    }

    /// <summary>
    /// The completions of <paramref name="start"/> in <paramref name="folder"/>: its entries whose names start with it,
    /// any case, folders first in natural order, hidden and period-named entries as the list shows them (AF-2.2); for a
    /// null folder, the ready drives whose root starts with it ("C:\", "Local Disk (C:), drive"). Empty when the folder
    /// cannot be read.
    /// </summary>
    public static IReadOnlyList<Completion> Candidates(string? folder, string start, bool showHidden, bool showDotFiles)
    {
        if (folder is null)
        {
            return FolderListing.Drives()
                .Where(drive => drive.Path.StartsWith(start, StringComparison.OrdinalIgnoreCase))
                .Select(drive => new Completion(drive.Path, drive.Label))
                .ToList();
        }

        IReadOnlyList<Entry> entries;
        try
        {
            entries = FolderListing.Read(folder, showHidden, showDotFiles).Entries;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return [];
        }

        var prefix = folder.EndsWith('\\') ? folder : folder + "\\";
        return entries
            .Where(entry => entry.Name.StartsWith(start, StringComparison.OrdinalIgnoreCase))
            .Select(entry => new Completion(prefix + entry.Name + (entry.IsFolder ? "\\" : string.Empty), entry.Label))
            .ToList();
    }

    /// <summary>"Windows, folder, 2 of 5": what a completion says when the field takes it.</summary>
    public static string Spoken(Completion completion, int index, int count) => $"{completion.Label}, {index + 1} of {count}";
}
