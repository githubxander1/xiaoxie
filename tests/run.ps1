param([switch]$Network)
$ErrorActionPreference='Stop'
$ProjectRoot=Split-Path $PSScriptRoot -Parent
$CheckRoot=Join-Path $ProjectRoot 'build\checks'
New-Item -ItemType Directory -Force -Path $CheckRoot | Out-Null
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'build\RestBuddy.exe') -Destination (Join-Path $CheckRoot 'RestBuddy.exe') -Force
$Compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $Compiler /nologo /target:exe /utf8output /codepage:65001 /reference:"$CheckRoot\RestBuddy.exe" /reference:System.Core.dll /reference:System.Web.Extensions.dll /out:"$CheckRoot\RestBuddy.Checks.exe" "$PSScriptRoot\EngineChecks.cs"
if($LASTEXITCODE -ne 0){throw 'Check compilation failed.'}
$CacheRoot=Join-Path $CheckRoot ('run-'+[Guid]::NewGuid().ToString('N'))
if($Network){& "$CheckRoot\RestBuddy.Checks.exe" $CacheRoot --network}else{& "$CheckRoot\RestBuddy.Checks.exe" $CacheRoot}
$CheckExit=$LASTEXITCODE
Write-Output "Report: $CacheRoot\report.json"
if($CheckExit -ne 0){throw 'Checks failed.'}
