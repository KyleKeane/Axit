using System.Runtime.InteropServices;
using System.Text;

namespace Axit.Core;

/// <summary>
/// Text kept on disk that only this Windows account can read back (security audit S-21): Windows' data protection
/// (DPAPI, <c>CryptProtectData</c>), called directly so that no package is needed. A copy of the file on another
/// computer, in a backup, or read by another user is useless; a program running as the same user can still ask Windows
/// to unprotect it, which no file-based protection prevents. Used for axClip's kept history.
/// </summary>
public static class UserSecret
{
    /// <summary>The text protected for the current user, as base64.</summary>
    public static string Protect(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var input = new DataBlob(bytes);
        try
        {
            if (!CryptProtectData(ref input, null, 0, 0, 0, UiForbidden, out var output))
            {
                throw new InvalidOperationException("Windows could not protect the data: " + Marshal.GetLastPInvokeErrorMessage());
            }

            try
            {
                return Convert.ToBase64String(output.ToArray());
            }
            finally
            {
                LocalFree(output.Data);
            }
        }
        finally
        {
            input.Free();
            Array.Clear(bytes);
        }
    }

    /// <summary>The text back from <see cref="Protect"/>'s base64; null when it was protected for another user or computer, or is no such data.</summary>
    public static string? Unprotect(string protectedText)
    {
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(protectedText);
        }
        catch (FormatException)
        {
            return null;
        }

        var input = new DataBlob(bytes);
        try
        {
            if (!CryptUnprotectData(ref input, 0, 0, 0, 0, UiForbidden, out var output))
            {
                return null;
            }

            try
            {
                var plain = output.ToArray();
                var text = Encoding.UTF8.GetString(plain);
                Array.Clear(plain);
                return text;
            }
            finally
            {
                LocalFree(output.Data);
            }
        }
        finally
        {
            input.Free();
        }
    }

    private const int UiForbidden = 0x1;

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int Size;
        public nint Data;

        public DataBlob(byte[] bytes)
        {
            Size = bytes.Length;
            Data = Marshal.AllocHGlobal(Math.Max(1, bytes.Length));
            Marshal.Copy(bytes, 0, Data, bytes.Length);
        }

        public readonly byte[] ToArray()
        {
            var bytes = new byte[Size];
            Marshal.Copy(Data, bytes, 0, Size);
            return bytes;
        }

        public void Free()
        {
            if (Data != 0)
            {
                Marshal.FreeHGlobal(Data);
                Data = 0;
            }
        }
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptProtectData(ref DataBlob dataIn, string? description, nint entropy, nint reserved, nint prompt, int flags, out DataBlob dataOut);

    [DllImport("crypt32.dll", SetLastError = true)]
    private static extern bool CryptUnprotectData(ref DataBlob dataIn, nint description, nint entropy, nint reserved, nint prompt, int flags, out DataBlob dataOut);

    [DllImport("kernel32.dll")]
    private static extern nint LocalFree(nint memory);
}
