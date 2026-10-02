<#
.SYNOPSIS
  Installs Axit for the current user, or removes it with -Uninstall. No administrator rights are needed.

.DESCRIPTION
  Run it from the folder that holds Axit.exe: the extracted zip, or publish\win-x64 after publish.ps1.
  Axit is one program with several apps inside (docs/axit/SPEC.md AX-2). It copies Axit to
  %LOCALAPPDATA%\Programs\Axit and creates the ways to start each app:
    - a Start menu entry per app, "Axit AxClaude" and "Axit AxDown" (type axc or axd in the Start menu),
    - File Explorer entries: "Open in AxClaude" in the right-click menu of folders (and of a folder's background),
      "Open in AxDown" in the right-click menu of files, and AxDown under "Open with" for .md, .markdown and .txt,
    - the console commands axclaude, axdown and axit (a folder opens AxClaude, a file AxDown): one-line shims in
      %USERPROFILE%\.local\bin, the folder that Claude Code's installer already puts on PATH.
  It also lists Axit in Settings, Apps (Add or remove programs), whose Uninstall runs install.cmd -Uninstall.
  -Uninstall removes all of that. The settings in %APPDATA%\Axit and the logs in %LOCALAPPDATA%\Axit stay.
  An installation of AxClaude 1.x, from before the bundle, is removed completely first; its settings file stays in
  %APPDATA%\AxClaude, and AxClaude copies it on its first start under Axit.

.EXAMPLE
  .\install.ps1                 # install or update
  .\install.ps1 -Uninstall      # remove
  .\install.ps1 -NoContextMenu  # install without the File Explorer entries
  .\install.ps1 -NoStartMenu    # install without the Start menu entries

  install.cmd, next to this script, runs it with the execution policy bypassed: double-click it, or install.cmd -Uninstall.
  Downloaded on its own from the releases page, install.cmd fetches the latest release and runs this script from it.

  The apps' own Update now (Help menu) runs the downloaded version's copy of this script as
    install.ps1 -WaitForProcess <pid> [-Start <folder> [-ContinueConversation]] [-StartFile <file>] -LogFile <file>
  which waits for the running app to end, installs, and starts AxClaude on the folder or AxDown on the file.
  AxClaude 1.7.0 calls it with -Start (AX-2.7), which is how an installed AxClaude becomes Axit.
#>
[CmdletBinding()]
param(
    [switch]$Uninstall,
    [switch]$NoContextMenu,
    [switch]$NoStartMenu,
    [int]$WaitForProcess,
    [string]$Start,
    [switch]$ContinueConversation,
    [string]$StartFile,
    [string]$LogFile
)

$ErrorActionPreference = 'Stop'

function Say([string]$text) {
    Write-Host $text
    if ($LogFile) {
        try { Add-Content -Path $LogFile -Value "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') $text" -Encoding utf8 } catch { }
    }
}

# Any error that nothing below handles is said and logged: the updater runs this script without a window, so the
# log is the only place it would show.
trap {
    Say "Axit was not installed: $($_.Exception.Message)"
    exit 1
}

if ($WaitForProcess) {
    Say "Waiting for the app (process $WaitForProcess) to close..."
    try { Wait-Process -Id $WaitForProcess -Timeout 120 -ErrorAction Stop } catch { }
    if (Get-Process -Id $WaitForProcess -ErrorAction SilentlyContinue) {
        Say "The app is still running after two minutes. The update was not installed; start it from the Help menu again."
        exit 1
    }
}

$source = Split-Path -Parent $MyInvocation.MyCommand.Path
$target = Join-Path $env:LOCALAPPDATA 'Programs\Axit'
$exe = Join-Path $target 'Axit.exe'
$bin = Join-Path $env:USERPROFILE '.local\bin'
$programs = [Environment]::GetFolderPath('Programs')
$classes = 'HKCU:\Software\Classes'
# The entry in Settings, Apps (Add or remove programs) for this user; its Uninstall runs install.cmd -Uninstall.
$appsKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Axit'

# The apps (AX-2): the verb Axit.exe takes, the Start menu entry, the console command, the description and the icon.
# A new app of the bundle adds a block here.
$apps = @(
    @{ Verb = 'claude'; Start = 'Axit AxClaude.lnk'; Shim = 'axclaude.cmd'; Description = 'AxClaude, an accessible front end for Claude Code'; Icon = "$exe,0" },
    @{ Verb = 'down'; Start = 'Axit AxDown.lnk'; Shim = 'axdown.cmd'; Description = 'AxDown, a plain editor for Markdown and text files'; Icon = (Join-Path $target 'AxDown.ico') }
)
# File Explorer: folders open in AxClaude, files in AxDown; AxDown is a program for these file types (Open with).
$folderKeys = @("$classes\Directory\shell\AxClaude", "$classes\Directory\Background\shell\AxClaude")
$fileKey = "$classes\*\shell\AxDown"
$progId = 'Axit.AxDown'
$progIdKey = "$classes\$progId"
$extensions = '.md', '.markdown', '.txt'

# What AxClaude 1.x put on the machine under its own names; the rest of its places are the same as Axit's.
$oldTarget = Join-Path $env:LOCALAPPDATA 'Programs\AxClaude'
$oldExe = Join-Path $oldTarget 'AxClaude.exe'
$oldAppsKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\AxClaude'
$oldStartMenu = Join-Path $programs 'AxClaude.lnk'

# Everything an installation puts on the machine, this version's and 1.x's, so that one removal clears both. The
# Open with values live inside the file types' own keys and are removed one by one (Remove-Installation).
$places = @($target, $oldTarget, $appsKey, $oldAppsKey, $oldStartMenu, $fileKey, $progIdKey) + $folderKeys +
    @($apps | ForEach-Object { Join-Path $programs $_.Start }) +
    @(Join-Path $bin 'axit.cmd') + @($apps | ForEach-Object { Join-Path $bin $_.Shim })

function Get-Version([string]$path) {
    (Get-Item $path).VersionInfo.FileVersion -replace '^(\d+\.\d+\.\d+).*', '$1'
}

function Test-Running {
    [bool](Get-Process -Name Axit, AxClaude -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $exe -or $_.Path -eq $oldExe })
}

