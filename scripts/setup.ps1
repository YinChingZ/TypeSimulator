param([string]$SdkVersion = '8.0.424')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$toolsPath = Join-Path $projectRoot '.tools'
New-Item -ItemType Directory -Force $toolsPath | Out-Null
$installer = Join-Path $toolsPath 'dotnet-install.ps1'
Invoke-WebRequest 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installer -UseBasicParsing
& $installer -Version $SdkVersion -InstallDir (Join-Path $toolsPath 'dotnet') -NoPath
if ($LASTEXITCODE -ne 0) { throw 'SDK installation failed.' }
& (Join-Path $PSScriptRoot 'build.ps1') -Test
