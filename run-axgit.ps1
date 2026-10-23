<#
.SYNOPSIS
  Builds Axit and starts axGIT: run.ps1 with -App git. Every other argument goes through unchanged. Without a path it
  opens the last repository axGIT had.

.EXAMPLE
  .\run-axgit.ps1                           # the last repository
  .\run-axgit.ps1 C:\src\myproject          # the repository that holds that folder (or file)
  .\run-axgit.ps1 -NoBuild                  # without building
#>
& "$PSScriptRoot\run.ps1" -App git @args
exit $LASTEXITCODE