# Removes everything an installation put on the machine, whatever version made it. The settings and the logs stay.
# Windows can hold the executable for a moment after the app exits, so a folder is tried again for a few seconds.
# Registry paths go through -LiteralPath: the file key has a * in it, which PowerShell would otherwise expand.
function Remove-Installation {
    foreach ($folder in $target, $oldTarget) {
        for ($attempt = 1; Test-Path -LiteralPath $folder; $attempt++) {
            try { Remove-Item -LiteralPath $folder -Recurse -Force }
            catch {
                if ($attempt -ge 10) { throw }
                Start-Sleep -Seconds 1
            }
        }
    }
    foreach ($item in $places) {
        if (Test-Path -LiteralPath $item) { Remove-Item -LiteralPath $item -Recurse -Force }
    }
    foreach ($extension in $extensions) {
        $openWith = "$classes\$extension\OpenWithProgids"
        if ((Test-Path -LiteralPath $openWith) -and (Get-ItemProperty -LiteralPath $openWith).PSObject.Properties[$progId]) {
            Remove-ItemProperty -LiteralPath $openWith -Name $progId
        }
    }
}

if ($Uninstall) {
    # A running app holds its folder: then nothing is removed, so the Apps entry and install.cmd stay to try again.
    if (Test-Running) {
        Say "Axit is still running, so nothing was removed. Close it and uninstall again."
        exit 1
    }

    Remove-Installation
    Say "Axit was removed. Settings in $env:APPDATA\Axit and the logs in $env:LOCALAPPDATA\Axit were kept."
    exit 0
}

