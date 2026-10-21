using System.ComponentModel;
using System.Runtime.InteropServices;

namespace AxFile;

/// <summary>
/// Windows' own ways with one entry (SPEC.md AF-4.7): the Open with dialog, the Properties sheet, and the shell's
/// context menu, File Explorer's classic menu, built through <c>IShellFolder</c> and <c>IContextMenu</c> and shown as a
/// native popup the window owns. All three are the system's own windows, like the folder picker (bundle AX-7.2). The
/// shell's extensions run inside this process while its menu is up, which is why the menu sits behind one explicit
/// item of the window's own menu rather than being the window's menu (AF-D8). Everything here runs on the UI thread:
/// the dialogs are modal to the window, the popup pumps messages while it tracks, and the window forwards the
/// popup's messages through <see cref="ForwardMenuMessage"/> so that the shell fills its submenus (Send to, Open with).
/// </summary>
internal static class Shell
{
    private const int ErrorCancelled = unchecked((int)0x800704C7);
    private const uint CmfNormal = 0;
    private const uint IdFirst = 1;
    private const uint IdLast = 0x7FFF;
    private const uint TpmReturnCmd = 0x0100;
    private const int SwShowNormal = 1;
    private const int WmKeyDown = 0x0100;
    private const int WmInitMenuPopup = 0x0117;
    private const int WmMenuChar = 0x0120;
    private const int WmMeasureItem = 0x002C;
    private const int WmDrawItem = 0x002B;
    private const int VkDown = 0x28;
    private const uint SeeMaskInvokeIdList = 0x0000000C;
    private const uint SeeMaskFlagNoUi = 0x00000400;
    private const int OaifAllowRegistration = 0x1;
    private const int OaifRegisterExt = 0x2;
    private const int OaifExec = 0x4;
    private const uint CmicMaskUnicode = 0x00004000;
    private const uint CmicMaskPtInvoke = 0x20000000;
    private const uint GcsVerbW = 0x00000004;
    private const uint VerbMax = 64;

    private static readonly Guid IidIShellFolder = new("000214E6-0000-0000-C000-000000000046");
    private static readonly Guid IidIContextMenu = new("000214E4-0000-0000-C000-000000000046");

    /// <summary>The shell's menu being tracked, for <see cref="ForwardMenuMessage"/>; null otherwise.</summary>
    private static object? s_tracking;

    /// <summary>The Windows "How do you want to open this file?" dialog for a file, and the program chosen runs it; the reason when Windows refuses, null otherwise.</summary>
    public static string? OpenWith(nint owner, string path)
    {
        var info = new OpenAsInfo { File = path, Flags = OaifAllowRegistration | OaifRegisterExt | OaifExec };
        var hr = SHOpenWithDialog(owner, in info);
        return hr < 0 && hr != ErrorCancelled ? Reason(hr) : null;
    }

    /// <summary>The Windows Properties sheet of a file or a folder, the one File Explorer shows; the reason when Windows refuses, null otherwise.</summary>
    public static string? Properties(nint owner, string path)
    {
        var info = new ShellExecuteInfo
        {
            Size = Marshal.SizeOf<ShellExecuteInfo>(),
            Mask = SeeMaskInvokeIdList | SeeMaskFlagNoUi,
            Window = owner,
            Verb = "properties",
            File = path,
            Show = SwShowNormal,
        };
        if (ShellExecuteExW(ref info))
        {
            return null;
        }

        var error = Marshal.GetLastWin32Error();
        return error == 0 ? "Windows did not show the properties" : new Win32Exception(error).Message;
    }

    /// <summary>
    /// The shell's menu for a file or a folder at a screen point, with its first item selected, as File Explorer's
    /// Shift+F10; what the user chooses runs. The reason when Windows refuses, null otherwise (closing the menu is
    /// not a refusal).
    /// </summary>
    public static string? ShowMenu(nint owner, string path, Point at)
    {
        var built = Build(owner, path, out var reason);
        if (built is not { } parts)
        {
            return reason;
        }

        try
        {
            // Selects the first item for the keyboard before the menu's loop takes over, as the shell does for Shift+F10.
            s_tracking = parts.Handler;
            PostMessageW(owner, WmKeyDown, VkDown, 0);
            var id = TrackPopupMenuEx(parts.Menu, TpmReturnCmd, at.X, at.Y, owner, 0);
            s_tracking = null;
            if (id == 0)
            {
                return null;
            }

            var info = new InvokeInfo
            {
                Size = Marshal.SizeOf<InvokeInfo>(),
                Mask = CmicMaskUnicode | CmicMaskPtInvoke,
                Window = owner,
                Verb = (nint)(id - IdFirst),
                VerbW = (nint)(id - IdFirst),
                Show = SwShowNormal,
                X = at.X,
                Y = at.Y,
            };
            var hr = ((IContextMenu)parts.Handler).InvokeCommand(ref info);
            return hr < 0 && hr != ErrorCancelled ? Reason(hr) : null;
        }
        finally
        {
            s_tracking = null;
            Release(parts);
        }
    }

