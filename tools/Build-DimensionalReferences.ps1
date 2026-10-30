param([string]$Blender='C:\Program Files\Blender Foundation\Blender 5.2\blender.exe',[switch]$Resume)
$ErrorActionPreference='Stop'
$dimensionRoot=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$dimensionProcess=$null
$dimensionArguments=@('--background','--threads','4','--python-exit-code','1','--python',
    ('"'+(Join-Path $PSScriptRoot 'render_dimensional_references.py')+'"'),'--',('"'+$dimensionRoot+'"'))
if ($Resume) { $dimensionArguments+='--resume' }
try {
    $dimensionProcess=Start-Process -FilePath $Blender -ArgumentList $dimensionArguments -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput (Join-Path $dimensionRoot 'Validation/dimensional-references-final.log') `
        -RedirectStandardError (Join-Path $dimensionRoot 'Validation/dimensional-references-final-error.log')
    Write-Output "Owned Blender PID: $($dimensionProcess.Id)"
    if (-not $dimensionProcess.WaitForExit(600000)) { throw 'Dimensional reference generation exceeded ten minutes.' }
    $dimensionProcess.Refresh()
    if ($dimensionProcess.ExitCode -ne 0) { throw "Blender exit $($dimensionProcess.ExitCode)" }
    if (-not (Select-String -LiteralPath (Join-Path $dimensionRoot 'Validation/dimensional-references-final.log') -SimpleMatch 'DIMENSIONAL_REFERENCES_COMPLETE' -Quiet)) { throw 'Missing completion marker' }
} finally {
    if ($dimensionProcess) {
        $dimensionProcess.Refresh()
        if (-not $dimensionProcess.HasExited) { Stop-Process -Id $dimensionProcess.Id -Force; $dimensionProcess.WaitForExit() }
        $dimensionProcess.Dispose()
    }
}
