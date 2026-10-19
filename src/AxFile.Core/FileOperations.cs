using System.Runtime.InteropServices;

namespace AxFile.Core;

/// <summary>
/// The four operations (SPEC.md AF-4): a new folder, a rename, a delete to the Recycle Bin through the shell, and
/// the check of a typed name. Each throws, or answers with a message, and the window turns that into one
/// announcement, "Failed: …" (AF-4.5). The path to copy is the entry's own.
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

    /// <summary>
    /// Sends the file or folder to the Recycle Bin (AF-4.3): the shell's own operation with undo allowed and no
    /// confirmation or error window of its own, run on a thread of its own since the shell wants a single-threaded
    /// apartment. Null when it succeeded, the reason otherwise.
    /// </summary>
    public static Task<string?> DeleteToRecycleBinAsync(string path)
    {
        var done = new TaskCompletionSource<string?>();
        var thread = new Thread(() =>
        {
            try
            {
                done.SetResult(DeleteToRecycleBin(path));
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

    /// <summary>One line for the user from what the file system threw.</summary>
    public static string Message(Exception exception) => exception switch
    {
        UnauthorizedAccessException => "Access denied",
        DirectoryNotFoundException => "The folder is gone",
        FileNotFoundException => "The file is gone",
        _ => exception.Message.Replace("\r\n", " ").Replace('\n', ' ').Trim(),
    };

    private static string? DeleteToRecycleBin(string path)
    {
        var operation = new SHFILEOPSTRUCT
        {
            wFunc = FO_DELETE,
            // The list of paths ends in a second null; the marshaller adds the first.
            pFrom = path + "\0",
            fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT | FOF_NOERRORUI,
        };
        var code = SHFileOperationW(ref operation);
        if (code == 0)
        {
            return operation.fAnyOperationsAborted ? "The deletion was interrupted" : null;
        }

        return ShellMessage(code);
    }

    /// <summary>The shell's own codes for a file operation, and Windows' message for the rest.</summary>
    private static string ShellMessage(int code) => code switch
    {
        0x74 => "A drive's root cannot be deleted",
        0x75 => "The deletion was cancelled",
        0x78 => "Access denied",
        0x7C => "The path is not valid",
        0x7E or 0x80 => "A file and a folder of that name are in the way",
        0x81 => "The name is too long",
        0x402 => "The deletion failed",
        < 0x71 => new System.ComponentModel.Win32Exception(code).Message,
        _ => $"The deletion failed (code {code:X})",
    };

    private const uint FO_DELETE = 3;
    private const ushort FOF_SILENT = 0x0004;
    private const ushort FOF_NOCONFIRMATION = 0x0010;
    private const ushort FOF_ALLOWUNDO = 0x0040;
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
