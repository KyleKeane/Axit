# Makes the files for the manual NVDA tests, and puts them back as they were after a test changed them.
# Every app folder is emptied except its _main.md, then filled again. Git ignores everything made here.
# Usage: .\manual_tests\prepare.ps1            (all apps)
#        .\manual_tests\prepare.ps1 axfile     (one app: axclaude, axdown, axjson, axgit, axterminal, axfile, axmedia, axpreview)
# Close the Axit windows that have these files open first: a file in use cannot be replaced.

param([string]$App = '')

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$apps = @('axclaude', 'axdown', 'axjson', 'axgit', 'axterminal', 'axfile', 'axmedia', 'axpreview')
if ($App -ne '') {
    if ($apps -notcontains $App) { throw "Unknown app '$App'. One of: $($apps -join ', ')" }
    $apps = @($App)
}

$utf8 = New-Object Text.UTF8Encoding $false

# ---- helpers ----

function New-Folder([string]$path) {
    New-Item -ItemType Directory -Force -Path $path | Out-Null
    $path
}

# A text file; lines end with CRLF unless -Lf; UTF-8 without a mark unless another encoding is given.
function Write-Text([string]$path, [string]$text, [Text.Encoding]$encoding = $utf8, [switch]$Lf) {
    New-Folder (Split-Path $path) | Out-Null
    $eol = if ($Lf) { "`n" } else { "`r`n" }
    $text = ($text -replace "`r`n", "`n") -replace "`n", $eol
    [IO.File]::WriteAllText($path, $text, $encoding)
}

function Write-Bytes([string]$path, [byte[]]$bytes) {
    New-Folder (Split-Path $path) | Out-Null
    [IO.File]::WriteAllBytes($path, $bytes)
}

function Set-Age([string]$path, [int]$days) {
    (Get-Item -Force $path).LastWriteTime = (Get-Date).AddDays(-$days)
}

function Set-Hidden([string]$path) {
    $item = Get-Item -Force $path
    $item.Attributes = $item.Attributes -bor [IO.FileAttributes]::Hidden
}

# ---- spoken sound files: Windows' own voice says what the file is, so the ear tells which file plays ----

