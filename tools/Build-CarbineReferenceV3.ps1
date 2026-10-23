param(
    [switch]$SkipRendering,
    [string]$Blender='C:\Program Files\Blender Foundation\Blender 5.2\blender.exe',
    [string]$Unity='D:\Unity Editor\6000.6.7f1\Editor\Unity.exe',
    [string]$Python=(Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'),
    [string]$SharpModule=(Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp')
)
$ErrorActionPreference='Stop'
$carbineRoot=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$carbineFolder='ArtSource/Original/PrecisionCarbine-References-v3'
$carbineMaster=$carbineFolder+'/Carbine-Reference-Study.blend'
function Invoke-CarbineBlender([string]$Script,[int]$Timeout=90,[string[]]$Extra=@()) {
    $owned=$null
    try {
        $owned=Start-Process $Blender -ArgumentList (@('--background','--threads','4','--python-exit-code','1','--python',('"'+(Join-Path $PSScriptRoot $Script)+'"'),'--',('"'+$carbineRoot+'"'))+$Extra) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $carbineRoot ('Validation/'+$Script+'.log')) -RedirectStandardError (Join-Path $carbineRoot ('Validation/'+$Script+'-error.log'))
        Write-Output "Owned $Script PID: $($owned.Id)"
        if(-not $owned.WaitForExit($Timeout*1000)){throw "$Script exceeded $Timeout seconds"}
        $owned.Refresh();if($owned.ExitCode -ne 0){throw "$Script failed"}
    } finally {
        if($owned){$owned.Refresh();if(-not $owned.HasExited){Stop-Process -Id $owned.Id -Force;$owned.WaitForExit()};$owned.Dispose()}
    }
}
function Assert-CarbineExit { if($LASTEXITCODE -ne 0){throw 'Carbine package step failed'} }
Push-Location $carbineRoot
try {
    if(-not $SkipRendering){Invoke-CarbineBlender 'build_carbine_reference_v3.py' 720}
    & $Python tools/prepare_carbine_reference_v3.py;Assert-CarbineExit
    Invoke-CarbineBlender 'export_carbine_review_v3.py'
    & $Python tools/package_dimensional_references.py --folder $carbineFolder --master $carbineMaster;Assert-CarbineExit
    & $Python tools/package_rifle_reference_v2.py --folder $carbineFolder;Assert-CarbineExit
    & node tools/rasterize_rifle_reference_v2.cjs $SharpModule $carbineFolder;Assert-CarbineExit
    Invoke-CarbineBlender 'build_dimensional_reference_rig.py' 90 @('--folder',$carbineFolder)
    & unity run (Join-Path $carbineRoot 'RailArena') --editor-path $Unity --timeout 300 --log-file (Join-Path $carbineRoot 'Validation/carbine-reference-v3-unity.log') --no-tail --format json -- -job-worker-count 4 -executeMethod RailArena.Editor.CarbineReferenceReview.Capture;Assert-CarbineExit
    & $Python tools/validate_dimensional_references.py --folder $carbineFolder --master $carbineMaster;Assert-CarbineExit
    Invoke-CarbineBlender 'validate_carbine_reference_v3.py'
    & $Python tools/finalize_carbine_reference_v3.py;Assert-CarbineExit
} finally { Pop-Location }
