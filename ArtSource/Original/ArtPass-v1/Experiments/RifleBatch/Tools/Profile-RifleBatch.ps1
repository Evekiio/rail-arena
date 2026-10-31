param([Parameter(Mandatory=$true)][string]$BuildDirectory,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$railPlayer=Join-Path (Resolve-Path -LiteralPath $BuildDirectory).Path 'RailArena.exe'
$railFolder=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $railFolder){throw 'Preserve prior profile evidence; choose a new output folder'}
[IO.Directory]::CreateDirectory($railFolder)|Out-Null
$railExecutableHash=(Get-FileHash -LiteralPath $railPlayer -Algorithm SHA256).Hash.ToLowerInvariant()
foreach($railMode in @('off','on')){
    $railOutput=Join-Path $railFolder ($railMode+'.json')
    $railArguments=@('--profile-art-frame','--validation-output',('"'+$railOutput+'"'),'-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','-noaudio','-logFile',('"'+$railOutput+'.log"'))
    if($railMode -eq 'off'){$railArguments+='--profile-rifle-batch-off'}
    if((Get-FileHash -LiteralPath $railPlayer -Algorithm SHA256).Hash.ToLowerInvariant() -ne $railExecutableHash){throw 'Player changed between controlled profiles'}
    $railStarted=[DateTime]::UtcNow.ToString('o')
    $railProcess=Start-Process -FilePath $railPlayer -ArgumentList $railArguments -WindowStyle Hidden -PassThru
    $railProcess.Id|Set-Content -LiteralPath (Join-Path $railFolder ($railMode+'.pid'))
    try{
        if(-not $railProcess.WaitForExit(180000)){throw "$railMode profile timed out"}
        $railExitCode=$railProcess.ExitCode
        @{mode=$railMode;executable=$railPlayer;executableSha256=$railExecutableHash;arguments=$railArguments;pid=$railProcess.Id;startedUtc=$railStarted;completedUtc=[DateTime]::UtcNow.ToString('o');exitCode=$railExitCode}|ConvertTo-Json -Depth 4|Set-Content -LiteralPath ($railOutput+'.run.json')
        if($railExitCode -ne 0){throw "$railMode profile exited $railExitCode"}
        $railReport=Get-Content -Raw -LiteralPath $railOutput|ConvertFrom-Json
        if(-not $railReport.passed -or $railReport.exceptions -ne 0){throw "$railMode profile reported errors"}
        Write-Output "RIFLE_BATCH_PROFILE_$railMode passed"
    }finally{if(-not $railProcess.HasExited){Stop-Process -Id $railProcess.Id}}
}
