param([string]$Blender='C:\Program Files\Blender Foundation\Blender 5.2\blender.exe')
$ErrorActionPreference='Stop'
$rifleRoot=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$rifleProcess=$null
try {
    $rifleProcess=Start-Process -FilePath $Blender -ArgumentList @('--background','--threads','4',
        '--python-exit-code','1','--python',('"'+(Join-Path $PSScriptRoot 'build_rifle_reference_v2.py')+'"'),
        '--',('"'+$rifleRoot+'"')) -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput (Join-Path $rifleRoot 'Validation/rifle-reference-v2.log') `
        -RedirectStandardError (Join-Path $rifleRoot 'Validation/rifle-reference-v2-error.log')
    Write-Output "Owned reference-study Blender PID: $($rifleProcess.Id)"
    if (-not $rifleProcess.WaitForExit(720000)) { throw 'Rifle reference job exceeded twelve minutes.' }
    $rifleProcess.Refresh()
    if ($rifleProcess.ExitCode -ne 0) { throw "Reference-study Blender exit $($rifleProcess.ExitCode)" }
    if (-not (Select-String -LiteralPath (Join-Path $rifleRoot 'Validation/rifle-reference-v2.log') -SimpleMatch 'RIFLE_REFERENCE_V2_COMPLETE' -Quiet)) { throw 'No completion marker' }
} finally {
    if ($rifleProcess) {
        $rifleProcess.Refresh()
        if (-not $rifleProcess.HasExited) { Stop-Process -Id $rifleProcess.Id -Force; $rifleProcess.WaitForExit() }
        $rifleProcess.Dispose()
    }
}
