param(
    [Parameter(Mandatory=$true)][string]$BuildDirectory,
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference='Stop'
$railBuild=(Resolve-Path -LiteralPath $BuildDirectory).Path
$railPlayer=Join-Path $railBuild 'RailArena.exe'
if(-not (Test-Path -LiteralPath $railPlayer)){throw 'Windows player missing'}
$railEvidence=[IO.Path]::GetFullPath($OutputDirectory)
[IO.Directory]::CreateDirectory($railEvidence)|Out-Null
foreach($railMode in @('mechanics','bots','performance')){
    $railReport=Join-Path $railEvidence ($railMode+'.json')
    if(Test-Path -LiteralPath $railReport){throw "Preserve previous evidence: $railReport"}
    $railFlag=switch($railMode){'mechanics'{'--validate-mechanics'} 'bots'{'--validate-offline --validation-seconds 45 --validation-bots 3'} default{'--validate-art-performance'}}
    $railArguments=$railFlag+' --validation-output "'+$railReport+'" -screen-width 1920 -screen-height 1080 -screen-fullscreen 0 -noaudio -logFile "'+$railReport+'.log"'
    $railJob=Start-Process -FilePath $railPlayer -ArgumentList $railArguments -WindowStyle Hidden -PassThru
    $railJob.Id|Set-Content -LiteralPath (Join-Path $railEvidence ($railMode+'.pid'))
    try{
        if(-not $railJob.WaitForExit(120000)){throw "$railMode timed out"}
        if($railJob.ExitCode -ne 0){throw "$railMode exited $($railJob.ExitCode)"}
        $railResult=Get-Content -Raw -LiteralPath $railReport|ConvertFrom-Json
        if(-not $railResult.passed){throw "$railMode reported failure"}
        Write-Output "$railMode passed"
    }finally{if(-not $railJob.HasExited){$railJob.Kill()}}
}
& (Join-Path $PSScriptRoot 'Test-Duel.ps1') -Build $railPlayer -OutputDirectory (Join-Path $railEvidence 'Duel') -MovementModes
