# AxMedia.Core, the pure code

Everything of AxMedia that needs no window: the one seam to Windows' media stack, the queue, a folder's media files, the settings. Unit tested in `tests/AxMedia.Tests` over temporary folders; the player itself is checked by ear. The spec is `docs/axmedia/SPEC.md` ("SPEC.md" below); the window is `src/AxMedia`, with its own `CLAUDE.md`. The bundle rules are in the root `CLAUDE.md`. The player and the queue came from AxFile.Core on 2026-10-22 (`docs/axit/axfile-split.md`); the rules of media files (`MediaFiles`: the extensions, the time format, the speed steps) and `NaturalOrder` are `Axit.Core`'s, shared with AxFile.

## Commands

```
dotnet test tests/AxMedia.Tests/AxMedia.Tests.csproj
```

## Where things are

- `MediaPlayer` and `PlayerState` (SPEC.md AM-4.2, AM-D5): Media Foundation through MFPlay (`mfplay.dll`: `MFPCreateMediaPlayer`, `IMFPMediaPlayer`, `IMFPMediaItem`, declared as COM interfaces in vtable order with `PreserveSig`), one file at a time into the window handle given at creation. `Play` makes the item synchronously (a file that cannot be opened throws with the reason), sets it, and `Started` fires when the engine reports it set and playing; `Ended` and `Failed` are the other events; `Position`, `Duration`, `Seek` (the position reached, clamped to the file), `Pause`, `Resume`, `Stop`, `SetRate` (kept only once the engine accepts it; a file that refuses it plays at 1 and `Rate` says so), `HasVideo`, `UpdateVideo`, `Dispose` (Shutdown). `Reason` turns an HRESULT into one line.
- `PlayQueue` (AM-3): the queued paths with `Playing` (the index playing or last played, -1 for none; `Set`, `Add`, `Select`, `MoveOn`, `Remove` keeping the index on its file, `Clear`), `Lines(active)` (the names, the playing one marked "(playing)" while the player is active, or "Queue is empty"), `ListName` ("Queue, 12 items"), `IsFile`, `Information` ("03 Chapter 3.mp3, 3 of 12"), `PlaceText` (the status bar's "3 of 12, speed 1.25").
- `MediaFolder` (AM-1.1, AM-3.2, AM-D2): `Files` (a folder's media files in natural order, hidden and system ones left out), `Queue` (what a play of paths makes: one file with its folder's media files around it and its index, a folder from the first, several paths expanded), `Expand` (what an add appends: files as they are, folders as their media files).
- `MediaSettings` (AM-7.1): the settings file, on the bundle's `WindowSettings`, with `SayEachFile`, `Speed`, `SeekSeconds` and `Recent` (the files and folders added by command, ten).

## Gotchas already learned

- In a COM interface a C# `bool` marshals as a two-byte VARIANT_BOOL, not the four-byte BOOL MFPlay uses: the BOOL parameters are `int`.
- MFPlay's events arrive as a pointer to a header, on a thread of MFPlay's choosing (a worker thread was seen); only the header's first two fields (the type and the HRESULT) are read there, and everything else happens on the window's thread through the `post` action the window hands the player (its `BeginInvoke`), so the engine is only ever called from the thread that made it. Calling it from another thread fails with E_NOINTERFACE: Windows has no proxy for MFPlay's interfaces, and `SynchronizationContext.Current` was found not to be the WinForms one in an off-screen probe, which is why the window passes its own marshalling in.
- The `PROPVARIANT` for a position is `VT_I8` with the value in 100-nanosecond units, which is what `TimeSpan.Ticks` are.
- An event names the item it is about (the pointer right after the header, in the item-set, play and ended events alike); one about an item replaced by a quicker second `Play` is left alone, told by the interface pointer kept in `_itemPointer`, since the old item's failure skipped the new file (2026-10-23).
