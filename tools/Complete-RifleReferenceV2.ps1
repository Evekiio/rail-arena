param(
    [switch]$SkipRendering,
    [string]$Blender='C:\Program Files\Blender Foundation\Blender 5.2\blender.exe',
    [string]$Python=(Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'),
    [string]$SharpModule=(Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp')
)
$ErrorActionPreference='Stop'
$riflePackageRoot=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$rifleFolder='ArtSource/Original/RailRifle-Modeling-References-v2'
$rifleMaster=$rifleFolder+'/Rifle-Reference-Study.blend'
function Invoke-RifleReferenceJob([string]$Script,[string]$Log,[string[]]$ExtraArguments=@()) {
    $ownedRifleJob=$null
    try {
        $jobArguments=@('--background','--threads','4','--python-exit-code','1','--python',
            ('"'+(Join-Path $PSScriptRoot $Script)+'"'),'--',('"'+$riflePackageRoot+'"'))+$ExtraArguments
        $ownedRifleJob=Start-Process -FilePath $Blender -ArgumentList $jobArguments -WindowStyle Hidden -PassThru `
            -RedirectStandardOutput (Join-Path $riflePackageRoot ('Validation/'+$Log+'.log')) `
            -RedirectStandardError (Join-Path $riflePackageRoot ('Validation/'+$Log+'-error.log'))
        Write-Output "Owned $Script PID: $($ownedRifleJob.Id)"
        if (-not $ownedRifleJob.WaitForExit(90000)) { throw "$Script exceeded ninety seconds" }
        $ownedRifleJob.Refresh()
        if ($ownedRifleJob.ExitCode -ne 0) { throw "$Script failed: $($ownedRifleJob.ExitCode)" }
    } finally {
        if ($ownedRifleJob) {
            $ownedRifleJob.Refresh()
            if (-not $ownedRifleJob.HasExited) { Stop-Process -Id $ownedRifleJob.Id -Force; $ownedRifleJob.WaitForExit() }
            $ownedRifleJob.Dispose()
        }
    }
}
Push-Location $riflePackageRoot
try {
    if (-not $SkipRendering) { & (Join-Path $PSScriptRoot 'Build-RifleReferenceV2.ps1') -Blender $Blender }
    & $Python (Join-Path $PSScriptRoot 'package_dimensional_references.py') --folder $rifleFolder --master $rifleMaster
    if ($LASTEXITCODE -ne 0) { throw 'Dimensional sheet packaging failed' }
    & $Python (Join-Path $PSScriptRoot 'package_rifle_reference_v2.py')
    if ($LASTEXITCODE -ne 0) { throw 'Rifle handoff packaging failed' }
    & node (Join-Path $PSScriptRoot 'rasterize_rifle_reference_v2.cjs') $SharpModule
    if ($LASTEXITCODE -ne 0) { throw 'Sheet rasterization failed' }
    Invoke-RifleReferenceJob 'build_dimensional_reference_rig.py' 'rifle-reference-v2-rig' @('--folder',$rifleFolder)
    & $Python (Join-Path $PSScriptRoot 'validate_dimensional_references.py') --folder $rifleFolder --master $rifleMaster
    if ($LASTEXITCODE -ne 0) { throw 'Image calibration validation failed' }
    Invoke-RifleReferenceJob 'validate_rifle_reference_v2.py' 'rifle-reference-v2-validation'
    & $Python (Join-Path $PSScriptRoot 'finalize_rifle_reference_v2.py')
    if ($LASTEXITCODE -ne 0) { throw 'Final reference coverage check failed' }
    Write-Output 'RIFLE_REFERENCE_V2_PACKAGE_COMPLETE'
} finally { Pop-Location }
