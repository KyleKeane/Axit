# Axit.Forms, the shared window pieces

WinForms code every app of the bundle uses (`docs/axit/SPEC.md` AX-6.2, AX-6.4). It has no window of its own and no unit tests; the apps check it with NVDA. Nothing here references an app (M2); a piece arrived here on its second use, unchanged apart from its namespace (M3), so AxClaude's behaviour stayed the same.

- `OverlayPanel` and the records `Notice`, `OverlayChoice`, `OverlayInput`, `OverlayPreset`, `OverlaySession`: a window's own dialogs drawn inside the window in place of its main controls (AxClaude D23, bundle AX-7.2). The window shows one with `Populate`, sets its `AcceptButton` and `CancelButton` to `DefaultButton` and `CancelButton`, calls `FocusStart`, hides its other controls and disables its menu, and swallows its own shortcuts while the notice shows; `Chosen` tells it which button was pressed. The New session controls (`OverlaySession`) are AxClaude's and sit here because the panel moved whole.
- `ControlArea` and `ListKeys`: one list or one field in the input field's place, named with the question, new for every question (AxClaude D33). `ShowList`, `ShowField`, `Clear`, `Hold`, `FitHeight`; the lists are `ListBox` and `CheckedListBox` (NVDA read a `ListView` as a table).
- `Sounds`: the chimes and ticks through one winmm wave-out device kept open for the life of the process, every sound prepared once and queued with a single write (see the gotchas in `src/AxClaude/CLAUDE.md`). `Notice()` is the one every window uses.
- `EditPaging`: Page Up and Page Down in an edit control, one screen at a time and to the ends (`EM_LINESCROLL` against `EM_GETFIRSTVISIBLELINE`, since `EM_SCROLLCARET` is ignored without the focus).
- `StatusLayout`: fixed widths for two status labels when their texts do not fit (an unexposed label otherwise).
- `BundleKeys` (AX-8.2): the keys that mean the same in every window; `All` feeds the collision test.

Gotchas that live here are AxClaude's and are kept in `src/AxClaude/CLAUDE.md` (the EDIT control, NVDA's reading after a key press, the notice state as a flag in the form, `AcceptButton` for Enter, Alt and F10 as `SC_KEYMENU`).
