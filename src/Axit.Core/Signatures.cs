using System.Runtime.InteropServices;

namespace Axit.Core;

/// <summary>
/// Whether a program file carries a valid Authenticode signature, asked of Windows (<c>WinVerifyTrust</c>): signed, the
/// signature intact, the certificate chaining to a root Windows trusts, not revoked as far as the local cache knows
/// (no network call). Used before a secret is handed to a program found outside an administrator's folders: a file
/// planted in a folder the user can write is unsigned, or signed by no one Windows trusts (security audit S-19).
/// </summary>
public static class Signatures
{
    public static bool IsTrusted(string file)
    {
        if (!OperatingSystem.IsWindows() || !File.Exists(file))
        {
            return false;
        }

        var fileInfo = new WinTrustFileInfo { Size = (uint)Marshal.SizeOf<WinTrustFileInfo>(), FilePath = file };
        var fileInfoPointer = Marshal.AllocHGlobal(Marshal.SizeOf<WinTrustFileInfo>());
        try
        {
            Marshal.StructureToPtr(fileInfo, fileInfoPointer, false);
            var data = new WinTrustData
            {
                Size = (uint)Marshal.SizeOf<WinTrustData>(),
                UiChoice = WtdUiNone,
                RevocationChecks = WtdRevokeNone,
                UnionChoice = WtdChoiceFile,
                FileInfo = fileInfoPointer,
                StateAction = WtdStateActionIgnore,
                ProvFlags = WtdCacheOnlyUrlRetrieval,
            };
            var action = WinTrustActionGenericVerifyV2;
            return WinVerifyTrust(-1, ref action, ref data) == 0;
        }
        finally
        {
            Marshal.FreeHGlobal(fileInfoPointer);
        }
    }

    private const uint WtdUiNone = 2;
    private const uint WtdRevokeNone = 0;
    private const uint WtdChoiceFile = 1;
    private const uint WtdStateActionIgnore = 0;
    private const uint WtdCacheOnlyUrlRetrieval = 0x00001000;

    private static Guid WinTrustActionGenericVerifyV2 = new("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WinTrustFileInfo
    {
        public uint Size;
        [MarshalAs(UnmanagedType.LPWStr)] public string FilePath;
        public nint File;
        public nint KnownSubject;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WinTrustData
    {
        public uint Size;
        public nint PolicyCallbackData;
        public nint SipClientData;
        public uint UiChoice;
        public uint RevocationChecks;
        public uint UnionChoice;
        public nint FileInfo;
        public uint StateAction;
        public nint StateData;
        public nint UrlReference;
        public uint ProvFlags;
        public uint UiContext;
        public nint SignatureSettings;
    }

    [DllImport("wintrust.dll", CharSet = CharSet.Unicode)]
    private static extern int WinVerifyTrust(nint window, ref Guid action, ref WinTrustData data);
}
