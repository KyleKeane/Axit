# axJSON, the app

The window side of axJSON: an `EditorApp` record for the bundle's text editor shell (`src/Axit.Forms/TextEditorForm`), the key table and the texts. The pure code (the JSON outline and the formatter) is `src/AxJSON.Core`, with its own `CLAUDE.md`. The spec is `docs/axjson/SPEC.md` ("SPEC.md" below); the guide `docs/axjson/user-guide.md` is embedded by `AxJSON.csproj`; the manual test plan is `docs/axjson/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\run-axjson.ps1 C:\path\to\settings.json   # build Axit and start axJSON on the file (run.ps1 -App json; close a running Axit first)
.\run-axjson.ps1                            # axJSON with an empty document
dotnet build Axit.sln
dotnet test tests/AxJSON.Tests/AxJSON.Tests.csproj
```

## Where things are

- `AxJSONApp`: the entry point `Run` that Axit's `Program` calls (bundle AX-1.7), one call into `Axit.Forms.EditorHost.Run` with `App`, axJSON's `EditorApp` record: name, verb, settings file (`axjson.json`), file filter (`.json`, `.jsonc`), F1 text, guide, the JSON key pairs, the JSON resources pages, and the Format command (`AxJSON.Core.JsonFormat.Apply` as an `EditorCommand`).
- `AxJSONKeys`: the window's keys (SPEC.md AJ-2.2, AJ-2.3): the shell's through `EditorKeys.All`, then Ctrl+K key, Ctrl+J object, Ctrl+L array, Ctrl+I item, Ctrl+E error (Ctrl+Shift for the previous one), Ctrl+Shift+F format; `Jumps` pairs each kind with `JsonOutline`'s next and previous functions, for the editor and every notice text.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's (`Axit.Core.BundleAbout`).
- The window, the menus, the files, find, go to line, the notices and the update flow are the shell's: see `src/Axit.Forms/CLAUDE.md` and `src/AxDown/CLAUDE.md`; axJSON adds nothing to them but data.

Structured mode (SPEC.md AJ-3, a form of real fields from templates) is not built; raw mode is version one (AJ-D3).
