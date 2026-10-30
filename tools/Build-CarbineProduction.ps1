param(
    [switch]$SkipModeling,[switch]$SkipWindowsBuild,
    [string]$Blender='C:\Program Files\Blender Foundation\Blender 5.2\blender.exe',
    [string]$Unity='D:\Unity Editor\6000.6.7f1\Editor\Unity.exe',
    [string]$Python=(Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe')
)
$ErrorActionPreference='Stop'
$carbineRoot=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
function Invoke-CarbineBlender([string]$Script,[int]$Timeout) {
    $owned=$null
    try {
        $owned=Start-Process $Blender -ArgumentList @('--background','--threads','4','--python-exit-code','1','--python',('"'+(Join-Path $PSScriptRoot $Script)+'"'),'--',('"'+$carbineRoot+'"')) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $carbineRoot ('Validation/'+$Script+'.log')) -RedirectStandardError (Join-Path $carbineRoot ('Validation/'+$Script+'-error.log'))
        Write-Output "Owned $Script PID: $($owned.Id)"
        if(-not $owned.WaitForExit($Timeout*1000)){throw "$Script timed out"}
        $owned.Refresh();if($owned.ExitCode -ne 0){throw "$Script failed"}
    }finally{if($owned){$owned.Refresh();if(-not $owned.HasExited){Stop-Process -Id $owned.Id -Force;$owned.WaitForExit()};$owned.Dispose()}}
}
function Assert-CarbineStep {if($LASTEXITCODE -ne 0){throw 'Carbine production step failed'}}
Push-Location $carbineRoot
try {
    if(-not $SkipModeling){
        Invoke-CarbineBlender 'build_carbine_production.py' 240
        & $Python tools/texture_carbine_production.py;Assert-CarbineStep
        Invoke-CarbineBlender 'audit_carbine_production.py' 300
    }
    Invoke-CarbineBlender 'audit_carbine_exports.py' 90
    & unity run (Join-Path $carbineRoot 'RailArena') --editor-path $Unity --timeout 300 --log-file (Join-Path $carbineRoot 'Validation/carbine-production-unity.log') --no-tail --format json -- -job-worker-count 4 -executeMethod RailArena.Editor.CarbineProduction.Build;Assert-CarbineStep
    Invoke-CarbineBlender 'finalize_carbine_anchors.py' 90
    & unity test (Join-Path $carbineRoot 'RailArena') --mode EditMode --output (Join-Path $carbineRoot 'Validation/carbine-regressions.xml') --editor-path $Unity --timeout 300 --format json;Assert-CarbineStep
    if(-not $SkipWindowsBuild){
        & unity run (Join-Path $carbineRoot 'RailArena') --editor-path $Unity --timeout 600 --log-file (Join-Path $carbineRoot 'Validation/carbine-windows-build.log') --no-tail --format json -- -job-worker-count 4 -executeMethod RailArena.Editor.CarbineProduction.BuildValidationWindows;Assert-CarbineStep
    }
    & $Python tools/package_carbine_production.py;Assert-CarbineStep
}finally{Pop-Location}
