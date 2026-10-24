using AxProcess.Core;

namespace AxProcess.Tests;

/// <summary>The Services and Startup views (docs/axprocess/SPEC.md PR-4, PR-5): labels, the Service Control Manager's numbers, the StartupApproved bytes, the program of a command.</summary>
public sealed class ServiceAndStartupTests
{
    [Fact]
    public void A_service_reads_display_name_name_state_and_start()
    {
        var row = new ServiceRow("wuauserv", "Windows Update", ServiceState.Running, ServiceStart.Manual, 1234, true);
        Assert.Equal("Windows Update (wuauserv), running, manual", row.Label);
        Assert.Equal(["Name wuauserv", "State running", "Start type manual", "Process ID 1234"], row.Details);
        Assert.Equal("BITS, stopped, automatic (delayed)", new ServiceRow("BITS", "BITS", ServiceState.Stopped, ServiceStart.AutomaticDelayed, 0, false).Label);
        Assert.Equal("ActiveX Installer (AxInstSV), stopped, manual", new ServiceRow("AxInstSV", "ActiveX Installer (AxInstSV)", ServiceState.Stopped, ServiceStart.Manual, 0, false).Label);
        Assert.Equal("Services, 280", ServiceRow.ListName(280));
    }

    [Theory]
    [InlineData(1u, ServiceState.Stopped)]
    [InlineData(2u, ServiceState.Starting)]
    [InlineData(3u, ServiceState.Stopping)]
    [InlineData(4u, ServiceState.Running)]
    [InlineData(7u, ServiceState.Paused)]
    public void States_come_from_the_managers_numbers(uint code, ServiceState state) => Assert.Equal(state, ServiceRow.StateOf(code));

    [Theory]
    [InlineData(2u, false, ServiceStart.Automatic)]
    [InlineData(2u, true, ServiceStart.AutomaticDelayed)]
    [InlineData(3u, false, ServiceStart.Manual)]
    [InlineData(4u, false, ServiceStart.Disabled)]
    [InlineData(0u, false, ServiceStart.Boot)]
    public void Start_types_come_from_the_configuration(uint code, bool delayed, ServiceStart start) => Assert.Equal(start, ServiceRow.StartOf(code, delayed));

    [Fact]
    public void The_services_of_this_computer_can_be_listed()
    {
        var services = ServiceManager.List();
        Assert.NotEmpty(services);
        Assert.Contains(services, service => service.Name.Equals("EventLog", StringComparison.OrdinalIgnoreCase) && service.State == ServiceState.Running);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(new byte[0], true)]
    [InlineData(new byte[] { 0x02, 0, 0, 0 }, true)]
    [InlineData(new byte[] { 0x06, 0, 0, 0 }, true)]
    [InlineData(new byte[] { 0x03, 0, 0, 0 }, false)]
    [InlineData(new byte[] { 0x01, 0, 0, 0 }, false)]
    public void Task_Managers_bytes_say_whether_an_entry_runs(byte[]? approved, bool enabled) => Assert.Equal(enabled, StartupList.IsEnabled(approved));

    [Theory]
    [InlineData("\"C:\\Program Files\\App\\app.exe\" --background", "C:\\Program Files\\App\\app.exe")]
    [InlineData("C:\\Tools\\tool.exe /min", "C:\\Tools\\tool.exe")]
    [InlineData("D:\\Startup\\notes.lnk", "D:\\Startup\\notes.lnk")]
    [InlineData("rundll32 something", null)]
    [InlineData("\"unclosed", null)]
    [InlineData("", null)]
    public void The_program_of_a_command_is_found(string command, string? program) => Assert.Equal(program, CommandLine.ProgramOf(command));

    [Fact]
    public void A_startup_entry_reads_name_state_and_place()
    {
        var entry = new StartupEntry("OneDrive", "\"C:\\OneDrive\\OneDrive.exe\" /background", StartupPlace.UserRunKey, true);
        Assert.Equal("OneDrive, enabled, current user's Run key", entry.Label);
        Assert.Equal(["Command \"C:\\OneDrive\\OneDrive.exe\" /background", "Set in current user's Run key", "Enabled", "Program C:\\OneDrive\\OneDrive.exe"], entry.Details);
        Assert.Equal("Tool, disabled, computer's Run key (32-bit)", new StartupEntry("Tool", "tool", StartupPlace.MachineRunKey32, false).Label);
        var sorted = StartupList.Sorted([entry, new StartupEntry("alpha", "a", StartupPlace.CommonFolder, true)]);
        Assert.Equal(["alpha", "OneDrive"], sorted.Select(e => e.Name));
        Assert.Equal("Startup, 2", StartupEntry.ListName(2));
    }

    [Fact]
    public void The_startup_list_of_this_computer_can_be_read()
    {
        // Whatever this machine starts; the read must not throw, and every entry has a name.
        Assert.All(StartupList.Read(), entry => Assert.False(string.IsNullOrEmpty(entry.Name)));
    }
}
