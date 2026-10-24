# The app kit: a new app as a description

Asked by the user on 2026-10-24: "an extensive consolidation and refactor to minimize the codebase … as much reusable
composable modular infrastructure as possible … future apps extremely lightweight … through easy processes that do
not require you to ever touch core code unless there is a new control or interaction". Specification first; built
after the security fixes, one app per step. Requirements AK-n.m. The evidence (line counts, the copies) is in
`inventory-2026-10-24.md` and the review of the windows the same day.

## 1. What an app is today, and what it becomes

Today a key is written five times: a constant in the window's key table, its entry in `All`, a menu item, a `case` in
`ProcessWindowKey`, and a line in the F1 text; and a new app is added in seventeen places across the repository.
After the kit:

- AK-1.1 **An app is**: its Core (pure logic, tested), one description file `AxThingApp.cs` (its name, its archetype,
  its commands, its views or editor kind), its window only where it has behaviour no archetype has, and its documents
  (spec, guide, test plan). Nothing in `Axit.Core` or `Axit.Forms` changes for it unless it brings a control or an
  interaction no app has yet.
- AK-1.2 **Registered once**: its line in `Dispatch.Apps` (name, verb, project, group, what it opens, Show in) and a
  `tools/new-app.ps1` run, which writes the folders, the empty documents, the description, and the few text places that
  must stay text (the READMEs, the root notes, the issue forms, the changelog). Everything else reads `Dispatch.Apps`
  (AK-4).

## 2. Commands: one list per window

- AK-2.1 `Command(Label, Key, Scope, Run, Enabled?, Help?, Mark)`: the menu's label with its letter, the key (from the
  window's key table, so that the collision test keeps static data), where the key acts (the window, the list, the
  field, the details), what runs, when it is enabled, the F1 line, and its mark for MCP (read, open, change, act;
  `mcp.md` MC-3.1) and whether it is secret.
- AK-2.2 `MenuSpec(Label, Items)`: a menu of commands, separators and submenus. A window describes its menus as specs.
- AK-2.3 **The window base builds from them**: the menu bar with the bundle's frame (the first menu's standard shape
  ending in Exit, Options with Text, General and Developer, the Axit menu, Help); the key handling (`RunCommand`, one
  loop over the commands in scope order, the window's own keys first, then the focused control's); the F1 text (the
  bundle's section, the app's commands from their help, a short hand-written paragraph for the controls' own keys);
  the command palette (it already reads the menus). Menu, key, F1, palette and MCP cannot disagree, since there is one
  list.
- AK-2.4 Tests from the data: every command's key is in the F1 text; letters are unique within a menu; no key equals a
  bundle key; every act command is marked.

## 3. Archetypes

- AK-3.1 The windows keep their archetype bases and gain one: **`ListWindow`** (shapes of axGIT, axProcess, axMedia,
  axCalc and most of axFile): one list with views (`ListViewState` and the refresh, show, stale and cycle flow, once),
  an optional details pane, an optional field, Tab between them, find in the list or the details, the default
  bodies of the base's hooks (hide for a notice, the font, the focus).
- AK-3.2 The others stay: `TextEditorForm` with `EditorApp` (axDown is 112 lines; the model of the kit), `ConsoleForm`,
  `ItemForm`, `HotkeyForm`. Each takes commands as AK-2 does.
- AK-3.3 Kept as overrides, never flattened: the behaviours tuned under NVDA (a window's keys before the control's, the
  list that must not ding, axFile's own Tab, an announcement on the focused control, axMedia showing without
  activation, menus filled as they open).

## 4. One place for an app

- AK-4.1 Read from `Dispatch.Apps` (extended with the description, the icon's letters and colour, the file types, Open
  with): `run.ps1` (no hand-kept verb list), `install.ps1` (through an `Axit.exe apps` listing), `make-icon.ps1`, the
  project references (an MSBuild glob), `Program.cs` (one table), `KeyTests` (one entry per app).
- AK-4.2 Kept as text, checked by `InventoryTests`: the READMEs, the root notes, the issue forms, the changelog, the
  specs' lists (which point to `Dispatch.Apps` rather than repeat it, at the user's word).

## 5. Order, one step per commit, each shippable

1. The records, the builders and the tests, unused.
2. axCalc on them (the smallest), then axProcess, then axGIT moving the views flow into `ListWindow`, then axMedia.
3. axTask and axPass through `ItemForm`, axStart and axClip through `HotkeyForm`, axTerminal and axClaude, the editors.
4. axFile last (the most states).
5. In parallel, the registration places of AK-4, then `tools/new-app.ps1`.

Every step: the app's NVDA test plan passed before the next, as the bundle's rule says.
