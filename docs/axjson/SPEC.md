# axJSON — Specification

axJSON is the third app of the Axit bundle (`docs/axit/SPEC.md`): an editor for JSON files for a blind developer who
uses the NVDA screen reader. Raw mode (AJ-1, AJ-2, AJ-4) is built and checked with NVDA (the record:
`docs/axit/nvda-test-record.md`); structured mode (AJ-3) is specified and not built (AJ-D3). The document is kept in step with the code (a behaviour
change updates it in the same commit). Requirements are numbered `AJ-n.m` and decisions `AJ-Dn`; the bundle's rules,
AX-7 (accessibility) and AX-8 (keys), and the editor shell's, axDown's AD-n in `docs/axdown/SPEC.md`, are cited by
number and not repeated. In axJSON's code and documents, "SPEC.md" means this file.

## 1. Summary

A blind developer edits JSON all day: Claude Code's `settings.json` and `.mcp.json`, VS Code's settings, `package.json`,
and the settings files of this bundle. axJSON opens one JSON file in one window. **Raw mode**, built, is the bundle's
text editor shell (the window axDown is) with the whole file in its plain edit control, plus keys that jump by the
file's structure (the next key, object, array, item, and the first error) and a Format command that lays the text out
and repairs what it safely can. **Structured mode**, planned, shows the same document as a form of ordinary Windows
input fields, one per value: a text field for a string or a number, a check box for a boolean, a list for an array, a
group for an object, laid out from a template when the file is one the app knows and from the document's own keys
otherwise. Native fields mean NVDA reads them by itself, and the caret cannot leave a value.

## 2. Goals and non-goals

- **G1 Two views, one document** (structured mode planned). Raw mode and structured mode edit the same text; switching
  re-parses or re-renders, and the file on disk changes only on Save. Text that does not parse stays in raw mode with
  the first error named.
- **G2 Native controls only.** The raw editor is the EDIT control axDown uses; the form will be made of EDIT, check box,
  list box and group controls (AX-7.1). No tree control of the app's own, no owner-drawn grid.
- **G3 Templated forms, not a schema engine** (planned). A template is a plain record in code. JSON Schema is a non-goal.
- **G4 Faithful files.** As axDown (AD G2): encoding, byte order mark and line endings come back as they were. Formatting
  changes the text only when the user asks for it.
- **G5 Shared behaviour.** The window, files, find, go to line, notices, settings, text size and the update check are
  the editor shell's (AX-6.2, AD-1 to AD-11), not copied: axJSON is a record of what differs (AJ-D4).
- **Non-goals.** Several files at once; comments and trailing commas as a dialect to preserve (they are repaired or
  reported); schema validation; colouring; a tree view; editing anything but JSON.

## 3. Scenarios

1. Open `.claude/settings.json`, press Ctrl+K until "permissions, object with 2 keys" is heard, Ctrl+K again into its
   keys, add an entry, Ctrl+Shift+F, save.
2. Open an `.mcp.json` with a comma missing between two members: Ctrl+E says "No next error", since the parser puts
   that comma in; Format says "Formatted, repaired: 1 missing comma"; the user saves. With a value missing, Ctrl+E lands
   on the place and says "Error, line 12: expected a value, found '}'".
3. (Planned) Switch a known settings file to structured mode: a form with a labelled field per setting.
4. (Planned) Open an unknown JSON file in structured mode: a form from its keys.

## 4. Functional requirements

### AJ-1 Launch

- AJ-1.1 `Axit.exe json [<file>]`; the console command `axjson`; the Start menu entry "Axit axJSON"; axJSON under Open
  with for `.json` in File Explorer (B-9: never the default). A `.json` file given to `Axit.exe` or `axit` alone
  reaches here (AX-1.2); any other file is axDown's.
- AJ-1.2 One window, one document, as axDown (AD-1.3).
- AJ-1.3 `AxJSONApp.Run(args)` is the entry point (AX-1.7): one call into the editor shell's `EditorHost.Run` with
  axJSON's `EditorApp` (its name, verb, settings file `axjson.json` and log `axjson.log`, file filter `.json` and
  `.jsonc` with `json` as the default extension, F1 text, guide, key pairs, resources and the Format command).

### AJ-2 Raw mode

- AJ-2.1 The editor, files, find, go to line, line and column on demand, the unsaved-changes and changed-on-disk
  questions, the notices, the settings, the status bar, the title and the Axit menu are the editor shell's exactly as
  axDown's AD-2 to AD-11 describe them, with "axJSON" where they say "axDown".
