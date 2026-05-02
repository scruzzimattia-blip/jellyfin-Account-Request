$ErrorActionPreference = 'Stop'

$RootDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = Join-Path $RootDir 'Jellyfin.Plugin.AccountRequest'
$ProjectFile = Join-Path $ProjectDir 'Jellyfin.Plugin.AccountRequest.csproj'
$OutputDir = Join-Path $ProjectDir 'bin/Release/net9.0'
$DistDir = Join-Path $RootDir 'dist'

dotnet build $ProjectFile --configuration Release

if (Test-Path $DistDir) {
    Remove-Item $DistDir -Recurse -Force
}

New-Item -ItemType Directory -Path $DistDir | Out-Null

Copy-Item (Join-Path $OutputDir 'Jellyfin.Plugin.AccountRequest.dll') $DistDir
Copy-Item (Join-Path $OutputDir 'Jellyfin.Plugin.AccountRequest.deps.json') $DistDir

$MetaFile = Join-Path $RootDir 'meta.json'
if (Test-Path $MetaFile) {
    Copy-Item $MetaFile $DistDir
}

Write-Host "Release artifacts copied to $DistDir"
