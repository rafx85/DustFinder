[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$HdtInstallDir,
    [string]$PluginDirectory
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($PluginDirectory)) {
    $PluginDirectory = Join-Path $PSScriptRoot '..\artifacts\release\DustFinder'
}
$hdt = [IO.Path]::GetFullPath($HdtInstallDir)
$plugin = [IO.Path]::GetFullPath($PluginDirectory)
$resolver = [ResolveEventHandler]{
    param($sender, $eventArgs)
    $name = ([Reflection.AssemblyName]$eventArgs.Name).Name + '.dll'
    foreach ($directory in @($plugin, $hdt)) {
        $candidate = Join-Path $directory $name
        if (Test-Path -LiteralPath $candidate) { return [Reflection.Assembly]::LoadFrom($candidate) }
    }
    return $null
}
[AppDomain]::CurrentDomain.add_AssemblyResolve($resolver)
try {
    [void][Reflection.Assembly]::LoadFrom((Join-Path $hdt 'HearthstoneDeckTracker.exe'))
    [void][Reflection.Assembly]::LoadFrom((Join-Path $plugin 'DustFinder.Core.dll'))
    $assembly = [Reflection.Assembly]::LoadFrom((Join-Path $plugin 'DustFinder.Plugin.dll'))
    $type = $assembly.GetTypes() | Where-Object { $_.GetInterfaces().FullName -contains 'Hearthstone_Deck_Tracker.Plugins.IPlugin' } | Select-Object -First 1
    if ($null -eq $type) { throw 'No public HDT IPlugin implementation was found.' }
    $instance = [Activator]::CreateInstance($type)
    if ($instance.Name -ne 'DustFinder') { throw 'The plugin metadata could not be read.' }
    $viewModelType = $assembly.GetType('DustFinder.Plugin.ViewModels.MainViewModel', $true)
    $windowType = $assembly.GetType('DustFinder.Plugin.Views.MainWindow', $true)
    $viewModel = [Runtime.Serialization.FormatterServices]::GetUninitializedObject($viewModelType)
    $window = [Activator]::CreateInstance($windowType, @($viewModel))
    $window.Close()
    Write-Output "Verified HDT plugin type $($type.FullName), version $($instance.Version), and WPF window construction."
}
finally {
    [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolver)
}
