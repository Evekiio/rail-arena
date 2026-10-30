param(
    [ValidateSet('Model','Render','Audit','Import','Capture','Tests','Fit','Texture','RigFix','Details')][string]$Stage='Model',
    [string]$Blender='C:\Program Files\Blender Foundation\Blender 5.2\blender.exe',
    [string]$Unity='D:\Unity Editor\6000.6.7f1\Editor\Unity.exe'
)
$ErrorActionPreference='Stop'
$pilotRoot=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$logs=Join-Path $pilotRoot 'Validation/PlayerPilot-v1'
New-Item -ItemType Directory -Force -Path $logs | Out-Null
Push-Location $pilotRoot
try {
    if($Stage -in @('Model','Render','Audit','Fit','Texture','RigFix','Details')) {
        $owned=$null
        try {
            $owned=Start-Process -FilePath $Blender -WindowStyle Hidden -PassThru -ArgumentList @(
                '--background','--factory-startup','--threads','4','--python-exit-code','1',
                '--python',('"'+(Join-Path $PSScriptRoot 'build_player_pilot.py')+'"'),
                '--',('"'+$pilotRoot+'"'),'--stage',$Stage.ToLowerInvariant()
            ) -RedirectStandardOutput (Join-Path $logs ($Stage+'.log')) -RedirectStandardError (Join-Path $logs ($Stage+'-error.log'))
            Write-Output "Owned Blender PID $($owned.Id), stage $Stage, timeout 600 seconds"
            if(-not $owned.WaitForExit(600000)){throw "Player pilot $Stage timed out"}
            $owned.Refresh()
            if($owned.ExitCode -ne 0){throw "Player pilot $Stage failed; inspect $logs"}
        } finally {
            if($owned){$owned.Refresh();if(-not $owned.HasExited){Stop-Process -Id $owned.Id -Force;$owned.WaitForExit()};$owned.Dispose()}
        }
    } elseif($Stage -eq 'Tests') {
        & unity test (Join-Path $pilotRoot 'RailArena') --editor-path $Unity --mode EditMode --filter RailArena.Tests --output (Join-Path $logs 'EditMode.xml') --timeout 300 --format json
        if($LASTEXITCODE -ne 0){throw 'Player pilot tests failed'}
    } else {
        $method=if($Stage -eq 'Import'){'RailArena.Editor.PlayerPilotBuilder.Build'}else{'RailArena.Editor.PlayerPilotBuilder.Capture'}
        $extra=if($Stage -eq 'Import'){@('-nographics')}else{@()}
        & unity run (Join-Path $pilotRoot 'RailArena') --editor-path $Unity --timeout 480 --log-file (Join-Path $logs ($Stage+'.log')) --no-tail --format json -- -job-worker-count 4 @extra -executeMethod $method
        if($LASTEXITCODE -ne 0){throw "Player pilot Unity $Stage failed"}
    }
} finally {Pop-Location}
