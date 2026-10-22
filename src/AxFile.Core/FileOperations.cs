using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace AxFile.Core;

/// <summary>
/// The operations (SPEC.md AF-4): a new folder, a rename, a delete to the Recycle Bin, a copy or a move into a
/// folder with the free names chosen first ("name - 2"), and the check of a typed name. The delete, the copy and the
/// move are the shell's own operations, with undo allowed and no window of theirs, run on a thread of their own since
/// the shell wants a single-threaded apartment. Each throws, or answers with a message, and the window turns that into
/// one announcement, "Failed: …" (AF-4.5).
/// </summary>
public static class FileOperations
{
    /// <summary>Why a typed name cannot be used, or null when it can (AF-4.1, AF-4.2).</summary>
    public static string? CheckName(string name)
    {
        if (name.Trim().Length == 0)
        {
            return "Type a name";
        }

        if (name is "." or "..")
        {
            return "That name is not allowed";
        }

        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return "A name cannot contain \\ / : * ? \" < > |";
        }

        return null;
    }

    /// <summary>
    /// The name among <paramref name="names"/> that reads the same as <paramref name="name"/> without being the same
    /// name (AF-4.2): the two differ only in case, in a trailing or doubled space, in the kind of space, in an
    /// invisible character (a zero-width space, a joiner, a mark), or in an accent written as one or two characters.
    /// Windows keeps both as two files; a screen reader hears one name twice. Null when none does.
    /// </summary>
    public static string? Lookalike(string name, IEnumerable<string> names)
    {
        var key = ReadingKey(name);
        return names.FirstOrDefault(other => !string.Equals(other, name.Trim(), StringComparison.OrdinalIgnoreCase) && ReadingKey(other) == key);
    }

    /// <summary>How a name is heard: composed, upper-cased, every kind of white space one space, invisible characters dropped, trimmed.</summary>
    private static string ReadingKey(string name)
    {
        var composed = name.Normalize(NormalizationForm.FormKC);
        var text = new StringBuilder(composed.Length);
        var space = false;
        foreach (var c in composed)
        {
            if (char.IsWhiteSpace(c))
            {
                space = true;
                continue;
            }

            var category = char.GetUnicodeCategory(c);
            if (category is UnicodeCategory.Format or UnicodeCategory.Control or UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark)
            {
                continue;
            }

            if (space && text.Length > 0)
            {
                text.Append(' ');
            }

            space = false;
            text.Append(char.ToUpperInvariant(c));
        }

        return text.ToString();
    }

    /// <summary>Makes the folder inside <paramref name="folder"/> and returns its path; a name already taken is an error.</summary>
    public static string CreateFolder(string folder, string name)
    {
        var path = Path.Combine(folder, name.Trim());
        if (Directory.Exists(path) || File.Exists(path))
        {
            throw new IOException($"There is already an entry named {name.Trim()}");
        }

        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>Makes an empty file in <paramref name="folder"/>, "New file.txt", or "New file - 2.txt" when that is taken (AF-4.1), and returns its path.</summary>
    public static string CreateFile(string folder)
    {
        var path = FreeName(folder, "New file", ".txt");
        File.WriteAllBytes(path, []);
        return path;
    }

    /// <summary>Renames the file or folder at <paramref name="path"/> inside its folder and returns the new path; a name already taken is an error, a changed case is a rename.</summary>
    public static string Rename(string path, string newName)
    {
        newName = newName.Trim();
        var folder = Path.GetDirectoryName(path) ?? throw new IOException("The entry has no folder");
        var target = Path.Combine(folder, newName);
        var sameName = string.Equals(Path.GetFileName(path), newName, StringComparison.Ordinal);
        if (sameName)
        {
            return path;
        }

        var caseOnly = string.Equals(Path.GetFileName(path), newName, StringComparison.OrdinalIgnoreCase);
        if (!caseOnly && (Directory.Exists(target) || File.Exists(target)))
        {
            throw new IOException($"There is already an entry named {newName}");
        }

        if (Directory.Exists(path))
        {
            Directory.Move(path, target);
        }
        else
        {
            File.Move(path, target);
        }

        return target;
    }

    /// <summary>Sends files and folders to the Recycle Bin (AF-4.3), so that they can come back, in one operation. Null when it succeeded, the reason otherwise.</summary>
    public static Task<string?> DeleteToRecycleBinAsync(IReadOnlyList<string> paths) =>
        RunShellOperationAsync(FO_DELETE, paths, null, "The deletion");

    /// <summary>"name.ext", or "name - 2.ext", "name - 3.ext" when taken: the first path in <paramref name="folder"/> that nothing has, nor <paramref name="taken"/> holds (AF-4.8).</summary>
    public static string FreeName(string folder, string stem, string extension, IEnumerable<string>? taken = null)
    {
        for (var n = 1; ; n++)
        {
            var name = n == 1 ? stem + extension : $"{stem} - {n}{extension}";
            var path = Path.Combine(folder, name);
            if (!File.Exists(path) && !Directory.Exists(path) && !(taken?.Contains(path, StringComparer.OrdinalIgnoreCase) ?? false))
            {
                return path;
            }
        }
    }

    /// <summary>Where each source lands when copied or moved into <paramref name="folder"/> (AF-4.8): under its own name, or "name - 2" when that is taken by the folder or by an earlier source.</summary>
    public static IReadOnlyList<string> Destinations(IReadOnlyList<string> sources, string folder)
    {
        var destinations = new List<string>();
        foreach (var source in sources)
        {
            var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(source));
            var isFolder = Directory.Exists(source);
            var path = Path.Combine(folder, name);
            if (File.Exists(path) || Directory.Exists(path) || destinations.Contains(path, StringComparer.OrdinalIgnoreCase))
            {
                path = isFolder
                    ? FreeName(folder, name, string.Empty, destinations)
                    : FreeName(folder, Path.GetFileNameWithoutExtension(name), Path.GetExtension(name), destinations);
            }

            destinations.Add(path);
        }

        return destinations;
    }

    /// <summary>
    /// Copies, or moves, files and folders to <paramref name="destinations"/>, one per source, from
    /// <see cref="Destinations"/> (AF-4.8), the shell's way, so that the move or copy can be undone from File
    /// Explorer. Null when it succeeded, the reason otherwise.
    /// </summary>
    public static Task<string?> CopyOrMoveAsync(IReadOnlyList<string> sources, IReadOnlyList<string> destinations, bool move) =>
        RunShellOperationAsync(move ? FO_MOVE : FO_COPY, sources, destinations, move ? "The move" : "The copy");

    /// <summary>One line for the user from what the file system threw.</summary>
    /// <summary>
    /// Why an operation cannot be taken back now (AF-4.14), or null when it can: an entry it made or moved is gone or
    /// renamed since ("Not undone: <name> has changed"), a new folder or file has been filled meanwhile, or the place
    /// an entry would go back to is taken.
    /// </summary>
    public static string? CheckUndo(Operation operation)
    {
        foreach (var path in operation.After)
        {
            if (!Exists(path))
            {
                return $"Not undone: {Path.GetFileName(path)} has changed";
            }
        }

        switch (operation.Kind)
        {
            case OperationKind.NewFolder when Directory.EnumerateFileSystemEntries(operation.After[0]).Any():
                return $"Not undone: {Path.GetFileName(operation.After[0])} has files now";
            case OperationKind.NewFile when new FileInfo(operation.After[0]).Length > 0:
                return $"Not undone: {Path.GetFileName(operation.After[0])} has content now";
            case OperationKind.Rename or OperationKind.Move or OperationKind.Delete:
                foreach (var path in operation.Before)
                {
                    if (Exists(path))
                    {
                        return $"Not undone: {Path.GetFileName(path)} is already here";
                    }
                }

                break;
        }

        return null;
    }

    private static bool Exists(string path) => File.Exists(path) || Directory.Exists(path);

    /// <summary>
    /// The operation taken back (AF-4.14): a rename renamed back; a new folder or file, still empty, deleted; a move
    /// moved back, the folder it made removed when empty again; a paste that copied, a compress and an extract sent
    /// to the Recycle Bin. A delete is the shell's own Restore on the Recycle Bin's item, which the window does. The
    /// reason when it fails, null when done.
    /// </summary>
    public static async Task<string?> UndoAsync(Operation operation)
    {
        try
        {
            switch (operation.Kind)
            {
                case OperationKind.Rename:
                    await Task.Run(() => Rename(operation.After[0], Path.GetFileName(operation.Before[0])));
                    return null;
                case OperationKind.NewFolder:
                    await Task.Run(() => Directory.Delete(operation.After[0]));
                    return null;
                case OperationKind.NewFile:
                    await Task.Run(() => File.Delete(operation.After[0]));
                    return null;
                case OperationKind.Move:
                    if (await CopyOrMoveAsync(operation.After, operation.Before, move: true) is { } reason)
                    {
                        return reason;
                    }

                    if (operation.MadeFolder is { } made && Directory.Exists(made) && !Directory.EnumerateFileSystemEntries(made).Any())
                    {
                        await Task.Run(() => Directory.Delete(made));
                    }

                    return null;
                case OperationKind.Copy or OperationKind.Compress or OperationKind.Extract:
                    return await DeleteToRecycleBinAsync(operation.After);
                default:
                    return "A delete is undone through the Recycle Bin";
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Message(ex);
        }
    }

    public static string Message(Exception exception) => exception switch
    {
        UnauthorizedAccessException => "Access denied",
        DirectoryNotFoundException => "The folder is gone",
        FileNotFoundException => "The file is gone",
        _ => exception.Message.Replace("\r\n", " ").Replace('\n', ' ').Trim(),
    };

    private static Task<string?> RunShellOperationAsync(uint function, IReadOnlyList<string> sources, IReadOnlyList<string>? destinations, string what) =>
        OnShellThreadAsync(() => RunShellOperation(function, sources, destinations, what));

    /// <summary>Runs shell work that wants a single-threaded apartment on a thread of its own, off the window's thread: the reason it gives back, or null.</summary>
    public static Task<string?> OnShellThreadAsync(Func<string?> work)
    {
        var done = new TaskCompletionSource<string?>();
        var thread = new Thread(() =>
        {
            try
            {
                done.SetResult(work());
            }
            catch (Exception ex)
            {
                done.SetException(ex);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        return done.Task;
    }

    private static string? RunShellOperation(uint function, IReadOnlyList<string> sources, IReadOnlyList<string>? destinations, string what)
    {
        var flags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT | FOF_NOERRORUI;
        if (destinations is not null)
        {
            // One destination path per source, chosen free beforehand, so that nothing is written over or asked.
            flags |= FOF_MULTIDESTFILES | FOF_NOCONFIRMMKDIR;
        }

        var operation = new SHFILEOPSTRUCT
        {
            wFunc = function,
            // Each list of paths ends in a second null; the marshaller adds the first.
            pFrom = string.Join("\0", sources) + "\0",
            pTo = destinations is null ? null : string.Join("\0", destinations) + "\0",
            fFlags = (ushort)flags,
        };
        var code = SHFileOperationW(ref operation);
        if (code == 0)
        {
            return operation.fAnyOperationsAborted ? $"{what} was interrupted" : null;
        }

        return ShellMessage(code, what);
    }

    /// <summary>The shell's own codes for a file operation, and Windows' message for the rest.</summary>
    private static string ShellMessage(int code, string what) => code switch
    {
        0x71 => "The source and the destination are the same",
        0x74 => $"{what} of a drive's root is not allowed",
        0x75 => $"{what} was cancelled",
        0x76 => "A folder cannot go inside itself",
        0x78 => "Access denied",
        0x7C => "The path is not valid",
        0x7E or 0x80 => "A file and a folder of that name are in the way",
        0x81 => "The name is too long",
        0x402 => $"{what} failed",
        < 0x71 => new System.ComponentModel.Win32Exception(code).Message,
        _ => $"{what} failed (code {code:X})",
    };

    private const uint FO_MOVE = 1;
    private const uint FO_COPY = 2;
    private const uint FO_DELETE = 3;
    private const ushort FOF_MULTIDESTFILES = 0x0001;
    private const ushort FOF_SILENT = 0x0004;
    private const ushort FOF_NOCONFIRMATION = 0x0010;
    private const ushort FOF_ALLOWUNDO = 0x0040;
    private const ushort FOF_NOCONFIRMMKDIR = 0x0200;
    private const ushort FOF_NOERRORUI = 0x0400;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEOPSTRUCT
    {
        public nint hwnd;
        public uint wFunc;
        public string pFrom;
        public string? pTo;
        public ushort fFlags;
        [MarshalAs(UnmanagedType.Bool)]
        public bool fAnyOperationsAborted;
        public nint hNameMappings;
        public string? lpszProgressTitle;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHFileOperationW(ref SHFILEOPSTRUCT operation);
}
