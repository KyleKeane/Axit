# Axit, the executable

The one program file of the bundle (`docs/axit/SPEC.md` AX-1, B-1, B-2). `Program.Main` calls `ApplicationConfiguration.Initialize()`, asks `Axit.Core/Dispatch` which app the command line means, sets the app's taskbar id and calls that app's `Run` (`AxClaudeApp.Run`, later `AxDownApp.Run`). Everything after, including the crash handling and the log, belongs to the app. The icons live here; `tools/make-icon.ps1` draws them.

`<Version>` in `Axit.csproj` is the bundle's version (`release.ps1` sets it); `Axit.Core/BundleInfo.Version` reads it from the executable. The WinForms application settings (`ApplicationHighDpiMode`, visual styles, `InvariantGlobalization`) are here because only an executable project generates `ApplicationConfiguration`.

A running `Axit.exe` locks `src/Axit/bin`; build elsewhere to check a change (`dotnet build src/Axit -o <scratch dir>`). The message boxes in `Program` are the only ones allowed in the bundle: no window exists yet when they show (AX-7.2).
