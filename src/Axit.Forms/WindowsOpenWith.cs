using System.Runtime.InteropServices;

namespace Axit.Forms;

/// <summary>
/// Windows' "How do you want to open this file?" dialog, one of the system's own windows (AX-7.2): the program chosen
/// runs the file, and the dialog's own "Always" makes it the default. From axFile's <c>Shell.OpenWith</c> on its
/// second use, axPreview's Open with (docs/axpreview/SPEC.md AP-7.3, 2026-10-24).
/// </summary>
public static class WindowsOpenWith
{
    private const int ErrorCancelled = unchecked((int)0x800704C7);
    private const int OaifAllowRegistration = 0x1;
    private const int OaifRegisterExt = 0x2;
    private const int OaifExec = 0x4;

    /// <summary>The dialog for a file, and the program chosen runs it; the reason when Windows refuses, null otherwise (a cancel included).</summary>
    public static string? Show(nint owner, string path)
    {
        var info = new OpenAsInfo { File = path, Flags = OaifAllowRegistration | OaifRegisterExt | OaifExec };
        var hr = SHOpenWithDialog(owner, in info);
        return hr < 0 && hr != ErrorCancelled ? Marshal.GetExceptionForHR(hr)?.Message ?? $"error 0x{hr:X8}" : null;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int SHOpenWithDialog(nint owner, in OpenAsInfo info);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OpenAsInfo
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string File;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? Class;

        public int Flags;
    }
}
