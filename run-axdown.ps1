<#
.SYNOPSIS
  Builds Axit and starts AxDown: run.ps1 with -App down. Every other argument goes through unchanged.

.EXAMPLE
  .\run-axdown.ps1 C:\notes\todo.md   # that file
  .\run-axdown.ps1                    # an empty document
  .\run-axdown.ps1 -NoBuild           # skip the build
#>
& "$PSScriptRoot\run.ps1" -App down @args
exit $LASTEXITCODE
