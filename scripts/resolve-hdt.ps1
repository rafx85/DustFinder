[CmdletBinding()]
param(
    [string]$Version = '1.53.8',
    [string]$Destination
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($Destination)) {
    $Destination = Join-Path $PSScriptRoot '..\.hdt'
}
$destinationPath = [IO.Path]::GetFullPath($Destination)
$resolved = Join-Path $destinationPath 'lib\net472'
if (Test-Path -LiteralPath (Join-Path $resolved 'HearthstoneDeckTracker.exe')) {
    Write-Output $resolved
    exit 0
}

New-Item -ItemType Directory -Force -Path $destinationPath | Out-Null
$packageName = "HearthstoneDeckTracker-$Version.nupkg"
$packagePath = Join-Path $destinationPath $packageName
if (-not (Test-Path -LiteralPath $packagePath)) {
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
        throw 'GitHub CLI (gh) is required to download the pinned official HDT release package.'
    }
    & gh release download "v$Version" --repo HearthSim/HDT-Releases --pattern $packageName --dir $destinationPath
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to download official HDT release v$Version."
    }
}

$zipPath = Join-Path $destinationPath "$packageName.zip"
Copy-Item -LiteralPath $packagePath -Destination $zipPath -Force
Expand-Archive -LiteralPath $zipPath -DestinationPath $destinationPath -Force
Remove-Item -LiteralPath $zipPath -Force
if (-not (Test-Path -LiteralPath (Join-Path $resolved 'HearthstoneDeckTracker.exe'))) {
    throw "The HDT package did not contain lib\net472\HearthstoneDeckTracker.exe."
}
Write-Output $resolved
