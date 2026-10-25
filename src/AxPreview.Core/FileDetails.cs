using System.Runtime.InteropServices;
using Axit.Core;

namespace AxPreview.Core;

/// <summary>
/// The Details list (SPEC.md AP-6.2): three groups under a heading line each, every line "Name: value". The File group
/// comes from the file system and the shell's type name; the Contents group is the renderer's (AP-7); the Windows
/// group is every other property Windows' property system holds for the file, by its display name and formatted as
/// Windows formats it, read on a thread of its own with a time limit (AP-6.4). Tested in
/// <c>tests/AxPreview.Tests/FileDetailsTests</c>.
/// </summary>
public static class FileDetails
{
    public const string FileHeading = "File";
    public const string ContentsHeading = "Contents";
    public const string WindowsHeading = "Windows' details";
    public const string TooSlow = "Windows' details took too long";

    /// <summary>How long Windows' property handlers may take before the group is given up (a file on a slow share).</summary>
    public static readonly TimeSpan WindowsTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Every line of the list, the three groups in order; a group with nothing in it is left out with its heading.</summary>
    public static IReadOnlyList<string> Lines(string path, IReadOnlyList<string> contents)
    {
        var lines = new List<string> { FileHeading };
        lines.AddRange(FileGroup(path));
        if (contents.Count > 0)
        {
            lines.Add(ContentsHeading);
            lines.AddRange(contents);
        }

        var windows = WindowsGroup(path, WindowsTimeout);
        if (windows.Count > 0)
        {
            lines.Add(WindowsHeading);
            lines.AddRange(windows);
        }

        return lines;
    }

    /// <summary>"12 KB, 12,345 bytes"; a size under a kilobyte as its bytes alone.</summary>
    public static string Size(long bytes) => bytes < 1024 ? FileSize.Text(bytes) : $"{FileSize.Text(bytes)}, {bytes:N0} bytes";

    /// <summary>The value of a line, what Ctrl+C copies (AP-6.3): the text after the first ": ", or the line when it has none.</summary>
    public static string ValueOf(string line) => line.IndexOf(": ", StringComparison.Ordinal) is var at and >= 0 ? line[(at + 2)..] : line;

    /// <summary>The name of a line, the text before the first ": ", or null for a heading line.</summary>
    public static string? NameOf(string line) => line.IndexOf(": ", StringComparison.Ordinal) is var at and >= 0 ? line[..at] : null;

    /// <summary>The File group (AP-6.2, 1): name, folder, size, type, the three dates and the attributes worth saying.</summary>
    public static IReadOnlyList<string> FileGroup(string path)
    {
        var info = new FileInfo(path);
        var lines = new List<string>
        {
            $"Name: {info.Name}",
            $"Folder: {info.DirectoryName}",
            $"Size: {Size(info.Length)}",
            $"Type: {TypeName(path)}",
            $"Modified: {info.LastWriteTime:yyyy-MM-dd HH:mm}",
            $"Created: {info.CreationTime:yyyy-MM-dd HH:mm}",
            $"Accessed: {info.LastAccessTime:yyyy-MM-dd HH:mm}",
        };
        if (Attributes(info.Attributes) is { Length: > 0 } attributes)
        {
            lines.Add($"Attributes: {attributes}");
        }

        return lines;
    }

    /// <summary>The attributes a reader wants to know of, in words; Archive and Normal, set on nearly every file, are not said.</summary>
    public static string Attributes(FileAttributes attributes)
    {
        var words = new List<string>();
        void Say(FileAttributes flag, string word)
        {
            if (attributes.HasFlag(flag))
            {
                words.Add(word);
            }
        }

        Say(FileAttributes.ReadOnly, "Read-only");
        Say(FileAttributes.Hidden, "Hidden");
        Say(FileAttributes.System, "System");
        Say(FileAttributes.Compressed, "Compressed");
        Say(FileAttributes.Encrypted, "Encrypted");
        Say(FileAttributes.Offline, "Offline");
        Say(FileAttributes.ReparsePoint, "a link");
        return string.Join(", ", words);
    }

    /// <summary>Windows' name for the file's kind with its extension, "Markdown File (.md)"; the extension alone when the shell has no name.</summary>
    public static string TypeName(string path)
    {
        var extension = Path.GetExtension(path);
        var info = new ShFileInfo();
        var name = OperatingSystem.IsWindows()
            && SHGetFileInfoW(path, FileAttributeNormal, ref info, (uint)Marshal.SizeOf<ShFileInfo>(), ShgfiTypeName | ShgfiUseFileAttributes) != 0
            && info.TypeName.Length > 0 ? info.TypeName : null;
        return (name, extension.Length > 0) switch
        {
            (not null, true) => $"{name} ({extension})",
            (not null, false) => name,
            (null, true) => extension,
            _ => "no extension",
        };
    }

