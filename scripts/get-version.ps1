[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$projectPath = Join-Path $root 'src\DustFinder.Plugin\DustFinder.Plugin.csproj'
[xml]$project = Get-Content -LiteralPath $projectPath -Raw
$version = $project.Project.PropertyGroup |
    ForEach-Object { $_.Version } |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
    Select-Object -First 1

if ([string]::IsNullOrWhiteSpace($version)) {
    throw 'DustFinder.Plugin.csproj does not define a Version.'
}
$version = $version.Trim()
if ($version -notmatch '^\d+\.\d+\.\d+(\.\d+)?$') {
    throw "DustFinder.Plugin.csproj defines unsupported version '$version'."
}

Write-Output $version
