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
    $sourceType = $assembly.GetType('DustFinder.Plugin.Integration.HdtCollectionSource', $true)
    $source = [Activator]::CreateInstance($sourceType)
    $viewModel = [Activator]::CreateInstance($viewModelType, @($source, $plugin))
    $window = [Activator]::CreateInstance($windowType, @($viewModel))
    $versionText = $window.FindName('PluginVersionText')
    $expectedVersionText = "v$($instance.Version.Major).$($instance.Version.Minor).$($instance.Version.Build)"
    if ($null -eq $versionText) {
        throw 'The visible plugin version label was not found.'
    }
    if ($viewModel.PluginVersion -ne $expectedVersionText -or $versionText.Text -ne $expectedVersionText) {
        throw "The visible plugin version does not match the installed assembly version: expected $expectedVersionText, view model $($viewModel.PluginVersion), label $($versionText.Text)."
    }
    $collectionGrid = $window.FindName('CollectionGrid')
    if ($null -eq $collectionGrid -or
		$collectionGrid.SelectionMode.ToString() -ne 'Extended' -or
		$collectionGrid.FrozenColumnCount -ne 1) {
        throw "The collection grid is not configured for responsive multi-selection: mode $($collectionGrid.SelectionMode), frozen columns $($collectionGrid.FrozenColumnCount)."
    }
	$applyResponsiveLayout = $windowType.GetMethod('ApplyResponsiveLayout', [Reflection.BindingFlags]'Instance,NonPublic')
	if ($null -eq $applyResponsiveLayout) {
		throw 'The responsive table layout was not found.'
	}
	$window.Width = 1200
	[void]$applyResponsiveLayout.Invoke($window, @())
	$collectionTypeColumn = $collectionGrid.Columns | Where-Object { $_.Header -eq 'Type' } | Select-Object -First 1
	if ($window.FindName('CollectionCardPreview').Visibility.ToString() -ne 'Visible' -or
		$window.FindName('CollectionCardPreview').Width -ne 225 -or
		$collectionTypeColumn.Visibility.ToString() -ne 'Collapsed') {
		throw 'The normal-window layout does not preserve the preview while releasing space for primary card columns.'
	}
	foreach ($header in @('Rarity', 'Class', 'Premium', 'Extra', 'Potential', 'Assessment')) {
		$column = $collectionGrid.Columns | Where-Object { $_.Header -eq $header } | Select-Object -First 1
		if ($null -eq $column -or $column.MinWidth -lt 45) {
			throw "The compact $header column can collapse below a readable width."
		}
	}
	$window.Width = 950
	[void]$applyResponsiveLayout.Invoke($window, @())
	if ($window.FindName('CollectionCardPreview').Visibility.ToString() -ne 'Collapsed') {
		throw 'The narrow-window layout does not release the card-preview space.'
	}
	$window.Width = 1800
	[void]$applyResponsiveLayout.Invoke($window, @())
	if ($window.FindName('CollectionCardPreview').Visibility.ToString() -ne 'Visible' -or
		$window.FindName('CollectionCardPreview').Width -ne 245 -or
		$collectionTypeColumn.Visibility.ToString() -ne 'Visible') {
		throw 'The wide layout does not restore the card preview and detail columns.'
	}
    if ($collectionGrid.Columns[1].Width.UnitType.ToString() -ne 'SizeToCells') {
        throw 'The expansion column is not configured to size itself to expansion names.'
    }
    if ($collectionGrid.Columns.Header -contains 'Trial') {
        throw 'The removed Trial column is still present in the collection view.'
    }
    $deckMaxColumn = $collectionGrid.Columns | Where-Object { $_.Header -eq 'Deck max' } | Select-Object -First 1
    if ($null -eq $deckMaxColumn -or $deckMaxColumn.Binding.Path.Path -ne 'DeckMax') {
        throw 'The Deck max column is not bound to the legal card-copy limit.'
    }
    $collectionPremiumColumn = $collectionGrid.Columns | Where-Object { $_.Header -eq 'Premium' } | Select-Object -First 1
    if ($null -eq $collectionPremiumColumn -or $collectionPremiumColumn.SortMemberPath -ne 'PremiumSortOrder') {
        throw 'The Collection Premium column does not use the Diamond-to-Normal sort order.'
    }
    $filterChecks = @{
        ExpansionFilter = 'Expansions'
        ClassFilter = 'Classes'
        PlannerExpansionFilter = 'Expansions'
        PlannerRarityFilter = 'Rarities'
        PlannerClassFilter = 'Classes'
        PlannerFormatFilter = 'Formats'
        PlannerPremiumFilter = 'Premiums'
    }
    foreach ($filterName in $filterChecks.Keys) {
        $filter = $window.FindName($filterName)
        $localIndex = $filter.ReadLocalValue([System.Windows.Controls.Primitives.Selector]::SelectedIndexProperty)
        $sourceItems = $viewModel.($filterChecks[$filterName])
        if ($null -eq $filter -or [int]$localIndex -ne 0 -or $sourceItems.Count -eq 0 -or $sourceItems[0] -ne 'All') {
            throw "The $filterName dropdown does not visibly default to All."
        }
    }
    if ($null -eq $window.FindName('MarkUncraftableButton') -or
		$null -eq $window.FindName('CopyCardNameButton') -or
		$null -eq $window.FindName('CopyPlanCardNameButton') -or
		$null -eq $window.FindName('PlannerCriteriaText') -or
		$null -eq $window.FindName('FindCombinationButton') -or
        $null -eq $window.FindName('UncraftableReportsGrid') -or
        $null -eq $window.FindName('CopyUncraftableJsonButton')) {
        throw 'The manual uncraftable reporting controls were not found.'
    }
    if ($null -eq $window.FindName('ExpansionProtectionList')) {
        throw 'The Settings expansion-protection checklist was not found.'
    }
    foreach ($previewName in @('CollectionCardPreview', 'PlanCardPreview', 'ProtectedCardPreview')) {
        if ($null -eq $window.FindName($previewName)) {
            throw "The $previewName HDT card-art preview was not found."
        }
    }
	$bindableCardImageType = $assembly.GetType('DustFinder.Plugin.Controls.BindableCardImage', $true)
	$boundCardIdField = $bindableCardImageType.GetField('BoundCardIdProperty', [Reflection.BindingFlags]'Public,Static')
	if ($null -eq $boundCardIdField -or
		-not [System.Windows.DependencyProperty].IsAssignableFrom($boundCardIdField.FieldType) -or
		-not [System.Windows.Controls.ContentControl].IsAssignableFrom($bindableCardImageType)) {
		throw 'The bindable HDT CardImage adapter is not backed by a dependency property and containment control.'
	}
	$previewXamlPath = Join-Path $PSScriptRoot '..\src\DustFinder.Plugin\Views\MainWindow.xaml'
	$previewAdapterPath = Join-Path $PSScriptRoot '..\src\DustFinder.Plugin\Controls\BindableCardImage.cs'
	$mainViewModelPath = Join-Path $PSScriptRoot '..\src\DustFinder.Plugin\ViewModels\MainViewModel.cs'
	if (Test-Path -LiteralPath $previewXamlPath) {
		$previewXaml = Get-Content -LiteralPath $previewXamlPath -Raw
		if ($previewXaml -notmatch 'BindableCardImage\s+BoundCardId="\{Binding CardId\}"' -or
			$previewXaml -match '<[^>]*CardImage\s+CardId="\{Binding') {
			throw 'The card preview must bind through BindableCardImage.BoundCardId, not HDT CardImage.CardId directly.'
		}
	}
	if (Test-Path -LiteralPath $previewAdapterPath) {
		$previewAdapter = Get-Content -LiteralPath $previewAdapterPath -Raw
		if ($previewAdapter -notmatch 'Cards\.All\.TryGetValue' -or
			$previewAdapter -notmatch 'SetCardIdFromCard' -or
			$previewAdapter -notmatch 'ShowQuestionmark\s*=\s*false') {
			throw 'The card preview adapter is not resolving HDT cached art without the question-mark overlay.'
		}
	}
	if (Test-Path -LiteralPath $mainViewModelPath) {
		$mainViewModelSource = Get-Content -LiteralPath $mainViewModelPath -Raw
		if ($mainViewModelSource -notmatch 'IsSafeByRules\s*&&\s*FilterPlannerCard' -or
			$mainViewModelSource -notmatch 'PastedDeckUsage\.HaveSameCards' -or
			$mainViewModelSource -notmatch 'PastedDeckUsage\.GetUniqueName' -or
			$mainViewModelSource -notmatch 'PastedDeckUsage\.NormalizeDeckList' -or
			$mainViewModelSource -notmatch 'last-collection\.json' -or
			$mainViewModelSource -notmatch 'LoadCachedCollection' -or
			$mainViewModelSource -notmatch 'SaveCollectionCache') {
			throw 'Planner constraints, deck identity checks, or offline collection caching are not wired into the view model.'
		}
	}
    if ($null -eq $window.FindName('CollectionCountText') -or $null -eq $window.FindName('ProtectedCountText')) {
        throw 'The filtered and total card counters were not found.'
    }
    if ($null -ne $window.FindName('DustAmountFilter')) {
        throw 'The removed Collection dust-value filter is still present.'
    }
    $planGrid = $window.FindName('PlanGrid')
    $requiredPlanColumns = @('Name', 'Expansion', 'Rarity', 'Class', 'Type', 'Premium', 'Owned', 'Deck max', 'Keep', 'Extra', 'Dust ea.', 'Potential', 'Assessment', 'Plan copies', 'Plan dust')
    if ($null -eq $planGrid -or
		$planGrid.SelectionMode.ToString() -ne 'Extended' -or
		$planGrid.FrozenColumnCount -ne 1) {
        throw 'The dust-plan grid is not configured for Ctrl/Shift multi-selection.'
    }
    foreach ($header in $requiredPlanColumns) {
        if ($planGrid.Columns.Header -notcontains $header) {
            throw "The dust-plan grid is missing the $header column."
        }
    }
    $planPremiumColumn = $planGrid.Columns | Where-Object { $_.Header -eq 'Premium' } | Select-Object -First 1
    if ($planPremiumColumn.SortMemberPath -ne 'PremiumSortOrder') {
        throw 'The dust-plan Premium column does not use the Diamond-to-Normal sort order.'
    }
    $protectedGrid = $window.FindName('ProtectedGrid')
    $protectedNameColumn = $protectedGrid.Columns | Where-Object { $_.Header -eq 'Name' } | Select-Object -First 1
    $protectedClassColumn = $protectedGrid.Columns | Where-Object { $_.Header -eq 'Class' } | Select-Object -First 1
    $protectedReasonColumn = $protectedGrid.Columns | Where-Object { $_.Header -eq 'Reason' } | Select-Object -First 1
	$protectedPremiumColumn = $protectedGrid.Columns | Where-Object { $_.Header -eq 'Premium' } | Select-Object -First 1
	$protectedRarityColumn = $protectedGrid.Columns | Where-Object { $_.Header -eq 'Rarity' } | Select-Object -First 1
    if ($null -eq $protectedGrid -or
		$null -eq $protectedRarityColumn -or
		$protectedGrid.FrozenColumnCount -ne 1 -or
		$protectedNameColumn.Width.UnitType.ToString() -ne 'Star' -or
		$protectedNameColumn.MinWidth -lt 200 -or
        $protectedClassColumn.Width.Value -gt 180 -or
		$protectedReasonColumn.Width.UnitType.ToString() -ne 'Star' -or
		$protectedReasonColumn.MinWidth -lt 180 -or
		$protectedPremiumColumn.SortMemberPath -ne 'PremiumSortOrder') {
        throw 'The protected-card columns are not configured for responsive sizing.'
    }
    foreach ($filterName in @(
        'ProtectedExpansionFilter',
        'ProtectedRarityFilter',
        'ProtectedClassFilter',
        'ProtectedFormatFilter',
        'ProtectedPremiumFilter',
        'ProtectionReasonFilter')) {
        $filter = $window.FindName($filterName)
        if ($null -eq $filter -or [int]$filter.ReadLocalValue([System.Windows.Controls.Primitives.Selector]::SelectedIndexProperty) -ne 0) {
            throw "The $filterName protected-card filter does not default to All."
        }
    }
    $window.Close()
    Write-Output "Verified HDT plugin type $($type.FullName), visible version $($viewModel.PluginVersion), responsive layouts, constrained planning, offline collection caching, and copy/reporting controls."
}
finally {
    [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolver)
}