    /// <summary>The Windows group (AP-6.2, 3), on an STA thread of its own, the property handlers being COM objects; <see cref="TooSlow"/> alone when it takes longer than <paramref name="timeout"/>.</summary>
    public static IReadOnlyList<string> WindowsGroup(string path, TimeSpan timeout)
    {
        if (!OperatingSystem.IsWindows())
        {
            return [];
        }

        IReadOnlyList<string> result = [];
        var thread = new Thread(() =>
        {
            try
            {
                result = ReadProperties(path);
            }
            catch (Exception ex) when (ex is COMException or InvalidCastException or ArgumentException)
            {
                // A kind without a property handler, or a file the handler refuses: the group is left out.
            }
        })
        { IsBackground = true, Name = "axPreview properties" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return thread.Join(timeout) ? result : [TooSlow];
    }

    /// <summary>The properties already said in the File group, or said there differently, by their canonical names.</summary>
    private static readonly HashSet<string> Listed = new(StringComparer.OrdinalIgnoreCase)
    {
        "System.ItemNameDisplay", "System.ItemName", "System.FileName", "System.ItemNameDisplayWithoutExtension",
        "System.ItemPathDisplay", "System.ItemPathDisplayNarrow", "System.ItemFolderPathDisplay",
        "System.ItemFolderPathDisplayNarrow", "System.ItemFolderNameDisplay", "System.ParsingName", "System.ParsingPath",
        "System.Size", "System.DateModified", "System.DateCreated", "System.DateAccessed", "System.ItemTypeText",
        "System.ItemType", "System.FileExtension", "System.FileAttributes", "System.FileAttributesDisplay",
        "System.SFGAOFlags", "System.ThumbnailCacheId", "System.ZoneIdentifier",
        // Ids rather than words: a media class's GUID (measured on an .ico, 2026-10-24).
        "System.Media.ClassPrimaryID", "System.Media.ClassSecondaryID",
    };

    private const int MaxValue = 500;

    private static IReadOnlyList<string> ReadProperties(string path)
    {
        var lines = new List<string>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Name", "Folder", "Size", "Type", "Modified", "Created", "Accessed", "Attributes" };
        var storeId = typeof(IPropertyStore).GUID;
        if (SHGetPropertyStoreFromParsingName(path, IntPtr.Zero, GpsBestEffort, ref storeId, out var store) != 0 || store is null)
        {
            return lines;
        }

        try
        {
            store.GetCount(out var count);
            var descriptionId = typeof(IPropertyDescription).GUID;
            for (uint i = 0; i < count; i++)
            {
                if (store.GetAt(i, out var key) != 0
                    || PSGetPropertyDescription(ref key, ref descriptionId, out var description) != 0 || description is null)
                {
                    continue;
                }

                try
                {
                    if (description.GetCanonicalName(out var canonical) != 0 || Listed.Contains(canonical)
                        || description.GetTypeFlags(PdtfIsViewable, out var flags) != 0 || (flags & PdtfIsViewable) == 0
                        || description.GetDisplayName(out var label) != 0 || string.IsNullOrWhiteSpace(label) || !names.Add(label))
                    {
                        continue;
                    }

                    var value = new PropVariant();
                    try
                    {
                        if (store.GetValue(ref key, ref value) != 0 || value.Type == 0
                            || PSFormatForDisplayAlloc(ref key, ref value, 0, out var text) != 0 || string.IsNullOrWhiteSpace(text))
                        {
                            continue;
                        }

                        // Windows puts direction marks around some values (dimensions, dates); they are not spoken.
                        text = text.Replace("‎", string.Empty).Replace("‏", string.Empty).Replace("‪", string.Empty).Replace("‬", string.Empty).Trim();
                        lines.Add($"{label}: {(text.Length > MaxValue ? text[..MaxValue] + "…" : text)}");
                    }
                    finally
                    {
                        PropVariantClear(ref value);
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(description);
                }
            }
        }
        finally
        {
            Marshal.ReleaseComObject(store);
        }

        return lines;
    }

    // ---- the shell's type name and the property system, by P/Invoke ----

    private const uint FileAttributeNormal = 0x80;
    private const uint ShgfiTypeName = 0x400;
    private const uint ShgfiUseFileAttributes = 0x10;
    private const int GpsBestEffort = 0x40;
    private const uint PdtfIsViewable = 0x200;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShFileInfo
    {
        public IntPtr Icon;
        public int IconIndex;
        public uint Attributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string DisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string TypeName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PropertyKey
    {
        public Guid FormatId;
        public uint PropertyId;
    }

    /// <summary>A PROPVARIANT: its type and its two pointer-sized words, which the property system fills and <c>PropVariantClear</c> frees.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct PropVariant
    {
        public ushort Type;
        public ushort Reserved1;
        public ushort Reserved2;
        public ushort Reserved3;
        public IntPtr Value1;
        public IntPtr Value2;
    }

    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        [PreserveSig] int GetCount(out uint count);
        [PreserveSig] int GetAt(uint index, out PropertyKey key);
        [PreserveSig] int GetValue(ref PropertyKey key, ref PropVariant value);
    }

    /// <summary>The first methods of IPropertyDescription, in vtable order, as far as the ones used.</summary>
    [ComImport, Guid("6F79D558-3E96-4549-A1D1-7D75D2288814"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyDescription
    {
        [PreserveSig] int GetPropertyKey(out PropertyKey key);
        [PreserveSig] int GetCanonicalName([MarshalAs(UnmanagedType.LPWStr)] out string name);
        [PreserveSig] int GetPropertyType(out ushort type);
        [PreserveSig] int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string name);
        [PreserveSig] int GetEditInvitation([MarshalAs(UnmanagedType.LPWStr)] out string invitation);
        [PreserveSig] int GetTypeFlags(uint mask, out uint flags);
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfoW(string path, uint attributes, ref ShFileInfo info, uint size, uint flags);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHGetPropertyStoreFromParsingName(string path, IntPtr bindContext, int flags, ref Guid id, [MarshalAs(UnmanagedType.Interface)] out IPropertyStore? store);

    [DllImport("propsys.dll")]
    private static extern int PSGetPropertyDescription(ref PropertyKey key, ref Guid id, [MarshalAs(UnmanagedType.Interface)] out IPropertyDescription? description);

    [DllImport("propsys.dll", CharSet = CharSet.Unicode)]
    private static extern int PSFormatForDisplayAlloc(ref PropertyKey key, ref PropVariant value, int flags, [MarshalAs(UnmanagedType.LPWStr)] out string text);

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PropVariant value);
}
