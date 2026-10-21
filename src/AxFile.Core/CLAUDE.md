# AxFile.Core, the pure code

Everything of AxFile that needs no window: a folder's entries, the player's rules, the one seam to Windows' media stack, the file operations, the settings. Unit tested in `tests/AxFile.Tests` over temporary folders; the player itself is checked by ear. The spec is `docs/axfile/SPEC.md` ("SPEC.md" below); the window is `src/AxFile`, with its own `CLAUDE.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
dotnet test tests/AxFile.Tests/AxFile.Tests.csproj
```

## Where things are

- `FolderListing` and `Entry`, `OpenWay` (SPEC.md AF-2.2, AF-3): `Read` (a folder's entries, hidden and system ones left out unless asked, folders first in natural order), `Sort`, `WayOf` (what Enter does, by the extension), `ListName` ("Dune, 24 items"), `FolderName`, `Information` ("report.pdf, 1.2 MB, modified …"), `FormatSize`, `Parent` (null at a drive's root), `Resolve` (a typed path with environment variables, a file's path giving its folder and the file).
- `NaturalOrder`: `StrCmpLogicalW` of shlwapi as an `IComparer<string>`, File Explorer's order of names.
- `MediaFiles` (AF-5): the extension sets, `IsMedia`, `Queue` (a listing's media files in order), `FormatTime`, `ParseTime`, `Faster`, `Slower`, `SanitizeSpeed`, `SpeedText`.
- `MediaPlayer` and `PlayerState` (AF-5.2, AF-D3): Media Foundation through MFPlay (`mfplay.dll`: `MFPCreateMediaPlayer`, `IMFPMediaPlayer`, `IMFPMediaItem`, declared as COM interfaces in vtable order with `PreserveSig`), one file at a time into the window handle given at creation. `Play` makes the item synchronously (a file that cannot be opened throws with the reason), sets it, and `Started` fires when the engine reports it set and playing; `Ended` and `Failed` are the other events; `Position`, `Duration`, `Seek`, `Pause`, `Resume`, `Stop`, `SetRate`, `HasVideo`, `UpdateVideo`, `Dispose` (Shutdown). `Reason` turns an HRESULT into one line.
- `FileOperations` (AF-4): `CheckName`, `CreateFolder`, `Rename` (a change of case included), `DeleteToRecycleBinAsync` (a list of paths, one operation) and `CopyOrMoveAsync` (`SHFileOperationW` with undo allowed, silent, no confirmation, a number on a name already there for the copy and the move, each on a single-threaded-apartment thread of its own), `Message` (one line from an exception).
- `FileSettings` (AF-8.1): the settings file, on the bundle's `WindowSettings`, with the last folder, the recent folders, `ShowHidden`, `SayEachFile`, `Speed`, `SeekSeconds`.

## Gotchas already learned

- In a COM interface a C# `bool` marshals as a two-byte VARIANT_BOOL, not the four-byte BOOL MFPlay uses: the BOOL parameters are `int`.
- MFPlay's events arrive as a pointer to a header, on a thread of MFPlay's choosing (a worker thread was seen); only the header's first two fields (the type and the HRESULT) are read there, and everything else happens on the window's thread through the `post` action the window hands the player (its `BeginInvoke`), so the engine is only ever called from the thread that made it. Calling it from another thread fails with E_NOINTERFACE: Windows has no proxy for MFPlay's interfaces, and `SynchronizationContext.Current` was found not to be the WinForms one in an off-screen probe, which is why the window passes its own marshalling in.
- The `PROPVARIANT` for a position is `VT_I8` with the value in 100-nanosecond units, which is what `TimeSpan.Ticks` are.
- `SHFileOperationW` wants its path list ended by a second null; the marshaller adds one, the code adds the other.
- `Directory.Move` and `File.Move` rename a name whose case alone changes; `Rename` skips the existence check for that case.
