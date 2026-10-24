using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Axit.Forms;

/// <summary>
/// A named pipe for the current user alone (security audit S-11): its access list lets this user's processes in and
/// denies every client over the network; and which process is at the other end. From the automation channel on its
/// second use, the MCP pipes (docs/axit/mcp.md MC-6.2, MC-6.3), 2026-10-24.
/// </summary>
public static class UserPipe
{
    /// <summary>The current user's security identifier, which the MCP pipes' names carry.</summary>
    public static SecurityIdentifier User()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return identity.User ?? throw new InvalidOperationException("No user for the pipe");
    }

    /// <summary>The access list: the user may read, write and make instances; the network is denied.</summary>
    public static PipeSecurity Security()
    {
        var security = new PipeSecurity();
        var user = User();
        security.SetOwner(user);
        security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.NetworkSid, null), PipeAccessRights.FullControl, AccessControlType.Deny));
        security.AddAccessRule(new PipeAccessRule(user, PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow));
        return security;
    }

    /// <summary>The process that connected to a pipe this process serves; null when Windows cannot say.</summary>
    public static uint? ClientOf(NamedPipeServerStream pipe) =>
        GetNamedPipeClientProcessId(pipe.SafePipeHandle.DangerousGetHandle(), out var id) ? id : null;

    /// <summary>The process that serves a pipe this process connected to; null when Windows cannot say.</summary>
    public static uint? ServerOf(NamedPipeClientStream pipe) =>
        GetNamedPipeServerProcessId(pipe.SafePipeHandle.DangerousGetHandle(), out var id) ? id : null;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetNamedPipeClientProcessId(nint pipe, out uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetNamedPipeServerProcessId(nint pipe, out uint processId);
}
