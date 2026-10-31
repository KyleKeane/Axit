<#
.SYNOPSIS
  Runs the app scripts with NVDA listening: the NVDA test plans as the assistant runs them (docs/axit/automation.md,
  section 3).

.DESCRIPTION
  Builds Axit.exe and the runner into %TEMP%\axit-nvda (never the running app's bin), gathers the scripts of
  tests\scripts\<app> and tests\nvda-scripts (all apps, or the one named), and runs them with tools\AxAutomate --nvda:
  the windows come to the front of the user's desktop, the keys go through SendInput, and every announcement and focus
  move a script expects must also be spoken by NVDA, read from the axitCapture add-on (tools\nvda-capture).

  It takes the keyboard and the mouse. Run it only at the user's word, after asking them to step away; Windows' voice
  repeats the warning through the run and says when it is over. The report is %TEMP%\axit-nvda\report.md; the result
  lines are printed without the account's paths. Exit code 0 when every script passed.

.EXAMPLE
  .\nvda-pass.ps1                 # every app's scripts and the NVDA-only scripts
  .\nvda-pass.ps1 axCalc          # tests\scripts\axcalc, and the NVDA-only scripts whose names start with axcalc
  .\nvda-pass.ps1 -NoBuild axDown # the last build again
  .\nvda-pass.ps1 -Skip axClaude  # every app but axClaude
#>
[CmdletBinding()]
param(
    # One app, by its folder name under tests\scripts (axCalc, axDown, ...); empty for all.
    [Parameter(Position = 0)]
    [string]$App = '',
    # Apps left out of a run of all, by folder name (axClaude: slow, and its replies need the network).
    [string[]]$Skip = @(),
    # Reuse the last build in %TEMP%\axit-nvda.
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$work = Join-Path $env:TEMP 'axit-nvda'
$exe = Join-Path $work 'axit\Axit.exe'
$runner = Join-Path $work 'runner\AxAutomate.exe'

$capture = Join-Path $env:APPDATA 'nvda\addons\axitCapture\globalPlugins\axitCapture.py'
if (-not (Test-Path $capture)) {
    Write-Host 'The NVDA capture add-on is not installed: copy tools\nvda-capture to %APPDATA%\nvda\addons\axitCapture and restart NVDA.'
    exit 2
}
if (-not (Get-Process nvda -ErrorAction SilentlyContinue)) {
    Write-Host 'NVDA is not running.'
    exit 2
}

if (-not $NoBuild) {
    Write-Host 'Building Axit and the runner into the scratch folder...'
    dotnet build (Join-Path $root 'src\Axit\Axit.csproj') -c Release -o (Join-Path $work 'axit') --nologo -v q | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Host 'Axit did not build.'; exit $LASTEXITCODE }
    dotnet build (Join-Path $root 'tools\AxAutomate\AxAutomate.csproj') -c Release -o (Join-Path $work 'runner') --nologo -v q | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Host 'The runner did not build.'; exit $LASTEXITCODE }
}

# The scripts of the pass in one folder, each app's in its own subfolder, in the runner's order.
$scripts = Join-Path $work 'scripts'
if (Test-Path $scripts) { Remove-Item $scripts -Recurse -Force }
New-Item -ItemType Directory -Force $scripts | Out-Null
$skipped = @($Skip | ForEach-Object { $_.ToLowerInvariant() })
$apps = @(Get-ChildItem (Join-Path $root 'tests\scripts') -Directory |
    Where-Object { ($App -eq '' -or $_.Name -eq $App.ToLowerInvariant()) -and $_.Name -notin $skipped })
if ($App -ne '' -and $apps.Count -eq 0) {
    Write-Host "No scripts in tests\scripts\$($App.ToLowerInvariant())."
    exit 1
}
foreach ($folder in $apps) {
    Copy-Item $folder.FullName (Join-Path $scripts $folder.Name) -Recurse
}
$nvdaOnly = Join-Path $scripts 'zz-nvda'
New-Item -ItemType Directory -Force $nvdaOnly | Out-Null
Get-ChildItem (Join-Path $root 'tests\nvda-scripts') -Filter *.txt |
    Where-Object { $App -eq '' -or $_.Name.StartsWith($App.ToLowerInvariant()) } |
    Copy-Item -Destination $nvdaOnly

$report = Join-Path $work 'report.md'
& $runner $scripts --exe $exe --nvda --report $report 2>&1 |
    ForEach-Object { "$_" } |
    Where-Object { $_ -match '^(==|FAIL|PASS|FAILED|NVDA tier)' -or $_ -match '^\s*(FAIL|failed)' } |
    ForEach-Object { $_ -replace '[A-Za-z]:(\\\\|\\)Users(\\\\|\\)[^\\"\s]+', '%USERPROFILE%' }
$code = $LASTEXITCODE
Write-Host 'Report: %TEMP%\axit-nvda\report.md'
exit $code
