[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$HdtInstallDir
)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$env:DOTNET_CLI_HOME = Join-Path $root '.dotnet-home'
$env:NUGET_PACKAGES = Join-Path $root '.nuget\packages'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
New-Item -ItemType Directory -Force -Path $env:DOTNET_CLI_HOME, $env:NUGET_PACKAGES | Out-Null
if ([string]::IsNullOrWhiteSpace($HdtInstallDir)) {
    $HdtInstallDir = $env:HDT_INSTALL_DIR
}
if ([string]::IsNullOrWhiteSpace($HdtInstallDir)) {
    $localRoot = Join-Path $env:LOCALAPPDATA 'HearthstoneDeckTracker'
    $latest = Get-ChildItem -LiteralPath $localRoot -Directory -Filter 'app-*' -ErrorAction SilentlyContinue |
        Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'HearthstoneDeckTracker.exe') } |
        Sort-Object { [version]($_.Name.Substring(4)) } -Descending |
        Select-Object -First 1
    if ($null -eq $latest) {
        throw 'No local HDT installation was found. Pass -HdtInstallDir or run scripts/resolve-hdt.ps1.'
    }
    $HdtInstallDir = $latest.FullName
}

$hdtPath = [IO.Path]::GetFullPath($HdtInstallDir)
& dotnet restore (Join-Path $root 'DustFinder.sln') --configfile (Join-Path $root 'NuGet.Config') -p:HdtInstallDir="$hdtPath"
if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }
& dotnet build (Join-Path $root 'DustFinder.sln') --no-restore -c $Configuration -p:Platform=x64 -p:HdtInstallDir="$hdtPath"
if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }
& dotnet test (Join-Path $root 'tests\DustFinder.Core.Tests\DustFinder.Core.Tests.csproj') --no-restore -c $Configuration
if ($LASTEXITCODE -ne 0) { throw 'dotnet test failed.' }
Write-Output "Build and tests passed against $hdtPath"
