# Third-party notices

Axit's own code is under the MIT licence in `LICENSE`. Axit uses no third-party packages. What it contains from others:

## .NET and Windows Forms

`Axit.exe` is a self-contained .NET program: it carries the .NET runtime and Windows Forms inside it, unchanged, as
Microsoft publishes them.

- .NET runtime: <https://github.com/dotnet/runtime>
- Windows Forms: <https://github.com/dotnet/winforms>

```
The MIT License (MIT)

Copyright (c) .NET Foundation and Contributors

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

The .NET runtime itself includes components under other open-source licences, listed in .NET's own notices:
<https://github.com/dotnet/runtime/blob/main/THIRD-PARTY-NOTICES.TXT> and
<https://github.com/dotnet/winforms/blob/main/THIRD-PARTY-NOTICES.TXT>.

## Unicode character data

The table of wide characters in `src/Axit.Core/Vt/Wcwidth.cs`, which tells how many columns a character takes in a
console, is derived from the Unicode Character Database (East Asian Width), © Unicode, Inc., used under the Unicode
License v3: <https://www.unicode.org/license.txt>.

## Programs Axit works with, not part of it

Axit runs, but does not contain, programs the user installs: Claude Code (Anthropic), git, KeePassXC's command-line
tool, PowerShell, and the user's own shells and programs. Each is under its maker's licence and terms; `PRIVACY.md`
says when Axit runs each one.

## Tools used only to build and test

The tests use xunit and the .NET test SDK (Apache 2.0 and MIT), and GitHub Actions builds the releases with
GitHub's own actions (MIT). None of them is shipped in `Axit.exe` or the zip.
