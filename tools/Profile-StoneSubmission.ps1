param([Parameter(Mandatory=$true)][string]$BuildDirectory,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$railPlayer=Join-Path (Resolve-Path -LiteralPath $BuildDirectory).Path 'RailArena.exe'
$railFolder=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $railFolder){throw 'Preserve prior profile evidence; choose a new output folder'}
[IO.Directory]::CreateDirectory($railFolder)|Out-Null
$railHash=(Get-FileHash -LiteralPath $railPlayer -Algorithm SHA256).Hash.ToLowerInvariant()
foreach($railMode in @('off','on')){
    $railOutput=Join-Path $railFolder ($railMode+'.json')
    $railArguments=@('--profile-art-frame','--validation-output',('"'+$railOutput+'"'),'-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','-noaudio','-logFile',('"'+$railOutput+'.log"'))
    if($railMode -eq 'on'){$railArguments+='--profile-stone-palette'}
    if((Get-FileHash -LiteralPath $railPlayer -Algorithm SHA256).Hash.ToLowerInvariant() -ne $railHash){throw 'Player changed between controlled profiles'}
    $railStarted=[DateTime]::UtcNow.ToString('o')
    $railJob=Start-Process -FilePath $railPlayer -ArgumentList $railArguments -WindowStyle Hidden -PassThru
    $railJob.Id|Set-Content -LiteralPath (Join-Path $railFolder ($railMode+'.pid'))
    try{
        if(-not $railJob.WaitForExit(180000)){throw "$railMode stone profile timed out"}
        @{mode=$railMode;executable=$railPlayer;executableSha256=$railHash;arguments=$railArguments;pid=$railJob.Id;startedUtc=$railStarted;completedUtc=[DateTime]::UtcNow.ToString('o');exitCode=$railJob.ExitCode}|ConvertTo-Json -Depth 4|Set-Content -LiteralPath ($railOutput+'.run.json')
        if($railJob.ExitCode -ne 0){throw "$railMode stone profile exited $($railJob.ExitCode)"}
        $railResult=Get-Content -Raw -LiteralPath $railOutput|ConvertFrom-Json
        if(-not $railResult.passed -or $railResult.exceptions -ne 0){throw "$railMode stone profile reported errors"}
        if($railMode -eq 'off' -and $railResult.stonePaletteRenderers -ne 0){throw 'Off workload unexpectedly used the candidate'}
        if($railMode -eq 'on' -and $railResult.stonePaletteRenderers -lt 20){throw 'On workload did not apply the candidate'}
        Write-Output "STONE_SUBMISSION_PROFILE_$railMode passed"
    }finally{if(-not $railJob.HasExited){$railJob.Kill()}}
}