$sourceExe = Join-Path $source 'Axit.exe'
if (-not (Test-Path -LiteralPath $sourceExe)) {
    Say "Axit.exe was not found next to this script in $source."
    exit 1
}

$sameFolder = [string]::Equals((Resolve-Path $source).Path.TrimEnd('\'), $target.TrimEnd('\'), [StringComparison]::OrdinalIgnoreCase)
if (-not $sameFolder) {
    # A previous installation, of Axit or of AxClaude 1.x, is removed completely first, so nothing of it stays
    # behind; then this one is installed fresh. The updater comes here too, after the running app has closed.
    $found = @($places | Where-Object { Test-Path -LiteralPath $_ })
    if ($found) {
        if (Test-Running) {
            Say "Axit is still running, so nothing was changed. Close it and run this again."
            exit 1
        }

        $what = if (Test-Path -LiteralPath $exe) { 'Axit ' + (Get-Version $exe) }
            elseif (Test-Path -LiteralPath $oldExe) { 'AxClaude ' + (Get-Version $oldExe) }
            else { 'what is left of an earlier installation' }
        Say "Removing $what before installing..."
        Remove-Installation
    }

    New-Item -ItemType Directory -Force $target | Out-Null
    foreach ($name in 'Axit.exe', 'AxDown.ico', 'README.md', 'LICENSE', 'install.ps1', 'install.cmd') {
        $file = Join-Path $source $name
        if (Test-Path -LiteralPath $file) { Copy-Item -LiteralPath $file $target -Force }
    }
}

# Files extracted from a downloaded zip carry the "downloaded from the internet" mark, which makes SmartScreen warn
# about an unknown publisher on every start. The installed copies lose the mark, so the warning does not come back.
try { Get-ChildItem $target -File | Unblock-File -ErrorAction Stop } catch { }

New-Item -ItemType Directory -Force $bin | Out-Null
Set-Content -Path (Join-Path $bin 'axit.cmd') -Value @('@echo off', "`"$exe`" %*") -Encoding ascii
foreach ($app in $apps) {
    Set-Content -Path (Join-Path $bin $app.Shim) -Value @('@echo off', "`"$exe`" $($app.Verb) %*") -Encoding ascii
}

if (-not $NoStartMenu) {
    $shell = New-Object -ComObject WScript.Shell
    foreach ($app in $apps) {
        $link = $shell.CreateShortcut((Join-Path $programs $app.Start))
        $link.TargetPath = $exe
        $link.Arguments = $app.Verb
        $link.WorkingDirectory = $target
        $link.Description = $app.Description
        $link.IconLocation = $app.Icon
        $link.Save()
    }
}

if (-not $NoContextMenu) {
    foreach ($key in $folderKeys) {
        New-Item -Path $key -Force | Out-Null
        Set-ItemProperty -LiteralPath $key -Name '(default)' -Value 'Open in AxClaude'
        Set-ItemProperty -LiteralPath $key -Name 'Icon' -Value "`"$exe`",0"
        $command = Join-Path $key 'command'
        New-Item -Path $command -Force | Out-Null
        $argument = if ($key -like '*Background*') { '%V' } else { '%1' }
        Set-ItemProperty -LiteralPath $command -Name '(default)' -Value "`"$exe`" claude `"$argument`""
    }

    $downIcon = Join-Path $target 'AxDown.ico'
    New-Item -Path $fileKey -Force | Out-Null
    Set-ItemProperty -LiteralPath $fileKey -Name '(default)' -Value 'Open in AxDown'
    Set-ItemProperty -LiteralPath $fileKey -Name 'Icon' -Value $downIcon
    New-Item -Path (Join-Path $fileKey 'command') -Force | Out-Null
    Set-ItemProperty -LiteralPath (Join-Path $fileKey 'command') -Name '(default)' -Value "`"$exe`" down `"%1`""

    # The ProgID that Open with and Default apps know AxDown by (AX-2.3, B-9): the user makes it the default, never this script.
    New-Item -Path $progIdKey -Force | Out-Null
    Set-ItemProperty -LiteralPath $progIdKey -Name '(default)' -Value 'Text document (AxDown)'
    Set-ItemProperty -LiteralPath $progIdKey -Name 'FriendlyTypeName' -Value 'Text document (AxDown)'
    New-Item -Path (Join-Path $progIdKey 'DefaultIcon') -Force | Out-Null
    Set-ItemProperty -LiteralPath (Join-Path $progIdKey 'DefaultIcon') -Name '(default)' -Value $downIcon
    New-Item -Path (Join-Path $progIdKey 'Application') -Force | Out-Null
    Set-ItemProperty -LiteralPath (Join-Path $progIdKey 'Application') -Name 'ApplicationName' -Value 'AxDown'
    Set-ItemProperty -LiteralPath (Join-Path $progIdKey 'Application') -Name 'ApplicationIcon' -Value $downIcon
    New-Item -Path (Join-Path $progIdKey 'shell\open\command') -Force | Out-Null
    Set-ItemProperty -LiteralPath (Join-Path $progIdKey 'shell\open\command') -Name '(default)' -Value "`"$exe`" down `"%1`""
    foreach ($extension in $extensions) {
        $openWith = "$classes\$extension\OpenWithProgids"
        New-Item -Path $openWith -Force | Out-Null
        New-ItemProperty -LiteralPath $openWith -Name $progId -Value ([byte[]]@()) -PropertyType None -Force | Out-Null
    }
}

New-Item -Path $appsKey -Force | Out-Null
$uninstallCmd = Join-Path $target 'install.cmd'
$appsUninstall = if (Test-Path -LiteralPath $uninstallCmd) { "`"$uninstallCmd`" -Uninstall" }
    else { "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$(Join-Path $target 'install.ps1')`" -Uninstall" }
$entries = [ordered]@{
    DisplayName = 'Axit'
    DisplayVersion = (Get-Version $exe)
    Publisher = 'Dr. Kyle Keane'
    DisplayIcon = "`"$exe`",0"
    InstallLocation = $target
    UninstallString = $appsUninstall
    URLInfoAbout = 'https://github.com/KyleKeane/Axit'
}
foreach ($name in $entries.Keys) { Set-ItemProperty -LiteralPath $appsKey -Name $name -Value $entries[$name] }
foreach ($name in 'NoModify', 'NoRepair') { New-ItemProperty -LiteralPath $appsKey -Name $name -Value 1 -PropertyType DWord -Force | Out-Null }
New-ItemProperty -LiteralPath $appsKey -Name 'EstimatedSize' -Value ([int]((Get-Item $exe).Length / 1KB)) -PropertyType DWord -Force | Out-Null

Say "Installed Axit to $target"
Say "Settings, Apps: Axit, with Uninstall"
if (-not $NoStartMenu) { Say "Start menu: Axit AxClaude, Axit AxDown (type axc or axd to find them)" }
if (-not $NoContextMenu) { Say "File Explorer: right-click a folder, Open in AxClaude; right-click a file, Open in AxDown (also under Open with for .md, .markdown and .txt)" }
Say "Console: axclaude [folder], axdown [file], or axit <folder or file> (shims in $bin)"
if (-not (($env:Path -split ';') -contains $bin)) {
    Say "Note: $bin is not on PATH in this console. Open a new console, or add it, before using the commands."
}

if ($Start) {
    $arguments = @('claude', "`"$Start`"")
    if ($ContinueConversation) { $arguments += @('--', '--continue') }
    Say "Starting AxClaude on $Start"
    Start-Process -FilePath $exe -ArgumentList $arguments -WorkingDirectory $target
}

if ($StartFile) {
    Say "Starting AxDown on $StartFile"
    Start-Process -FilePath $exe -ArgumentList @('down', "`"$StartFile`"") -WorkingDirectory $target
}