- AJ-2.2 Structure keys, window-specific (`AxJSONKeys`), Ctrl with a letter for the next element of that kind and
  Ctrl+Shift for the previous one (axDown's convention, AD-5): **K** key, **J** object, **L** array, **I** item,
  **E** error (AJ-D5). Each jump moves the caret to the element and speaks it through one notification (AX-7.3):
  - a key, at its opening quote: "model: opus" (a string without its quotes, a number or a literal as written, cut at
    60 characters, "empty string" for ""), or for a container "permissions, object with 2 keys", "deny, empty array";
  - an object, at its brace: "object with 5 keys", or with its key, "permissions, object with 2 keys"; the next element is the next one after the caret, as NVDA's browse mode and axDown's headings have it, so the caret on the first brace, already on the whole document, goes on to the object after it and Ctrl+Shift+J reaches the document from inside it; the whole
    document is the first object;
  - an array, at its bracket: "allow, array with 2 items", "empty array";
  - an item of an array, at its first character: "Item 2 of 2, Read";
  - the error, at the place the document stops being JSON: "Error, line 3: expected a comma or a closing brace after
    model, found a string"; "No next error" when the document is JSON, which is the quickest check of a file.
  Past the last element "No next key" and the like, the caret staying. The Navigate menu's Next and Previous submenus
  list Key, Object, Array, Item and Error with their keys, made from the pairs (AD-4.2). The same pairs work in every
  notice text (AX-7.2). The outline is forgiving (AJ-D6): comments, single-quoted strings, unquoted keys, a trailing
  comma and a missing comma between members or items read as JSON (they are the formatter's repairs); the first other
  problem ends the outline there, with the elements before it still listed.
- AJ-2.3 **Format**, Edit → Format (Ctrl+Shift+F): the document written out again with two spaces per level, one member
  or item per line, keys and strings in double quotes with JSON's escapes, the keys in their order, the text's own line
  ending and one at the end; empty containers as `{}` and `[]`. The repairs of AJ-2.2 are made on the way and named:
  "Formatted, repaired: 1 trailing comma, 1 unquoted key" (each kind with its count). "Formatted" when nothing needed
  repair, "Already formatted" when the text was already so (nothing changes, no unsaved-changes mark), "Nothing to
  format" for a blank document. A problem the parser cannot repair leaves the text as it is: "Not valid JSON: line 4,
  expected a value, found '}'". The whole text is replaced through the selection, one step for Undo, and the caret is
  put back at its offset.
- AJ-2.4 (Planned) The caret's path in the document ("permissions › allow › 3") on demand in the status bar.
- AJ-2.5 **Undo and redo** (specified and built 2026-10-21 at the user's ask, with axDown's AD-2.5): the history of
  the window's own that the shared shell keeps is axJSON's as it is axDown's: Ctrl+Z takes the last step back, Ctrl+Y
  puts it back, a Format is one step (AJ-2.3), and nothing reaches into an earlier file, window or session.

### AJ-3 Structured mode (planned, not built)

- AJ-3.1 A form in place of the editor: for each key a labelled control named with the key and the template's label,
  reached with Tab and Shift+Tab, grouped by object. Kinds: text (EDIT), number (EDIT, checked on leaving), boolean
  (check box), choice (a list of the template's values), list (a list box with Add, Edit, Remove), object (a group,
  or a button that opens the group in place).
- AJ-3.2 Templates in code for the files the bundle's users have: Claude Code's `settings.json` and `.mcp.json`, Axit's
  own settings files; a generic form from the keys for everything else. A template is matched by file name and by the
  keys found.
- AJ-3.3 Leaving the form writes the values back into the text at their places, keeping the file's formatting around
  them; a key the template does not know is kept untouched.
- AJ-3.4 The switch between the modes is one window key; the title says which mode shows.
- AJ-3.5 Every change made in the form is a step of the same history (AJ-2.5): a field's new value, a template's
  entry added or removed, each one step, undone with Ctrl+Z in the form as in the text, since the form writes into
  the text (AJ-3.3) and the history is the text's. The user's note of 2026-10-21: the undo matters most here, where
  a field is easy to change by mistake.

### AJ-4 Menus, keys, texts

- AJ-4.1 The menus are the shell's (AD-4) with axJSON's additions: Edit → Format (Ctrl+Shift+F) after the control's
  own items; Navigate → Next and Previous with the kinds of AJ-2.2; Help → JSON resources ▸ (JSON syntax,
  `https://www.json.org/json-en.html`; Claude Code settings, `https://code.claude.com/docs/en/settings`), as axDown's
  Help has Markdown resources.
- AJ-4.2 Keys (AX-8): the shell's (`EditorKeys`, included in `AxJSONKeys.All`), then Ctrl+K, Ctrl+J, Ctrl+L, Ctrl+I,
  Ctrl+E with Ctrl+Shift for the previous one, and Ctrl+Shift+F; the collision test covers them. Ctrl+J, a line feed
  in an EDIT control, is kept from the control as axDown keeps Ctrl+H and Ctrl+I (AD-5.1).
- AJ-4.3 The F1 text (`HelpText.Shortcuts`) names the shell's keys and AJ-2.2's; the user guide `docs/axjson/user-guide.md`
  is embedded (Help → User guide); About is the bundle's (AX-9.9).

## 5. Architecture

```
src/AxJSON.Core/   class library (net10.0): JsonScanner (tokens, forgiving), JsonParser and JsonTree (the tree, the
                   repairs, the first problem), JsonOutline (the elements for the keys, Axit.Core.Element), JsonFormat
src/AxJSON/        class library (net10.0-windows): AxJSONApp (Run, and axJSON's EditorApp record), AxJSONKeys,
                   HelpText (F1 text, the embedded user guide); references AxJSON.Core, Axit.Core, Axit.Forms
src/Axit.Forms/    TextEditorForm, EditorApp, EditorKeys, EditorHost: the text editor shell, shared with axDown
tests/AxJSON.Tests/ JsonOutlineTests, JsonFormatTests
docs/axjson/       this spec, user-guide.md (embedded), nvda-test-plan.md
```

The parser is the bundle's own (AJ-D6), not `System.Text.Json`: positions are character offsets into the text as the
edit control holds it, every input is accepted without an exception, and the repairs are the parser's own knowledge.

## 6. Design decisions

- **AJ-D1 Raw mode is the editor shell plus structure keys.** The editor NVDA reads best is already in the bundle;
  axJSON adds jumps and a formatter, nothing else to the editing.
- **AJ-D2 Structured mode is a form of real fields, from templates.** Real controls give field-bounded editing for free
  (the caret cannot leave a value), NVDA reads them natively, and a template is a record, not a schema engine. A tree
  or grid of the app's own was considered and dropped: it would be custom accessibility work for a worse result.
- **AJ-D3 Raw mode first.** axDown's remaining Markdown letters came first (2026-10-17), raw mode of axJSON next
  (2026-10-18), structured mode as a later version, axDown's own pattern.
- **AJ-D4 One editor shell, one app per file type, one file per window (2026-10-18).** From the outside the bundle is
  one editor for every file type: one program file, one installer, one update, and a path alone picks the app by its
  extension (AX-1.2). Inside, axDown's editor window moved to the shared projects on this second use (`TextEditorForm`,
  AX-6.2, AX-6.3), and each file type is one app over it as a record (`EditorApp`): axDown is the shell plus Markdown,
  axJSON the shell plus JSON plus Format, so that one app's folders hold everything about its file type. What differs
  per type is small and is the seam that existed already: the outline that finds the elements, the key pairs
  (`TextJump`), the Navigate menu made from them, and the type's own commands. Tabs for several files in one window
  were considered and dropped (B-14): one file per window, and the Axit menu (AX-9) lists the open windows.
- **AJ-D5 The letters.** A letter keeps its meaning where the concept exists in both editors: L is a list in axDown and
  an array here, I an item in both. A letter with no counterpart is the type's own: K is a key here (a link in axDown,
  NVDA's letter; JSON has no links), E the error. O is Open in every editor window, so an object is J, the letter of
  JSON's braces. Format is Ctrl+Shift+F next to Find.
- **AJ-D6 A forgiving parser of the bundle's own.** `System.Text.Json` reads bytes, throws on the first fault and knows
  no repairs; the navigation needs character offsets into the editor's text, an outline of a file that is nearly
  JSON, and the formatter the list of what it put right. A scanner and a recursive parser of two hundred lines, tested,
  give all three.

## 7. Open questions

- Structured mode's version and shape (AJ-3): a list box with Edit for an array of objects, or one group per item.
- The caret's path in the status bar (AJ-2.4).
- Whether a `.jsonc` file should keep its comments on Format rather than lose them as repairs.

## 8. Status

- 2.9.1 (2026-10-18): raw mode built (AJ-1, AJ-2.1 to AJ-2.3, AJ-4) on the editor shell shared the same day; released
  at the user's ask before its NVDA pass; `docs/axjson/nvda-test-plan.md` is the pass to run. 2.10.0: called beta in
  About and the changelog until that pass.
- 2026-10-21: undo and redo specified (AJ-2.5), the shared shell's history as in axDown, and the structured mode's changes
  as steps of it (AJ-3.5); AJ-2.5 built the same day with axDown's (Ctrl+Z, Ctrl+Y, Edit → Undo and Redo; the NVDA pass,
  test plan 3.1, is still to run), AJ-3.5 comes with the structured mode.
- 2026-10-22: released in 3.3.1 (3.3.0 tagged, not published). The whole NVDA test plan marked passed at the user's word, no step reported failing; the record by step is `docs/axit/nvda-test-record.md`.