Add-Type -AssemblyName System.Speech
$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
$rate = 16000
$bytesPerSecond = $rate * 2
$format = New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo $rate, ([System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen), ([System.Speech.AudioFormat.AudioChannel]::Mono)

function Get-Speech([string]$text) {
    $stream = New-Object IO.MemoryStream
    $synth.SetOutputToAudioStream($stream, $format)
    $synth.Speak($text)
    $synth.SetOutputToNull()
    , $stream.ToArray()
}

# A WAV file of 16 kHz mono speech: phrase n starts at n times $step seconds, the file lasts $phrases times $step seconds.
function New-SpokenWav([string]$path, [string[]]$phrases, [int]$step) {
    $pcm = New-Object IO.MemoryStream
    for ($i = 0; $i -lt $phrases.Count; $i++) {
        $start = $i * $step * $bytesPerSecond
        if ($pcm.Length -lt $start) { $pad = [int]($start - $pcm.Length); $pcm.Write((New-Object byte[] $pad), 0, $pad) }
        $speech = Get-Speech $phrases[$i]
        $pcm.Write($speech, 0, $speech.Length)
    }

    $end = $phrases.Count * $step * $bytesPerSecond
    if ($pcm.Length -lt $end) { $pad = [int]($end - $pcm.Length); $pcm.Write((New-Object byte[] $pad), 0, $pad) }
    $data = $pcm.ToArray()
    $out = New-Object IO.MemoryStream
    $w = New-Object IO.BinaryWriter $out
    $ascii = [Text.Encoding]::ASCII
    $w.Write($ascii.GetBytes('RIFF')); $w.Write([int](36 + $data.Length)); $w.Write($ascii.GetBytes('WAVE'))
    $w.Write($ascii.GetBytes('fmt ')); $w.Write([int]16); $w.Write([int16]1); $w.Write([int16]1)
    $w.Write([int]$rate); $w.Write([int]$bytesPerSecond); $w.Write([int16]2); $w.Write([int16]16)
    $w.Write($ascii.GetBytes('data')); $w.Write([int]$data.Length); $w.Write($data)
    $w.Flush()
    Write-Bytes $path $out.ToArray()
}

function Say-Time([int]$seconds) {
    $m = [math]::Floor($seconds / 60); $s = $seconds % 60
    if ($m -eq 0) { return "$s seconds" }
    $minutes = if ($m -eq 1) { '1 minute' } else { "$m minutes" }
    if ($s -eq 0) { return $minutes }
    "$minutes $s seconds"
}

# A file of $seconds seconds that says its title at the start and the time every 5 seconds after it.
function New-TimedWav([string]$path, [string]$title, [int]$seconds) {
    $phrases = @($title)
    for ($t = 5; $t -lt $seconds; $t += 5) { $phrases += (Say-Time $t) }
    New-SpokenWav $path $phrases 5
}

# A folder of numbered tracks, each saying "<who>, track n of m" and the time every 5 seconds.
function New-Tracks([string]$folder, [string]$who, [string]$prefix, [int]$count, [int]$seconds) {
    for ($n = 1; $n -le $count; $n++) {
        $name = '{0}-track-{1:D2}-of-{2:D2}.wav' -f $prefix, $n, $count
        New-TimedWav (Join-Path $folder $name) "$who, track $n of $count." $seconds
    }
}

function Copy-WindowsVideo([string]$folder) {
    $video = 'C:\Windows\SystemApps\Microsoft.Windows.CloudExperienceHost_cw5n1h2txyewy\media\oobe-intro.mp4'
    if (Test-Path $video) {
        Copy-Item $video (Join-Path $folder 'video-short-mp4-from-windows-setup.mp4')
    }
    else {
        Write-Text (Join-Path $folder 'no-video-found-put-a-short-mp4-here.txt') "This Windows has no sample video. Put any short .mp4 file in this folder for the video steps."
    }
}

# ---- shared texts ----

$readme = @'
# Readme for the preview tests

This file has headings, links, lists, a block quote and a table, for the navigation keys.

## Install

Run the installer and follow the steps. Check for an update now and then: the word update is here for the find test.

## Links

The project page is [Axit on GitHub](https://github.com/KyleKeane/Axit). A bare address: https://www.kylekeane.com.

## A list

- first item of the list
- second item of the list
- third item of the list

## A quote

> A block quote, for Ctrl+Q.

## A table

| Key | What it does |
| --- | --- |
| Ctrl+H | next heading |
| Ctrl+K | next link |

## Last heading

The last line of the readme. Another update is mentioned here, for F3.
'@

$plain = @'
Plain text file, line 1.
Line 2 of the plain text file.
Line 3: nothing special here.
Line 4, the last line.
'@

$appLog = @'
2026-10-22 09:00:01 INFO  The log file starts.
2026-10-22 09:00:02 INFO  Line 2 of the log.
2026-10-22 09:00:03 WARN  A warning on line 3.
2026-10-22 09:00:04 ERROR An error on line 4.
2026-10-22 09:00:05 INFO  The last line of the log.
'@

$settingsJson = @'
{
  "model": "opus",
  "permissions": { "allow": ["Bash", "Read"], "deny": [] },
  "count": 3,
  "on": true
}
'@

# The same with the comma after "opus" removed and a comma added after [] (AxJSON test plan).
$brokenJson = @'
{
  "model": "opus"
  "permissions": { "allow": ["Bash", "Read"], "deny": [], },
  "count": 3,
  "on": true
}
'@

$changeCmd = @'
@echo off
rem Adds one line to the file named on the next line, as another program would. Run it while the file is open in Axit.
echo Another line, added by the cmd file.>> "%~dp0{0}"
echo Added a line to {0}.
'@

function Write-ChangeCmd([string]$folder, [string]$target) {
    Write-Text (Join-Path $folder "append-a-line-to-$target.cmd") ($changeCmd -replace '\{0\}', $target)
}

# A long Markdown file for the editor's navigation keys (AxDown test plan sections 2 and 8).
$longLine = ('This line is longer than the window, so word wrap shows it on several rows and the caret test can tell. ' * 6).Trim()
$longMd = @"
# Long Markdown file for navigation

The first paragraph, one line.

$longLine

## Second heading, level 2

A paragraph of two lines,
the second line of it.

### Third heading, level 3

Links: [GitHub](https://github.com), a bare address https://www.example.com, and an angle address <https://example.org>.
An image, which is not a link: ![a picture](picture.png).

## Lists

- one
- two
  - two, nested
- three

1. numbered one
2. numbered two

## Quote and table

> A block quote, line one.
> Line two of the quote.

| Name | Value |
| --- | --- |
| alpha | 1 |
| beta | 2 |

## Code

``````
# not a heading, inside a fenced code block
[not a link](https://example.com)
``````

#### Fourth level heading

##### Fifth level heading

###### Sixth level heading

The last paragraph of the file.
"@

# ---- the apps ----

function Build-AxFile([string]$d) {
    # Moving around: type-ahead names, three levels down, an empty folder.
    $nav = New-Folder (Join-Path $d 'navigate-arrows-type-ahead-and-levels')
    foreach ($n in 'aardvark', 'apple', 'apricot', 'avocado', 'banana', 'blueberry', 'cherry', 'coconut', 'date', 'elderberry', 'fig') {
        Write-Text (Join-Path $nav "$n.txt") "The $n file, for typing the start of a name."
    }
    Write-Text (Join-Path $nav 'level-1-enter-me\level-2-enter-me\level-3-enter-me\you-reached-the-bottom.txt') 'Three levels down. Backspace three times goes back up.'
    Write-Text (Join-Path $nav 'level-1-enter-me\level-1-file.txt') 'A file on level 1.'
    Write-Text (Join-Path $nav 'level-1-enter-me\level-2-enter-me\level-2-file.txt') 'A file on level 2.'
    New-Folder (Join-Path $nav 'empty-folder-no-items') | Out-Null

    # Hidden entries and entries starting with a period (test plan 1.3a).
    $hid = New-Folder (Join-Path $d 'hidden-and-period-entries')
    Write-Text (Join-Path $hid 'visible-file.txt') 'Always shown.'
    Write-Text (Join-Path $hid 'hidden-file.txt') 'Shown only with Show hidden files and folders.'
    Set-Hidden (Join-Path $hid 'hidden-file.txt')
    Write-Text (Join-Path $hid '.period-file') 'Shown only with Show files and folders starting with a period.'

    # Enter, Ctrl+E and Windows' Open with (1.6, 3.10, 3.12).
    $open = New-Folder (Join-Path $d 'open-with-windows-and-editors')
    Write-Text (Join-Path $open 'markdown-for-enter-and-ctrl-e.md') "# A Markdown file`n`nEnter opens it in Windows' program for .md; Ctrl+E in AxDown."
    Write-Text (Join-Path $open 'json-ctrl-e-opens-axjson.json') $settingsJson
    Write-Text (Join-Path $open 'text-for-notepad-1.txt') 'The first text file for your own program, Notepad.'
    Write-Text (Join-Path $open 'text-for-notepad-2.txt') 'The second text file for your own program, Notepad.'
    Write-Text (Join-Path $open 'type-nothing-opens.axtest') 'No program opens this type: Enter shows the Windows dialog that asks for one.'

    # Filter, sort and find (1.10, 1.10a).
    $fs = New-Folder (Join-Path $d 'filter-sort-and-find')
    for ($n = 1; $n -le 5; $n++) { Write-Text (Join-Path $fs ("chapter-{0:D2}-notes.txt" -f $n)) "Chapter $n notes, for filter chap." }
    Write-Text (Join-Path $fs 'sort-oldest-and-smallest.txt') 'Small.'
    Set-Age (Join-Path $fs 'sort-oldest-and-smallest.txt') 30
    Write-Bytes (Join-Path $fs 'sort-middle-age-and-size.bin') (New-Object byte[] 102400)
    Set-Age (Join-Path $fs 'sort-middle-age-and-size.bin') 10
    Write-Bytes (Join-Path $fs 'sort-newest-and-largest.bin') (New-Object byte[] 1048576)
    Write-Text (Join-Path $fs 'find-me-somewhere-down-the-list.txt') 'Ctrl+F, type "find", Enter.'

    # Selection (1.13, 3.7b, 6.8, 6.9).
    $sel = New-Folder (Join-Path $d 'selection-space-shift-and-escape')
    for ($n = 1; $n -le 12; $n++) { Write-Text (Join-Path $sel ("select-{0:D2}.txt" -f $n)) "Entry $n for the selection tests." }
    New-Folder (Join-Path $sel 'select-folder-a') | Out-Null
    New-Folder (Join-Path $sel 'select-folder-b') | Out-Null

    # File operations (section 3).
    $ops = New-Folder (Join-Path $d 'file-operations')
    Write-Text (Join-Path $ops 'a.txt') 'The a.txt of the rename and copy steps.'
    Write-Text (Join-Path $ops 'rename-me.txt') 'F2 on me.'
    Write-Text (Join-Path $ops 'copy-me-then-paste.txt') 'Ctrl+C, Ctrl+V: a second file "copy-me-then-paste - 2.txt".'
    Write-Text (Join-Path $ops 'delete-me.txt') 'Delete on me, then look in the Recycle Bin.'
    Write-Text (Join-Path $ops 'delete-me-too.txt') 'Delete two selected entries at once.'
    Write-Text (Join-Path $ops 'move-into-new-folder-1.txt') 'Select me and the second, then Move to new folder.'
    Write-Text (Join-Path $ops 'move-into-new-folder-2.txt') 'Select me and the first, then Move to new folder.'
    Write-Text (Join-Path $ops 'not-a-zip.txt') 'Extract here on me says Not a zip file.'
    Write-Text (Join-Path $ops 'docs\plan.md') '# Plan, inside docs, for Compress to zip.'
    Write-Text (Join-Path $ops 'docs\notes.txt') 'Notes, inside docs.'
    Compress-Archive -Path (Join-Path $ops 'docs\*') -DestinationPath (Join-Path $ops 'existing-archive-extract-me.zip')
    New-Folder (Join-Path $ops 'paste-into-this-folder') | Out-Null
    New-Folder (Join-Path $ops 'in-use-open-a-terminal-in-me-then-rename') | Out-Null
    $big = New-Folder (Join-Path $ops 'big-folder-copy-twice-at-once')
    $block = New-Object byte[] 262144
    for ($n = 1; $n -le 200; $n++) { Write-Bytes (Join-Path $big ("part-{0:D3}.bin" -f $n)) $block }

    # Undo (3.14).
    $undo = New-Folder (Join-Path $d 'undo-ctrl-z')
    Write-Text (Join-Path $undo 'rename-then-undo.txt') 'Rename me, then Ctrl+Z.'
    Write-Text (Join-Path $undo 'copy-then-undo.txt') 'Copy and paste me, then Ctrl+Z.'
    Write-Text (Join-Path $undo 'delete-then-undo.txt') 'Delete me, then Ctrl+Z restores me.'
    Write-Text (Join-Path $undo 'delete-two-then-undo-1.txt') 'Delete me with the second, then Ctrl+Z.'
    Write-Text (Join-Path $undo 'delete-two-then-undo-2.txt') 'Delete me with the first, then Ctrl+Z.'
    Write-Text (Join-Path $undo 'move-two-then-undo-1.txt') 'Move to new folder with the second, then Ctrl+Z.'
    Write-Text (Join-Path $undo 'move-two-then-undo-2.txt') 'Move to new folder with the first, then Ctrl+Z.'
    Write-Text (Join-Path $undo 'rename-here-then-in-file-explorer.txt') 'Rename in AxFile, rename again in File Explorer, then Ctrl+Z: Not undone.'
    Write-Text (Join-Path $undo 'compress-then-undo\inside.txt') 'Compress the folder, then Ctrl+Z.'

    # The Actions list (section 6).
    $act = New-Folder (Join-Path $d 'actions-list')
    Write-Text (Join-Path $act 'start-here.txt') 'Make temp here with Ctrl+Shift+N, then Tab to the Actions list.'
    Write-Text (Join-Path $act 'subfolder-for-inner\readme.txt') 'Enter here, Ctrl+Shift+N inner.'
    Write-Text (Join-Path $act 'zip-two-files-1.txt') 'Select me and the second, Compress to zip.'
    Write-Text (Join-Path $act 'zip-two-files-2.txt') 'Select me and the first, Compress to zip.'

    # Changes from outside (1.9, 1.12).
    $out = New-Folder (Join-Path $d 'changes-from-file-explorer')
    Write-Text (Join-Path $out 'add-a-file-next-to-me-in-file-explorer.txt') 'Add a file in this folder from File Explorer, then Alt+Tab back.'
    Write-Text (Join-Path $out 'delete-this-folder-while-axfile-is-in-it\stand-here.txt') 'Stand in this folder in AxFile, delete the folder in File Explorer, Alt+Tab back.'

    # Hand-overs to AxMedia and AxPreview (section 4).
    $hand = New-Folder (Join-Path $d 'hand-over-to-axmedia-and-axpreview')
    Write-Text (Join-Path $hand 'README.md') $readme
    Write-Text (Join-Path $hand 'plain.txt') $plain
    Write-Text (Join-Path $hand 'app.log') $appLog
    Write-Bytes (Join-Path $hand 'binary-not-text.bin') ([byte[]](0, 1, 2, 3, 0, 255, 254, 0) * 64)
    New-Tracks (Join-Path $hand 'chapters-ctrl-enter-plays-me') 'Play folder chapter' 'chapter' 4 15
    New-Tracks (Join-Path $hand 'queue-folder-a-add-with-ctrl-shift-q') 'Queue folder A' 'folder-a' 3 10
    New-Tracks (Join-Path $hand 'queue-folder-b-add-with-ctrl-shift-q') 'Queue folder B' 'folder-b' 3 10
    Write-Text (Join-Path $hand 'no-media-here\only-text.txt') 'Ctrl+Enter here says No sound or video files here.'
}

function Build-AxMedia([string]$d) {
    $first = New-Folder (Join-Path $d 'first-queue-play-this-folder')
    New-Tracks $first 'First queue' 'first' 4 15
    Write-Text (Join-Path $first 'not-media-a-text-file.txt') 'Not sound: AxMedia says it cannot play this, and plays the next.'
    Write-Bytes (Join-Path $first 'cannot-decode-broken.opus') ([byte[]](1..255) * 8)

    New-Tracks (Join-Path $d 'replacement-queue-play-to-replace-the-first') 'Replacement queue' 'replacement' 3 10
    New-Tracks (Join-Path $d 'add-to-queue-folder-a') 'Added folder A' 'added-a' 3 10
    New-Tracks (Join-Path $d 'add-to-queue-folder-b') 'Added folder B' 'added-b' 3 10

    $seek = New-Folder (Join-Path $d 'seek-and-speed-one-long-file')
    New-TimedWav (Join-Path $seek 'long-file-3-minutes-time-spoken-every-5-seconds.wav') 'Long file for seeking and speed. 0 seconds.' 180

    $end = New-Folder (Join-Path $d 'end-of-queue-two-short-files')
    New-SpokenWav (Join-Path $end 'short-1-of-2.wav') @('Short file 1 of 2.') 3
    New-SpokenWav (Join-Path $end 'short-2-of-2-last.wav') @('Short file 2 of 2, the last. End of queue comes next.') 4

    $video = New-Folder (Join-Path $d 'video')
    Copy-WindowsVideo $video
    New-SpokenWav (Join-Path $video 'sound-after-the-video.wav') @('The sound file after the video.') 4

    Write-Text (Join-Path $d 'no-media-folder\only-text.txt') 'Ctrl+Shift+O on this folder says No sound or video files.'
}

function Build-AxPreview([string]$d) {
    Write-Text (Join-Path $d 'README.md') $readme
    Write-Text (Join-Path $d 'plain.txt') $plain
    Write-Text (Join-Path $d 'app.log') $appLog
    Write-Text (Join-Path $d 'data.json') $settingsJson
    Write-Bytes (Join-Path $d 'binary-not-text.bin') ([byte[]](0, 1, 2, 3, 0, 255, 254, 0) * 64)
    Write-ChangeCmd $d 'README.md'
    # Over 32 MB of text: the "Too large" notice.
    $line = 'A line of the file that is too large for AxPreview, over 32 megabytes of text in all.' + "`r`n"
    $chunk = $utf8.GetBytes($line * 4000)
    $stream = [IO.File]::Create((Join-Path $d 'too-large-over-32-mb.txt'))
    try { for ($i = 0; $i -lt 100; $i++) { $stream.Write($chunk, 0, $chunk.Length) } } finally { $stream.Dispose() }
}

function Build-AxDown([string]$d) {
    Write-Text (Join-Path $d 'plain.txt') $plain
    Write-Text (Join-Path $d 'bom.md') "# UTF-8 with a byte order mark`n`nLF line endings only. Saved, it keeps both." (New-Object Text.UTF8Encoding $true) -Lf
    Write-Text (Join-Path $d 'wide.txt') "UTF-16 little endian text.`nSaved, it stays UTF-16 with its mark." ([Text.Encoding]::Unicode)
    $e = [char]0xE9
    Write-Text (Join-Path $d 'ansi.txt') "Windows-1252 text with an accented letter: caf$e.`nThe letter survives a save." ([Text.Encoding]::GetEncoding(1252))
    Write-Text (Join-Path $d 'long.md') $longMd
    Write-Text (Join-Path $d 'change-me-on-disk.txt') "Open me, then run the cmd file next to me.`nLine 2."
    Write-ChangeCmd $d 'change-me-on-disk.txt'
    Write-Bytes (Join-Path $d 'not-text-image.png') ([byte[]](137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13) * 32)
    Write-Text (Join-Path $d 'notes.md') "# Notes`n`nThe file the Axit menu steps open, close and reopen."
}

function Build-AxJson([string]$d) {
    Write-Text (Join-Path $d 'settings.json') $settingsJson
    Write-Text (Join-Path $d 'broken.json') $brokenJson
    Write-Text (Join-Path $d 'one-line-format-me.json') '{"model":"opus","permissions":{"allow":["Bash","Read"],"deny":[]},"count":3,"on":true}'
    Write-Text (Join-Path $d 'already-formatted.json') "{`n  `"name`": `"formatted`",`n  `"items`": [`n    1,`n    2`n  ]`n}`n"
    Write-Text (Join-Path $d 'notes.txt') 'A text file: Open with Axit lists AxDown alone for it.'
    Write-ChangeCmd $d 'settings.json'
}

function Build-AxGit([string]$d) {
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
        Write-Text (Join-Path $d 'git-not-found.txt') 'Git was not found, so no scratch repository was made.'
        return
    }

    $remote = Join-Path $d 'scratch-remote-for-fetch-pull-push.git'
    $repo = Join-Path $d 'scratch-repository-with-changes'
    New-Folder (Join-Path $d 'not-a-repository') | Out-Null
    Write-Text (Join-Path $d 'not-a-repository\plain.txt') 'This folder is not a git repository.'

    $ErrorActionPreference = 'Continue'
    $mail = 'tester' + '@' + 'example.invalid'
    git init -q --bare -b main $remote
    git init -q -b main $repo
    git -C $repo config user.name 'Axit tester'
    git -C $repo config user.email $mail
    Write-Text (Join-Path $repo 'README.md') "# Scratch repository`n`nFor the AxGit tests."
    Write-Text (Join-Path $repo 'settings.json') $settingsJson
    Write-Text (Join-Path $repo 'tracked-change-me.txt') "Line 1.`nLine 2."
    Write-Text (Join-Path $repo 'tracked-discard-my-change.txt') 'The original text.'
    git -C $repo add -A
    git -C $repo commit -q -m 'First commit: the readme and three files'
    for ($n = 2; $n -le 5; $n++) {
        Add-Content -Path (Join-Path $repo 'README.md') -Value "Line added by commit $n."
        git -C $repo commit -q -am "Commit $n of the history"
    }
    git -C $repo remote add origin $remote
    git -C $repo push -q -u origin main 2>$null
    git -C $repo branch second-branch-to-switch-to
    # The changes the Changes list shows: two modified files, one staged file, one untracked file.
    Add-Content -Path (Join-Path $repo 'tracked-change-me.txt') -Value 'Line 3, an unstaged change.'
    Set-Content -Path (Join-Path $repo 'tracked-discard-my-change.txt') -Value 'A change to discard with Delete.'
    Write-Text (Join-Path $repo 'staged-new-file.txt') 'Staged already.'
    git -C $repo add staged-new-file.txt
    Write-Text (Join-Path $repo 'untracked-new-file.txt') 'Not tracked: Enter shows this text.'
    $ErrorActionPreference = 'Stop'
}

function Build-AxTerminal([string]$d) {
    $start = New-Folder (Join-Path $d 'start-here')
    New-Folder (Join-Path $start 'subfolder-for-cd') | Out-Null
    Write-Text (Join-Path $start 'ask-a-question.ps1') "`$answer = Read-Host 'Continue? (y/n)'`nWrite-Output `"You answered `$answer`""
    Write-Text (Join-Path $start 'print-many-lines.ps1') '1..60 | ForEach-Object { "Line $_ of 60" }'
    Write-Text (Join-Path $start 'run-for-30-seconds.ps1') "Write-Output 'Running for 30 seconds'`nStart-Sleep 30`nWrite-Output 'Finished'"
    Write-Text (Join-Path $start 'fail-with-exit-code-3.cmd') "@echo Failing on purpose.`r`n@exit /b 3"
    Write-Text (Join-Path $start 'hello.txt') 'Hello from a file. Get-Content hello.txt'
}

function Build-AxClaude([string]$d) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    # Claude Code remembers the folders it was trusted in, so a new name each time brings its trust question back.
    New-Folder (Join-Path $d "never-used-with-claude-$stamp") | Out-Null
    Write-Text (Join-Path $d "never-used-with-claude-$stamp\hello.txt") 'A file for Claude to read.'
    New-Folder (Join-Path $d "empty-folder-nothing-to-continue-$stamp") | Out-Null
    $proj = New-Folder (Join-Path $d 'project-for-conversations')
    Write-Text (Join-Path $proj 'notes.md') "# Notes`n`nAsk Claude to add a line to this file."
    Write-Text (Join-Path $proj 'plain.txt') $plain
}

# ---- empty each app folder but its _main.md, then fill it ----

foreach ($name in $apps) {
    $folder = New-Folder (Join-Path $root $name)
    Get-ChildItem -Force $folder | Where-Object { $_.Name -ne '_main.md' } | Remove-Item -Recurse -Force
    Write-Host "Making $name..."
    switch ($name) {
        'axclaude' { Build-AxClaude $folder }
        'axdown' { Build-AxDown $folder }
        'axjson' { Build-AxJson $folder }
        'axgit' { Build-AxGit $folder }
        'axterminal' { Build-AxTerminal $folder }
        'axfile' { Build-AxFile $folder }
        'axmedia' { Build-AxMedia $folder }
        'axpreview' { Build-AxPreview $folder }
    }
}

$synth.Dispose()
Write-Host 'Done. Each folder has a _main.md with its steps.'
