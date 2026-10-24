using System.ComponentModel;
using System.Runtime.InteropServices;

namespace AxProcess.Core;

public enum ServiceState
{
    Stopped,
    Starting,
    Stopping,
    Running,
    Continuing,
    Pausing,
    Paused,
}

public enum ServiceStart
{
    Automatic,
    AutomaticDelayed,
    Manual,
    Disabled,
    Boot,
}

/// <summary>One item of the Services view (SPEC.md PR-4.1).</summary>
public sealed record ServiceRow(string Name, string DisplayName, ServiceState State, ServiceStart Start, int ProcessId, bool CanStop)
{
    /// <summary>"Windows Update (wuauserv), running, manual"; the short name once, for a display name that holds it already ("ActiveX Installer (AxInstSV)") or is it.</summary>
    public string Label => $"{Title}, {StateText(State)}, {StartText(Start)}";

    private string Title =>
        DisplayName.Equals(Name, StringComparison.OrdinalIgnoreCase) || DisplayName.EndsWith($"({Name})", StringComparison.OrdinalIgnoreCase)
            ? DisplayName
            : $"{DisplayName} ({Name})";

    /// <summary>The lines of the details notice (PR-4.2).</summary>
    public IReadOnlyList<string> Details => ProcessId > 0
        ? [$"Name {Name}", $"State {StateText(State)}", $"Start type {StartText(Start)}", $"Process ID {ProcessId}"]
        : [$"Name {Name}", $"State {StateText(State)}", $"Start type {StartText(Start)}"];

    public static string StateText(ServiceState state) => state switch
    {
        ServiceState.Running => "running",
        ServiceState.Starting => "starting",
        ServiceState.Stopping => "stopping",
        ServiceState.Paused => "paused",
        ServiceState.Pausing => "pausing",
        ServiceState.Continuing => "continuing",
        _ => "stopped",
    };

    public static string StartText(ServiceStart start) => start switch
    {
        ServiceStart.Automatic => "automatic",
        ServiceStart.AutomaticDelayed => "automatic (delayed)",
        ServiceStart.Disabled => "disabled",
        ServiceStart.Boot => "at boot",
        _ => "manual",
    };

    /// <summary>The state from the Service Control Manager's number (SERVICE_STOPPED is 1 … SERVICE_PAUSED is 7).</summary>
    public static ServiceState StateOf(uint code) => code switch
    {
        2 => ServiceState.Starting,
        3 => ServiceState.Stopping,
        4 => ServiceState.Running,
        5 => ServiceState.Continuing,
        6 => ServiceState.Pausing,
        7 => ServiceState.Paused,
        _ => ServiceState.Stopped,
    };

    /// <summary>The start type from the configuration's number (SERVICE_BOOT_START 0 … SERVICE_DISABLED 4) and the delayed flag.</summary>
    public static ServiceStart StartOf(uint code, bool delayed) => code switch
    {
        0 or 1 => ServiceStart.Boot,
        2 => delayed ? ServiceStart.AutomaticDelayed : ServiceStart.Automatic,
        4 => ServiceStart.Disabled,
        _ => ServiceStart.Manual,
    };

    public static string ListName(int count) => count == 0 ? "Services, none" : $"Services, {count}";
}

/// <summary>What a start or a stop came to (PR-4.3): done, still on its way when the wait ran out, refused, or Windows' message.</summary>
public sealed record ServiceResult(bool Done, bool TimedOut, bool Denied, string? Error);

/// <summary>
/// The services through the Service Control Manager (SPEC.md PR-4), by P/Invoke on advapi32: the list with each
/// service's state and start type, and start and stop with a wait for the new state. Every call is blocking; the
/// window runs them on a background task. A user may list and read every service; starting and stopping most of them
/// needs administrator rights, which Windows reports as access denied.
/// </summary>
public static class ServiceManager
{
    private const uint ScManagerConnect = 0x0001;
    private const uint ScManagerEnumerate = 0x0004;
    private const uint ServiceQueryConfig = 0x0001;
    private const uint ServiceQueryStatus = 0x0004;
    private const uint ServiceStartAccess = 0x0010;
    private const uint ServiceStopAccess = 0x0020;
    private const uint ServiceWin32 = 0x30;
    private const uint ServiceStateAll = 3;
    private const uint ServiceAcceptStop = 0x1;
    private const uint ServiceControlStop = 1;
    private const int ErrorMoreData = 234;
    private const int ErrorAccessDenied = 5;
    private const int ErrorAlreadyRunning = 1056;
    private const int ErrorNotActive = 1062;
    private const int ScEnumProcessInfo = 0;
    private const uint ServiceConfigDelayedAutoStart = 3;

    public static readonly TimeSpan Wait = TimeSpan.FromSeconds(30);

