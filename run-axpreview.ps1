<#
.SYNOPSIS
  Builds Axit and starts AxPreview: run.ps1 with -App preview. Every other argument goes through unchanged. Without a
  path it opens an empty window.

.EXAMPLE
  .\run-axpreview.ps1 C:\notes\README.md    # the file, read-only
  .\run-axpreview.ps1                       # an empty window
  .\run-axpreview.ps1 -NoBuild              # without building
#>
& "$PSScriptRoot\run.ps1" -App preview @args
exit $LASTEXITCODE
