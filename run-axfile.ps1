<#
.SYNOPSIS
  Builds Axit and starts axFile: run.ps1 with -App file. Every other argument goes through unchanged. Without a path it
  opens the last folder axFile had.

.EXAMPLE
  .\run-axfile.ps1                          # the last folder
  .\run-axfile.ps1 C:\Music\Dune            # that folder (or a file, selected in its folder)
  .\run-axfile.ps1 -NoBuild                 # without building
#>
& "$PSScriptRoot\run.ps1" -App file @args
exit $LASTEXITCODE
