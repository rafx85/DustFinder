[CmdletBinding()]
param(
    [string]$Version = '0.1.0',
    [string]$HdtInstallDir,
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not $NoBuild) {
    & (Join-Path $PSScriptRoot 'build.ps1') -Configuration Release -HdtInstallDir $HdtInstallDir
    if ($LASTEXITCODE -ne 0) { throw 'Build failed before packaging.' }
}

$stageRoot = Join-Path $root 'artifacts\release'
$pluginRoot = Join-Path $stageRoot 'DustFinder'
$dist = Join-Path $root 'dist'
$workspacePrefix = $root.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not ([IO.Path]::GetFullPath($stageRoot).StartsWith($workspacePrefix, [StringComparison]::OrdinalIgnoreCase))) {
    throw 'Refusing to clean a staging directory outside the workspace.'
}
if (Test-Path -LiteralPath $stageRoot) { Remove-Item -LiteralPath $stageRoot -Recurse -Force }
New-Item -ItemType Directory -Force -Path $pluginRoot, $dist | Out-Null
$pluginOutput = Join-Path $root 'src\DustFinder.Plugin\bin\x64\Release\net472'
Copy-Item -LiteralPath (Join-Path $pluginOutput 'DustFinder.Plugin.dll') -Destination $pluginRoot
Copy-Item -LiteralPath (Join-Path $pluginOutput 'DustFinder.Core.dll') -Destination $pluginRoot

$zip = Join-Path $dist "DustFinder-$Version.zip"
if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
Compress-Archive -Path $pluginRoot -DestinationPath $zip -CompressionLevel Optimal
Write-Output $zip
