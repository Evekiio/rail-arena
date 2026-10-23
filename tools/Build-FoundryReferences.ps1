param([string]$Blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe')
$ErrorActionPreference = 'Stop'
$referenceRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$referenceScript = Join-Path $PSScriptRoot 'render_foundry_references.py'
$ownedJob = $null
try {
    $ownedJob = Start-Process -FilePath $Blender -ArgumentList @('--background','--factory-startup',
      '--threads','4','--python-exit-code','1','--python',('"'+$referenceScript+'"'),'--',('"'+$referenceRoot+'"')) `
      -WorkingDirectory $referenceRoot -WindowStyle Hidden -PassThru `
      -RedirectStandardOutput (Join-Path $referenceRoot 'Validation/foundry-references.log') `
      -RedirectStandardError (Join-Path $referenceRoot 'Validation/foundry-references-error.log')
    Write-Output "Owned Blender PID: $($ownedJob.Id)"
    if (-not $ownedJob.WaitForExit(480000)) { throw 'Foundry reference render exceeded eight minutes.' }
    $ownedJob.Refresh()
    if ($ownedJob.ExitCode -ne 0) { throw "Blender failed: $($ownedJob.ExitCode)" }
    if (-not (Select-String -LiteralPath (Join-Path $referenceRoot 'Validation/foundry-references.log') -SimpleMatch 'FOUNDRY_REFERENCES_COMPLETE' -Quiet)) { throw 'No completion marker.' }
} finally {
    if ($null -ne $ownedJob) {
        $ownedJob.Refresh()
        if (-not $ownedJob.HasExited) { Stop-Process -Id $ownedJob.Id -Force; $ownedJob.WaitForExit() }
        $ownedJob.Dispose()
    }
}
