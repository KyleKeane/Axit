<#
.SYNOPSIS
  Runs every test project of Axit in the Release configuration, the way GitHub Actions runs them.

.DESCRIPTION
  The test projects are found under tests\ (every *.Tests.csproj), so a new app's tests are included without an edit
  here. They run one by one, never through the solution: the solution would build src\Axit into the bin\Debug that a
  running Axit holds open. Release is what the workflows build, and only Release treats warnings as errors
  (Directory.Build.props), so a test that passes in Debug can still stop a release; 3.3.0 was lost that way.
  release.ps1 and run.ps1 -Test call this script. Exit code 0 when every project passed, otherwise the first failure's.

.EXAMPLE
  .\test.ps1                 # every test project
  .\test.ps1 axFile          # one project: tests\AxFile.Tests
#>
[CmdletBinding()]
param(
    # One app's tests alone, by the folder name without ".Tests" (axFile, Axit, axClaude, ...).
    [Parameter(Position = 0)]
    [string]$Only = ''
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

$projects = @(Get-ChildItem -Path (Join-Path $root 'tests') -Filter '*.Tests.csproj' -Recurse -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
    Sort-Object Name)
if ($Only -ne '') {
    $projects = @($projects | Where-Object { $_.BaseName -eq "$Only.Tests" })
    if ($projects.Count -eq 0) {
        Write-Host "No test project tests\$Only.Tests."
        exit 1
    }
}

foreach ($project in $projects) {
    Write-Host "Testing $($project.BaseName)..."
    dotnet test $project.FullName --configuration Release --nologo -v q
    if ($LASTEXITCODE -ne 0) {
        Write-Host "$($project.BaseName) failed."
        exit $LASTEXITCODE
    }
}

Write-Host "All $($projects.Count) test projects passed."
exit 0
