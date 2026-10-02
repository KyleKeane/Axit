<#
.SYNOPSIS
  Builds AxClaude and starts it on a project folder.

.EXAMPLE
  .\run.ps1                       # current folder is the project
  .\run.ps1 C:\src\myproject      # that folder is the project
  .\run.ps1 C:\src\myproject -- --resume     # extra arguments go to claude (default: --continue)
  .\run.ps1 C:\src\myproject -New            # a new conversation (PowerShell swallows a bare --)
  .\run.ps1 -NoBuild              # skip the build, just start
  .\run.ps1 -Test                 # run the unit tests instead

  If PowerShell refuses to run scripts: powershell -ExecutionPolicy Bypass -File .\run.ps1
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Project = (Get-Location).Path,

    [switch]$NoBuild,
    [switch]$Test,
    [switch]$New,

    # Start AxDown on this file instead of AxClaude on the project folder.
    [string]$Down,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ClaudeArgs
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

if ($Test) {
    dotnet test "$root\Axit.sln" -nologo
    exit $LASTEXITCODE
}

if (-not $NoBuild) {
    Write-Host "Building Axit..."
    dotnet build "$root\src\Axit\Axit.csproj" -c Debug -nologo -v q
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed."
        exit $LASTEXITCODE
    }
}

$exe = Join-Path $root 'src\Axit\bin\Debug\net10.0-windows\Axit.exe'
if (-not (Test-Path $exe)) {
    Write-Host "Axit.exe was not found at $exe. Run without -NoBuild first."
    exit 1
}

# Axit.exe holds every app of the bundle; the verb picks the app (docs/axit/SPEC.md AX-1).
if ($Down) {
    Write-Host "Starting AxDown on $Down"
    & $exe down $Down
    exit $LASTEXITCODE
}

$Project = (Resolve-Path $Project).Path
Write-Host "Starting AxClaude on $Project"
if ($New) {
    # A bare -- never reaches the app: PowerShell takes it as the end of the script's own parameters.
    & $exe claude $Project '--'
} elseif ($ClaudeArgs -and $ClaudeArgs.Count -gt 0) {
    & $exe claude $Project '--' @ClaudeArgs
} else {
    & $exe claude $Project
}
