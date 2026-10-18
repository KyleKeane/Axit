<#
.SYNOPSIS
  Builds Axit and starts AxJSON: run.ps1 with -App json. Every other argument goes through unchanged. Without a file
  it opens an empty document.

.EXAMPLE
  .\run-axjson.ps1                          # an empty document
  .\run-axjson.ps1 C:\x\settings.json       # that file
  .\run-axjson.ps1 -NoBuild                 # without building
#>
& "$PSScriptRoot\run.ps1" -App json @args
exit $LASTEXITCODE
