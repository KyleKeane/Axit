using System.Runtime.InteropServices;

namespace Axit.Forms;

/// <summary>
/// A secret on the clipboard (docs/axit/security-audit-2026-10-24.md S-15; axPass PW-5.4): the text with the marks
/// password managers set, which Windows clipboard history (Windows+V), the cloud clipboard and clipboard monitors such
/// as axClip honour by keeping no copy: <c>ExcludeClipboardContentFromMonitorProcessing</c>,
/// <c>CanIncludeInClipboardHistory</c> 0, <c>CanUploadToCloudClipboard</c> 0 and the older
/// <c>Clipboard Viewer Ignore</c>. <see cref="ClearIfUnchanged"/> empties the clipboard later only when nothing else
/// was copied meanwhile, told by the clipboard's sequence number, so that the secret is never read back to compare.
/// </summary>
public static class PrivateClipboard
{
    /// <summary>Puts <paramref name="text"/> on the clipboard marked private; the clipboard's sequence number after it, for <see cref="ClearIfUnchanged"/>. Throws <see cref="ExternalException"/> when another program holds the clipboard.</summary>
    public static uint Copy(string text)
    {
        var data = new DataObject();
        data.SetData(DataFormats.UnicodeText, text);
        data.SetData("ExcludeClipboardContentFromMonitorProcessing", new MemoryStream([0]));
        data.SetData("Clipboard Viewer Ignore", new MemoryStream([0]));
        data.SetData("CanIncludeInClipboardHistory", new MemoryStream(BitConverter.GetBytes(0)));
        data.SetData("CanUploadToCloudClipboard", new MemoryStream(BitConverter.GetBytes(0)));
        Clipboard.SetDataObject(data, copy: true);
        return GetClipboardSequenceNumber();
    }

    /// <summary>Empties the clipboard when it still holds what <see cref="Copy"/> put there; true when it did.</summary>
    public static bool ClearIfUnchanged(uint sequence)
    {
        if (GetClipboardSequenceNumber() != sequence)
        {
            return false;
        }

        try
        {
            Clipboard.Clear();
            return true;
        }
        catch (ExternalException)
        {
            return false;
        }
    }

    [DllImport("user32.dll")]
    private static extern uint GetClipboardSequenceNumber();
}
