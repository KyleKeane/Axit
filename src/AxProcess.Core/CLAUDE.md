# AxProcess.Core, the pure code

Everything of axProcess that needs no window. Unit tested in `tests/AxProcess.Tests`: the labels, the order, the arithmetic and the parsing from data, and one read each of this computer's processes, services and startup list. The spec is `docs/axprocess/SPEC.md` ("SPEC.md" below); the window is `src/AxProcess`, with its own `CLAUDE.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\test.ps1 AxProcess
```

## Where things are

- `ProcessTable`, `ProcessSample`, `ProcessRow`, `ProcessSort` (SPEC.md PR-3): `Sample` reads every process (name, working set, processor time, which a protected process does not give), `Rows` turns two samples into rows with each process's share of all processors over the time between (an ID that came back under another name has none), `Sorted`, `ListName`, `Summary` (PR-2.5).
- `SizeText`: "900 KB", "350 MB", "1.2 GB", and gigabytes with one decimal for the summary.
- `SystemInfo` and `SystemTimes`: the physical memory (`GlobalMemoryStatusEx`), the processors' idle, kernel and user times (`GetSystemTimes`; `Busy` between two, the kernel time including the idle time), a process's program file (`QueryFullProcessImageName` with the limited query right, which works for most processes a user cannot open fully) and the lines of its details notice.
- `ServiceManager`, `ServiceRow`, `ServiceResult` (PR-4): the Service Control Manager by P/Invoke on advapi32. `List` (`EnumServicesStatusEx` with the process information, looping on more data, then each service's start type from `QueryServiceConfig` and the delayed flag from `QueryServiceConfig2`), `Start` and `Stop` (the right asked for, the command, then a wait of up to 30 seconds for the state; already running or not active counts as done; access denied is `Denied`). `ServiceRow` maps the manager's numbers to states and start types.
- `StartupList`, `StartupEntry`, `StartupPlace`, `CommandLine` (PR-5): the three Run keys and the two Startup folders; `IsEnabled` reads Task Manager's `StartupApproved` bytes (odd first byte: turned off); `CommandLine.ProgramOf` finds the program of a command (the quoted path, the text through `.exe`, or a rooted path).
- `ProcessSettings` (PR-7.1): the settings file with the order of the Processes view.

## Gotchas

- `Process.TotalProcessorTime` throws for protected processes (System, the antivirus); their memory is still readable, so they are listed without a processor share.
- The working set is what Task Manager's Details tab calls memory; its Processes tab shows the private working set, a little smaller. The spec says working set.
- `ControlService` writes a `SERVICE_STATUS`, smaller than the `SERVICE_STATUS_PROCESS` it is given here, which is safe; the wait reads the full status with `QueryServiceStatusEx`.
