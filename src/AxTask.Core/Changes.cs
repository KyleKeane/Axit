using Axit.Core;

namespace AxTask.Core;

/// <summary>
/// One part of a change on disk: a file written (<see cref="Before"/> null when it was made, <see cref="After"/> null
/// when it was removed), or a file or folder moved from <see cref="Path"/> to <see cref="To"/>. A file made in a folder
/// the change made too (a new list's <c>list.json</c>) has <see cref="MadeFolder"/>, so that its undo takes the folder
/// away again when nothing else is in it.
/// </summary>
public sealed record FileChange(string Path, string? Before, string? After, string? To = null, bool MadeFolder = false)
{
    public bool IsMove => To is not null;

    public static FileChange Write(string path, string? before, string after) => new(path, before, after);

    /// <summary>A file made in a new folder: the folder made with it, and gone with it on undo.</summary>
    public static FileChange WriteInNewFolder(string path, string after) => new(path, null, after, MadeFolder: true);

    public static FileChange Move(string from, string to) => new(from, null, null, to);
}

/// <summary>A change the user made, in its parts, with its words for Ctrl+Z ("Undone: Done: Fix the tap"; IA-5.4).</summary>
public sealed record Step(IReadOnlyList<FileChange> Parts, string Said);

/// <summary>
/// The changes on disk and their undo (IA-5.4, IA-5.5). A step is applied part by part; undone in reverse, and only
/// when every part is still as the step left it, so that an edit made since in axJSON or by a sync is never lost.
/// </summary>
public static class Changes
{
    /// <summary>Applies the parts in order: writes (folders made as needed) and moves.</summary>
    public static void Apply(IReadOnlyList<FileChange> parts)
    {
        foreach (var part in parts)
        {
            if (part.IsMove)
            {
                MovePath(part.Path, part.To!);
            }
            else
            {
                WriteOrRemove(part.Path, part.After);
            }
        }
    }

    /// <summary>Why the step cannot be undone now ("Fix the tap changed on disk"); null when it can.</summary>
    public static string? CheckUndo(Step step)
    {
        // The state each path should be in, walking the parts forward: what the last part to touch it left.
        var expected = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var moved = new List<FileChange>();
        foreach (var part in step.Parts)
        {
            if (part.IsMove)
            {
                moved.Add(part);
                if (expected.Remove(part.Path, out var text))
                {
                    expected[part.To!] = text;
                }
            }
            else
            {
                expected[part.Path] = part.After;
            }
        }

        foreach (var (path, text) in expected)
        {
            var now = ReadOrNull(path);
            if (now != text)
            {
                return $"{System.IO.Path.GetFileNameWithoutExtension(path)} changed on disk";
            }
        }

        foreach (var move in moved)
        {
            if (!File.Exists(move.To) && !Directory.Exists(move.To))
            {
                return $"{System.IO.Path.GetFileNameWithoutExtension(move.To)} is not there any more";
            }

            if (File.Exists(move.Path) || Directory.Exists(move.Path))
            {
                return $"There is a {System.IO.Path.GetFileNameWithoutExtension(move.Path)} again where it was";
            }
        }

        return null;
    }

    /// <summary>Undoes the step, last part first; <see cref="CheckUndo"/> first.</summary>
    public static void Undo(Step step)
    {
        for (var i = step.Parts.Count - 1; i >= 0; i--)
        {
            var part = step.Parts[i];
            if (part.IsMove)
            {
                MovePath(part.To!, part.Path);
            }
            else
            {
                WriteOrRemove(part.Path, part.Before);
                if (part.MadeFolder && System.IO.Path.GetDirectoryName(part.Path) is { } folder && Directory.Exists(folder)
                    && !Directory.EnumerateFileSystemEntries(folder).Any())
                {
                    Directory.Delete(folder);
                }
            }
        }
    }

    /// <summary>The file's text; null when there is no such file.</summary>
    public static string? ReadOrNull(string path) =>
        File.Exists(path) ? TextDocument.Read(path).Text : Directory.Exists(path) ? string.Empty : null;

    private static void WriteOrRemove(string path, string? text)
    {
        if (text is null)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return;
        }

        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        var document = File.Exists(path) ? TextDocument.Read(path) : TextDocument.Empty();
        document.Write(path, text);
    }

    private static void MovePath(string from, string to)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(to)!);
        if (Directory.Exists(from))
        {
            Directory.Move(from, to);
        }
        else
        {
            File.Move(from, to);
        }
    }
}
