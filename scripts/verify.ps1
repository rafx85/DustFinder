[CmdletBinding()]
param(
    [string]$HdtInstallDir
)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$version = & (Join-Path $PSScriptRoot 'get-version.ps1')

if ([string]::IsNullOrWhiteSpace($HdtInstallDir)) {
    $HdtInstallDir = $env:HDT_INSTALL_DIR
}
if ([string]::IsNullOrWhiteSpace($HdtInstallDir)) {
    $localRoot = Join-Path $env:LOCALAPPDATA 'HearthstoneDeckTracker'
    $latest = Get-ChildItem -LiteralPath $localRoot -Directory -Filter 'app-*' -ErrorAction SilentlyContinue |
        Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'HearthstoneDeckTracker.exe') } |
        Sort-Object { [version]($_.Name.Substring(4)) } -Descending |
        Select-Object -First 1
    if ($null -ne $latest) {
        $HdtInstallDir = $latest.FullName
    }
}
if ([string]::IsNullOrWhiteSpace($HdtInstallDir)) {
    $resolvedOutput = @(& (Join-Path $PSScriptRoot 'resolve-hdt.ps1') -Version 1.53.8)
    $HdtInstallDir = $resolvedOutput | Select-Object -Last 1
}

$hdtPath = [IO.Path]::GetFullPath($HdtInstallDir)
if (-not (Test-Path -LiteralPath (Join-Path $hdtPath 'HearthstoneDeckTracker.exe'))) {
    throw "HearthstoneDeckTracker.exe was not found under $hdtPath."
}
if (-not (Test-Path -LiteralPath (Join-Path $hdtPath 'HearthDb.dll'))) {
    throw "HearthDb.dll was not found under $hdtPath."
}

& (Join-Path $PSScriptRoot 'build.ps1') -Configuration Release -HdtInstallDir $hdtPath
& (Join-Path $PSScriptRoot 'package.ps1') -HdtInstallDir $hdtPath -NoBuild
& powershell.exe -NoProfile -Sta -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'verify-plugin.ps1') -HdtInstallDir $hdtPath
if ($LASTEXITCODE -ne 0) {
    throw 'Plugin contract verification failed.'
}

$pluginAssemblyPath = Join-Path $root 'src\DustFinder.Plugin\bin\x64\Release\net472\DustFinder.Plugin.dll'
$actualVersion = [Reflection.AssemblyName]::GetAssemblyName($pluginAssemblyPath).Version
$requestedVersion = [version]$version
$expectedVersion = [version]::new(
    $requestedVersion.Major,
    $requestedVersion.Minor,
    [Math]::Max($requestedVersion.Build, 0),
    [Math]::Max($requestedVersion.Revision, 0))
if ($actualVersion -ne $expectedVersion) {
    throw "Built plugin version $actualVersion does not match project version $expectedVersion."
}

$zipPath = Join-Path $root "dist\DustFinder-$version.zip"
if (-not (Test-Path -LiteralPath $zipPath)) {
    throw "Expected release package was not created at $zipPath."
}

Write-Output "Verified DustFinder $version against $hdtPath"
Write-Output $zipPath
