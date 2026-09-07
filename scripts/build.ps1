param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Release', [switch]$Test)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$env:DOTNET_CLI_HOME = Join-Path $projectRoot '.tools/cli-home'
$env:NUGET_PACKAGES = Join-Path $projectRoot '.tools/nuget-packages'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$localDotnet = Join-Path $projectRoot '.tools/dotnet/dotnet.exe'
$dotnetCommand = if (Test-Path $localDotnet) { $localDotnet } else { 'dotnet' }
Push-Location $projectRoot
try {
    & $dotnetCommand build TypeSimulator/TypeSimulator/TypeSimulator.Local.csproj -c $Configuration --nologo --configfile NuGet.Config
    if ($LASTEXITCODE -ne 0) { throw 'Application build failed.' }
    if ($Test) {
        & $dotnetCommand build tests/TypeSimulator.Tests.csproj -c $Configuration --nologo --configfile NuGet.Config
        if ($LASTEXITCODE -ne 0) { throw 'Test build failed.' }
        & "tests/bin/$Configuration/net48/TypeSimulator.Tests.exe"
        if ($LASTEXITCODE -ne 0) { throw 'Regression tests failed.' }
    }
} finally { Pop-Location }
