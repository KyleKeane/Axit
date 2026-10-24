<#
.SYNOPSIS
  Builds Axit and starts axList: run.ps1 with -App list. Every other argument goes through unchanged.

.EXAMPLE
  .\run-axlist.ps1                          # build and start axList
  .\run-axlist.ps1 -NoBuild                 # without building
#>
& "$PSScriptRoot\run.ps1" -App list @args
exit $LASTEXITCODE
