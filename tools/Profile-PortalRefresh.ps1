param([Parameter(Mandatory=$true)][string]$BuildDirectory,[Parameter(Mandatory=$true)][string]$OutputDirectory,[ValidateSet('OffThenOn','OnThenOff')][string]$Order='OffThenOn')
$ErrorActionPreference='Stop'
$railBuild=(Resolve-Path -LiteralPath $BuildDirectory).Path
$railPlayer=Join-Path $railBuild 'RailArena.exe'
$railOutput=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $railOutput){throw 'Preserve previous evidence; use a new profile folder'}
[IO.Directory]::CreateDirectory($railOutput)|Out-Null
$railFiles=@('RailArena.exe','UnityPlayer.dll','RailArena_Data/Managed/RailArena.Runtime.dll','RailArena_Data/Managed/RailArena.Simulation.dll','RailArena_Data/resources.assets','RailArena_Data/resources.assets.resS','RailArena_Data/sharedassets0.assets','RailArena_Data/sharedassets0.assets.resS','RailArena_Data/level0')
function Get-RailPayload {
    $railHashes=[ordered]@{}
    foreach($railFile in $railFiles){$railHashes[$railFile]=(Get-FileHash -LiteralPath (Join-Path $railBuild $railFile) -Algorithm SHA256).Hash.ToLowerInvariant()}
    $railHashes|ConvertTo-Json -Compress
}
$railPayload=Get-RailPayload
$railIdentity=(Get-Content -LiteralPath (Join-Path $railBuild 'BuildIdentity.txt') -Raw).Trim()
$railModes=if($Order -eq 'OnThenOff'){@('on','off')}else{@('off','on')}
foreach($railMode in $railModes){
    if((Get-RailPayload) -ne $railPayload){throw 'Candidate payload changed before profile'}
    $railReportPath=Join-Path $railOutput ($railMode+'.json')
    $railArguments=@('--profile-art-frame','--validation-output',('"'+$railReportPath+'"'),'-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','-noaudio','-logFile',('"'+$railReportPath+'.log"'))
    if($railMode -eq 'on'){$railArguments+='--profile-portal-refresh'}
    $railStarted=[DateTime]::UtcNow.ToString('o')
    $railJob=Start-Process -FilePath $railPlayer -ArgumentList $railArguments -WindowStyle Hidden -PassThru
    $railJob.Id|Set-Content -LiteralPath (Join-Path $railOutput ($railMode+'.pid'))
    try{
        if(-not $railJob.WaitForExit(180000)){throw 'Portal profile timed out'}
        if($railJob.ExitCode -ne 0){throw "Portal profile failed: $railMode"}
        if((Get-RailPayload) -ne $railPayload){throw 'Candidate payload changed during profile'}
        $railResult=Get-Content -LiteralPath $railReportPath -Raw|ConvertFrom-Json
        if(-not $railResult.passed -or $railResult.exceptions -ne 0 -or $railResult.fingerprint -ne $railIdentity){throw 'Profile identity or validity failed'}
        if($railResult.adaptivePortalRefresh -ne ($railMode -eq 'on')){throw 'Portal profile mode was not applied'}
        @{mode=$railMode;order=$Order;fingerprint=$railIdentity;payloadHashes=($railPayload|ConvertFrom-Json);arguments=$railArguments;pid=$railJob.Id;startedUtc=$railStarted;completedUtc=[DateTime]::UtcNow.ToString('o');exitCode=$railJob.ExitCode}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath ($railReportPath+'.run.json')
        Write-Output "PORTAL_REFRESH_PROFILE_$railMode passed"
    }finally{if(-not $railJob.HasExited){$railJob.Kill()}}
}