    /// <summary>
    /// The shell's own Eject for a removable drive or a disc (AF-3.11): the drive's context menu is built and the
    /// item whose verb is "eject" invoked, as choosing it in File Explorer would; the shell says the rest (safe to
    /// remove, or in use). The reason when the drive has no Eject or Windows refuses, null otherwise.
    /// </summary>
    public static string? Eject(nint owner, string drive)
    {
        var built = Build(owner, drive, out var reason);
        if (built is not { } parts)
        {
            return reason;
        }

        var name = Marshal.AllocHGlobal((int)VerbMax * 2);
        try
        {
            var contextMenu = (IContextMenu)parts.Handler;
            var count = GetMenuItemCount(parts.Menu);
            for (var i = 0; i < count; i++)
            {
                var id = GetMenuItemID(parts.Menu, i);
                if (id == uint.MaxValue || id < IdFirst || contextMenu.GetCommandString((nuint)(id - IdFirst), GcsVerbW, 0, name, VerbMax) < 0)
                {
                    continue;
                }

                if (!string.Equals(Marshal.PtrToStringUni(name), "eject", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var info = new InvokeInfo
                {
                    Size = Marshal.SizeOf<InvokeInfo>(),
                    Mask = CmicMaskUnicode,
                    Window = owner,
                    Verb = (nint)(id - IdFirst),
                    VerbW = (nint)(id - IdFirst),
                    Show = SwShowNormal,
                };
                var hr = contextMenu.InvokeCommand(ref info);
                return hr < 0 && hr != ErrorCancelled ? Reason(hr) : null;
            }

            return "This drive has no Eject";
        }
        finally
        {
            Marshal.FreeHGlobal(name);
            Release(parts);
        }
    }

    /// <summary>
    /// The owner window's messages while the shell's menu tracks: the shell fills a submenu on WM_INITMENUPOPUP and
    /// draws its own items on the two item messages; true when the shell took the message, with the result set.
    /// </summary>
    public static bool ForwardMenuMessage(ref Message m)
    {
        if (s_tracking is null || m.Msg is not (WmInitMenuPopup or WmDrawItem or WmMeasureItem or WmMenuChar))
        {
            return false;
        }

        if (s_tracking is IContextMenu3 menu3)
        {
            if (menu3.HandleMenuMsg2((uint)m.Msg, m.WParam, m.LParam, out var result) == 0)
            {
                m.Result = result;
                return true;
            }

            return false;
        }

        return s_tracking is IContextMenu2 menu2 && menu2.HandleMenuMsg((uint)m.Msg, m.WParam, m.LParam) == 0;
    }

    /// <summary>The menu built and not yet shown: the popup, the shell's handler of it, the folder it came from and the item's id list, all to give back with <see cref="Release"/>.</summary>
    private static Parts? Build(nint owner, string path, out string? reason)
    {
        reason = null;
        var hr = SHParseDisplayName(path, 0, out var pidl, 0, out _);
        if (hr < 0 || pidl == 0)
        {
            reason = Reason(hr);
            return null;
        }

        IShellFolder? folder = null;
        object? handler = null;
        nint menu = 0;
        try
        {
            hr = SHBindToParent(pidl, in IidIShellFolder, out folder, out var child);
            if (hr < 0 || folder is null)
            {
                reason = Reason(hr);
                return null;
            }

            hr = folder.GetUIObjectOf(owner, 1, [child], in IidIContextMenu, 0, out handler);
            if (hr < 0 || handler is not IContextMenu contextMenu)
            {
                reason = Reason(hr);
                return null;
            }

            menu = CreatePopupMenu();
            hr = contextMenu.QueryContextMenu(menu, 0, IdFirst, IdLast, CmfNormal);
            if (hr < 0)
            {
                reason = Reason(hr);
                return null;
            }

            var parts = new Parts(menu, handler, folder, pidl);
            menu = 0;
            handler = null;
            folder = null;
            pidl = 0;
            return parts;
        }
        finally
        {
            if (menu != 0)
            {
                DestroyMenu(menu);
            }

            if (handler is not null)
            {
                Marshal.FinalReleaseComObject(handler);
            }

            if (folder is not null)
            {
                Marshal.FinalReleaseComObject(folder);
            }

            if (pidl != 0)
            {
                Marshal.FreeCoTaskMem(pidl);
            }
        }
    }

    private static void Release(Parts parts)
    {
        DestroyMenu(parts.Menu);
        Marshal.FinalReleaseComObject(parts.Handler);
        Marshal.FinalReleaseComObject(parts.Folder);
        Marshal.FreeCoTaskMem(parts.Pidl);
    }

    private static string Reason(int hr) => Marshal.GetExceptionForHR(hr)?.Message ?? $"error 0x{hr:X8}";

    private sealed record Parts(nint Menu, object Handler, IShellFolder Folder, nint Pidl);

    // ---- the shell's functions ----

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int SHParseDisplayName(string name, nint bindContext, out nint pidl, uint attributesIn, out uint attributesOut);

    [DllImport("shell32.dll", ExactSpelling = true)]
    private static extern int SHBindToParent(nint pidl, in Guid riid, out IShellFolder folder, out nint child);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int SHOpenWithDialog(nint owner, in OpenAsInfo info);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShellExecuteExW(ref ShellExecuteInfo info);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern nint CreatePopupMenu();

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern int GetMenuItemCount(nint menu);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern uint GetMenuItemID(nint menu, int position);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyMenu(nint menu);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern uint TrackPopupMenuEx(nint menu, uint flags, int x, int y, nint owner, nint parameters);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessageW(nint window, int message, nint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OpenAsInfo
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string File;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? Class;

        public int Flags;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShellExecuteInfo
    {
        public int Size;
        public uint Mask;
        public nint Window;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? Verb;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? File;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? Parameters;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? Directory;

        public int Show;
        public nint Instance;
        public nint IdList;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? Class;

        public nint KeyClass;
        public uint HotKey;
        public nint Icon;
        public nint Process;
    }

    /// <summary>CMINVOKECOMMANDINFOEX: the verb by its offset in both the narrow and the wide field, the window, the point.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct InvokeInfo
    {
        public int Size;
        public uint Mask;
        public nint Window;
        public nint Verb;
        public nint Parameters;
        public nint Directory;
        public int Show;
        public uint HotKey;
        public nint Icon;
        public nint Title;
        public nint VerbW;
        public nint ParametersW;
        public nint DirectoryW;
        public nint TitleW;
        public int X;
        public int Y;
    }

    // ---- the shell's interfaces, in vtable order; a derived interface repeats the base's methods, as COM interop wants ----

    [ComImport]
    [Guid("000214E6-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellFolder
    {
        [PreserveSig]
        int ParseDisplayName(nint owner, nint bindContext, [MarshalAs(UnmanagedType.LPWStr)] string name, nint eaten, out nint pidl, nint attributes);

        [PreserveSig]
        int EnumObjects(nint owner, uint flags, out nint enumerator);

        [PreserveSig]
        int BindToObject(nint pidl, nint bindContext, in Guid riid, out nint result);

        [PreserveSig]
        int BindToStorage(nint pidl, nint bindContext, in Guid riid, out nint result);

        [PreserveSig]
        int CompareIDs(nint parameter, nint pidl1, nint pidl2);

        [PreserveSig]
        int CreateViewObject(nint owner, in Guid riid, out nint result);

        // An array in a COM method marshals as a SAFEARRAY unless told otherwise; the shell wants a C array of pointers.
        [PreserveSig]
        int GetAttributesOf(uint count, [MarshalAs(UnmanagedType.LPArray)] nint[] pidls, ref uint attributes);

        [PreserveSig]
        int GetUIObjectOf(nint owner, uint count, [MarshalAs(UnmanagedType.LPArray)] nint[] pidls, in Guid riid, nint reserved, [MarshalAs(UnmanagedType.IUnknown)] out object result);

        [PreserveSig]
        int GetDisplayNameOf(nint pidl, uint flags, nint name);

        [PreserveSig]
        int SetNameOf(nint owner, nint pidl, [MarshalAs(UnmanagedType.LPWStr)] string name, uint flags, out nint newPidl);
    }

    [ComImport]
    [Guid("000214E4-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IContextMenu
    {
        [PreserveSig]
        int QueryContextMenu(nint menu, uint index, uint idFirst, uint idLast, uint flags);

        [PreserveSig]
        int InvokeCommand(ref InvokeInfo info);

        [PreserveSig]
        int GetCommandString(nuint id, uint type, nint reserved, nint name, uint max);
    }

    [ComImport]
    [Guid("000214F4-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IContextMenu2
    {
        [PreserveSig]
        int QueryContextMenu(nint menu, uint index, uint idFirst, uint idLast, uint flags);

        [PreserveSig]
        int InvokeCommand(ref InvokeInfo info);

        [PreserveSig]
        int GetCommandString(nuint id, uint type, nint reserved, nint name, uint max);

        [PreserveSig]
        int HandleMenuMsg(uint message, nint wParam, nint lParam);
    }

    [ComImport]
    [Guid("BCFCE0A0-EC17-11d0-8D10-00A0C90F2719")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IContextMenu3
    {
        [PreserveSig]
        int QueryContextMenu(nint menu, uint index, uint idFirst, uint idLast, uint flags);

        [PreserveSig]
        int InvokeCommand(ref InvokeInfo info);

        [PreserveSig]
        int GetCommandString(nuint id, uint type, nint reserved, nint name, uint max);

        [PreserveSig]
        int HandleMenuMsg(uint message, nint wParam, nint lParam);

        [PreserveSig]
        int HandleMenuMsg2(uint message, nint wParam, nint lParam, out nint result);
    }
}
