# Axit.Core, the shared pure code

Code every app may use and no window needs (`docs/axit/SPEC.md` AX-6.1): `Dispatch` (which app a command line starts, AX-1), `BundleInfo` (the version, read from the executable), `AppPaths` (settings, logs and updates under the `Axit` folders, AX-5.1), `SettingsFile` (how every app's settings file is read and written, and the take-over of an old file, AX-5.2 and AX-5.3), `WindowPlacement` (the window's place and size, in every app's settings), `Log` (one file per app; each app calls `Log.UseApp` first thing in its `Run`), `Updates/UpdateCheck` (the latest GitHub release: JSON parsing, version comparison; the repository name lives here) and `Updates/Updater` (the download and the hand-over to `install.ps1`, AX-3), `Audio/WaveTone` (the chimes and the tick as WAV bytes, generated in code; no media files ship).

Unit tested in `tests/Axit.Tests`: `DispatchTests`, `SettingsFileTests`, `UpdateCheckTests`, `WaveToneTests`, and the bundle's own `LayoutTests` (the project references point one way, M2). The key collision test of AX-8.4 comes with the key tables.

Rules: nothing here references an app (M2); a piece arrives here only when a second app needs it, unchanged where possible (M3); plain static classes and records, no frameworks (B-10). An app's settings class stays in the app; only the file handling is shared.
