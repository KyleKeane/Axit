using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AxStart.Core;
using Axit.Core;

namespace AxStart;

/// <summary>
/// The shell's Applications folder (SPEC.md AS-2.1, AS-D1), the list the Start menu's All apps shows, desktop programs
/// and packaged apps together: read through <c>IShellItem</c> and <c>IEnumShellItems</c> on a thread of its own in the
/// single-threaded apartment the shell wants, and a program started from its parsing name through the shell, as the
/// Start menu starts it. The interfaces are declared in vtable order with <c>PreserveSig</c>.
/// </summary>
public static class AppsFolder
{
    private const uint SIGDN_NORMALDISPLAY = 0;
    private const uint SIGDN_PARENTRELATIVEPARSING = 0x80018001;
    private const int ERROR_CANCELLED = 1223;

    private static readonly Guid BHID_EnumItems = new("94f60519-2850-4924-aa5a-d15e84868039");
    private static readonly Guid IID_IShellItem = new("43826d1e-e718-42ee-bc55-a1e261c37bfe");
    private static readonly Guid IID_IEnumShellItems = new("70629033-e363-4a28-a567-0db78006e6d7");

    /// <summary>The programs, one per name, read on a thread of their own; the window awaits it and stays responsive.</summary>
    public static Task<IReadOnlyList<ProgramEntry>> ReadAsync()
    {
        var done = new TaskCompletionSource<IReadOnlyList<ProgramEntry>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                done.SetResult(Read());
            }
            catch (Exception ex)
            {
                done.SetException(ex);
            }
        })
        {
            IsBackground = true,
            Name = "axStart programs",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return done.Task;
    }

    private static IReadOnlyList<ProgramEntry> Read()
    {
        var programs = new List<ProgramEntry>();
        Check(SHCreateItemFromParsingName("shell:AppsFolder", 0, IID_IShellItem, out var folder));
        try
        {
            Check(folder.BindToHandler(0, BHID_EnumItems, IID_IEnumShellItems, out var enumerator));
            var items = (IEnumShellItems)enumerator;
            try
            {
                while (items.Next(1, out var item, out var fetched) == 0 && fetched == 1)
                {
                    try
                    {
                        if (Name(item, SIGDN_NORMALDISPLAY) is { } name && Name(item, SIGDN_PARENTRELATIVEPARSING) is { } parsing)
                        {
                            programs.Add(ProgramEntry.From(name, parsing, KnownFolder));
                        }
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(item);
                    }
                }
            }
            finally
            {
                Marshal.ReleaseComObject(items);
            }
        }
        finally
        {
            Marshal.ReleaseComObject(folder);
        }

        return ProgramMatch.Distinct(programs);
    }

    /// <summary>
    /// Starts the program (AS-2.3): through <c>shell:AppsFolder\&lt;parsing name&gt;</c>, so that a packaged app starts
    /// as the Start menu starts it; as administrator through the verb <c>runas</c>, on the program file itself when its
    /// path is known. Null when it started (or the user said no to Windows' prompt), the reason otherwise.
    /// </summary>
    public static string? Start(ProgramEntry program, bool administrator)
    {
        if (administrator && program.Kind == ProgramKind.Store)
        {
            return "Cannot start as administrator";
        }

        var file = administrator && program.Target is { } target ? target : @"shell:AppsFolder\" + program.ParsingName;
        try
        {
            var info = new ProcessStartInfo(file) { UseShellExecute = true };
            if (administrator)
            {
                info.Verb = "runas";
            }

            if (program.Target is { } path && Path.GetDirectoryName(path) is { } folder && Directory.Exists(folder))
            {
                info.WorkingDirectory = folder;
            }

            Process.Start(info)?.Dispose();
            return null;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ERROR_CANCELLED)
        {
            // The user said no to Windows' administrator prompt: nothing to report.
            return null;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            Log.Error("A program could not be started", ex);
            return $"Could not start {program.Name}: {ex.Message}";
        }
    }

    private static string? KnownFolder(Guid id)
    {
        try
        {
            return SHGetKnownFolderPath(id, 0, 0);
        }
        catch (Exception ex) when (ex is COMException or ArgumentException or FileNotFoundException)
        {
            return null;
        }
    }

    private static string? Name(IShellItem item, uint kind)
    {
        if (item.GetDisplayName(kind, out var text) != 0 || text == 0)
        {
            return null;
        }

        try
        {
            return Marshal.PtrToStringUni(text);
        }
        finally
        {
            Marshal.FreeCoTaskMem(text);
        }
    }

    private static void Check(int hresult)
    {
        if (hresult < 0)
        {
            Marshal.ThrowExceptionForHR(hresult);
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHCreateItemFromParsingName(string path, nint bindContext, in Guid riid, out IShellItem item);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    [return: MarshalAs(UnmanagedType.LPWStr)]
    private static extern string SHGetKnownFolderPath(in Guid id, uint flags, nint token);

    [ComImport]
    [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        [PreserveSig]
        int BindToHandler(nint bindContext, in Guid handler, in Guid riid, [MarshalAs(UnmanagedType.Interface)] out object result);

        [PreserveSig]
        int GetParent(out IShellItem parent);

        [PreserveSig]
        int GetDisplayName(uint kind, out nint name);

        [PreserveSig]
        int GetAttributes(uint mask, out uint attributes);

        [PreserveSig]
        int Compare(IShellItem other, uint hint, out int order);
    }

    [ComImport]
    [Guid("70629033-e363-4a28-a567-0db78006e6d7")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IEnumShellItems
    {
        [PreserveSig]
        int Next(uint count, out IShellItem item, out uint fetched);

        [PreserveSig]
        int Skip(uint count);

        [PreserveSig]
        int Reset();

        [PreserveSig]
        int Clone(out IEnumShellItems copy);
    }
}