    /// <summary>Every Win32 service by its display name; an empty list when the Service Control Manager cannot be opened.</summary>
    public static IReadOnlyList<ServiceRow> List()
    {
        var manager = OpenSCManager(null, null, ScManagerConnect | ScManagerEnumerate);
        if (manager == IntPtr.Zero)
        {
            return [];
        }

        var rows = new List<ServiceRow>();
        try
        {
            uint resume = 0;
            EnumServicesStatusEx(manager, ScEnumProcessInfo, ServiceWin32, ServiceStateAll, IntPtr.Zero, 0, out var needed, out _, ref resume, null);
            while (true)
            {
                var size = needed + 4096;
                var buffer = Marshal.AllocHGlobal((int)size);
                try
                {
                    var ok = EnumServicesStatusEx(manager, ScEnumProcessInfo, ServiceWin32, ServiceStateAll, buffer, size, out needed, out var returned, ref resume, null);
                    var error = Marshal.GetLastWin32Error();
                    var entry = Marshal.SizeOf<EnumServiceStatusProcess>();
                    for (var i = 0; i < returned; i++)
                    {
                        var item = Marshal.PtrToStructure<EnumServiceStatusProcess>(buffer + i * entry);
                        var name = Marshal.PtrToStringUni(item.ServiceName) ?? string.Empty;
                        var display = Marshal.PtrToStringUni(item.DisplayName) ?? name;
                        rows.Add(new ServiceRow(name, display, ServiceRow.StateOf(item.Status.CurrentState), StartType(manager, name),
                            (int)item.Status.ProcessId, (item.Status.ControlsAccepted & ServiceAcceptStop) != 0));
                    }

                    if (ok || error != ErrorMoreData)
                    {
                        break;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }
        finally
        {
            CloseServiceHandle(manager);
        }

        return rows.OrderBy(row => row.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Starts the service and waits for it to run.</summary>
    public static ServiceResult Start(string name) => Control(name, ServiceStartAccess, ServiceState.Running, service =>
        StartService(service, 0, null) ? 0 : Marshal.GetLastWin32Error(), ErrorAlreadyRunning);

    /// <summary>Stops the service and waits for it to stop.</summary>
    public static ServiceResult Stop(string name) => Control(name, ServiceStopAccess, ServiceState.Stopped, service =>
        ControlService(service, ServiceControlStop, out _) ? 0 : Marshal.GetLastWin32Error(), ErrorNotActive);

    /// <summary>Opens the service with the right it needs, sends the command, and waits for the state; an error that means the state is there already counts as done.</summary>
    private static ServiceResult Control(string name, uint access, ServiceState target, Func<IntPtr, int> command, int alreadyThere)
    {
        var manager = OpenSCManager(null, null, ScManagerConnect);
        if (manager == IntPtr.Zero)
        {
            return Failed(Marshal.GetLastWin32Error());
        }

        try
        {
            var service = OpenService(manager, name, access | ServiceQueryStatus);
            if (service == IntPtr.Zero)
            {
                return Failed(Marshal.GetLastWin32Error());
            }

            try
            {
                var error = command(service);
                if (error != 0 && error != alreadyThere)
                {
                    return Failed(error);
                }

                var until = DateTime.UtcNow + Wait;
                while (DateTime.UtcNow < until)
                {
                    if (QueryServiceStatusEx(service, 0, out var status, (uint)Marshal.SizeOf<ServiceStatusProcess>(), out _)
                        && ServiceRow.StateOf(status.CurrentState) == target)
                    {
                        return new ServiceResult(true, false, false, null);
                    }

                    Thread.Sleep(250);
                }

                return new ServiceResult(false, true, false, null);
            }
            finally
            {
                CloseServiceHandle(service);
            }
        }
        finally
        {
            CloseServiceHandle(manager);
        }
    }

    private static ServiceResult Failed(int error) =>
        error == ErrorAccessDenied ? new ServiceResult(false, false, true, null) : new ServiceResult(false, false, false, new Win32Exception(error).Message);

    /// <summary>The start type from the service's configuration, manual when it cannot be read.</summary>
    private static ServiceStart StartType(IntPtr manager, string name)
    {
        var service = OpenService(manager, name, ServiceQueryConfig);
        if (service == IntPtr.Zero)
        {
            return ServiceStart.Manual;
        }

        try
        {
            QueryServiceConfig(service, IntPtr.Zero, 0, out var needed);
            if (needed == 0)
            {
                return ServiceStart.Manual;
            }

            var buffer = Marshal.AllocHGlobal((int)needed);
            try
            {
                if (!QueryServiceConfig(service, buffer, needed, out _))
                {
                    return ServiceStart.Manual;
                }

                // QUERY_SERVICE_CONFIG: dwServiceType, then dwStartType.
                var start = (uint)Marshal.ReadInt32(buffer, 4);
                return ServiceRow.StartOf(start, start == 2 && Delayed(service));
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        finally
        {
            CloseServiceHandle(service);
        }
    }

    private static bool Delayed(IntPtr service)
    {
        // SERVICE_DELAYED_AUTO_START_INFO is one BOOL.
        var buffer = Marshal.AllocHGlobal(16);
        try
        {
            return QueryServiceConfig2(service, ServiceConfigDelayedAutoStart, buffer, 16, out _) && Marshal.ReadInt32(buffer) != 0;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ServiceStatusProcess
    {
        public uint ServiceType;
        public uint CurrentState;
        public uint ControlsAccepted;
        public uint Win32ExitCode;
        public uint ServiceSpecificExitCode;
        public uint CheckPoint;
        public uint WaitHint;
        public uint ProcessId;
        public uint ServiceFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EnumServiceStatusProcess
    {
        public IntPtr ServiceName;
        public IntPtr DisplayName;
        public ServiceStatusProcess Status;
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr OpenSCManager(string? machine, string? database, uint access);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr OpenService(IntPtr manager, string name, uint access);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseServiceHandle(IntPtr handle);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumServicesStatusEx(IntPtr manager, int level, uint type, uint state, IntPtr buffer, uint size,
        out uint needed, out uint returned, ref uint resume, string? group);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryServiceConfig(IntPtr service, IntPtr buffer, uint size, out uint needed);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryServiceConfig2(IntPtr service, uint level, IntPtr buffer, uint size, out uint needed);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryServiceStatusEx(IntPtr service, int level, out ServiceStatusProcess status, uint size, out uint needed);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool StartService(IntPtr service, uint arguments, string[]? vectors);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ControlService(IntPtr service, uint control, out ServiceStatusProcess status);
}
