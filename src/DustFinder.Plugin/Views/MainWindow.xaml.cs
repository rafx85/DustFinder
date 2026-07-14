using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using DustFinder.Plugin.ViewModels;

namespace DustFinder.Plugin.Views;

public partial class MainWindow : Window
{
	private const double CompactLayoutWidth = 1400;
	private const double PreviewLayoutWidth = 1650;
	private readonly INotifyCollectionChanged? _cardsViewChanges;

	public MainWindow(MainViewModel viewModel)
	{
		InitializeComponent();
		DataContext = viewModel;
		PluginVersionText.Text = viewModel.PluginVersion;
		CollectionGrid.FrozenColumnCount = 1;
		PlanGrid.FrozenColumnCount = 1;
		ProtectedGrid.FrozenColumnCount = 1;
		_cardsViewChanges = viewModel.CardsView as INotifyCollectionChanged;
		if(_cardsViewChanges != null)
			_cardsViewChanges.CollectionChanged += CardsView_OnCollectionChanged;
		Loaded += async (_, _) =>
		{
			ApplyResponsiveLayout();
			await viewModel.RefreshAsync();
			EnsureFilterDefaults();
		};
		SizeChanged += (_, _) => ApplyResponsiveLayout();
		Closed += (_, _) =>
		{
			if(_cardsViewChanges != null)
				_cardsViewChanges.CollectionChanged -= CardsView_OnCollectionChanged;
		};
	}

	private void ApplyResponsiveLayout()
	{
		var availableWidth = ActualWidth > 0 ? ActualWidth : Width;
		var compact = availableWidth < CompactLayoutWidth;
		var showPreviews = availableWidth >= PreviewLayoutWidth;
		var previewVisibility = showPreviews ? Visibility.Visible : Visibility.Collapsed;
		CollectionCardPreview.Visibility = previewVisibility;
		PlanCardPreview.Visibility = previewVisibility;
		ProtectedCardPreview.Visibility = previewVisibility;

		SetColumnVisibility(CollectionGrid, !compact, "Type", "Owned", "Deck max", "Keep", "Dust ea.");
		SetColumnVisibility(PlanGrid, !compact, "Type", "Owned", "Deck max", "Keep", "Dust ea.", "Assessment");
	}

	private static void SetColumnVisibility(DataGrid grid, bool visible, params string[] headers)
	{
		var visibility = visible ? Visibility.Visible : Visibility.Collapsed;
		foreach(var column in grid.Columns.Where(column =>
			headers.Contains(column.Header as string, StringComparer.Ordinal)))
		{
			column.Visibility = visibility;
		}
	}

	private void CardsView_OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if(e.Action != NotifyCollectionChangedAction.Reset)
			return;
		Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
		{
			EnsureFilterDefaults();
			if(CollectionGrid.Items.Count == 0)
				return;
			CollectionGrid.ScrollIntoView(CollectionGrid.Items[0]);
			CollectionGrid.UpdateLayout();
		}));
	}

	private void EnsureFilterDefaults()
	{
		EnsureAllSelected(ExpansionFilter);
		EnsureAllSelected(ClassFilter);
		EnsureAllSelected(PlannerExpansionFilter);
		EnsureAllSelected(PlannerRarityFilter);
		EnsureAllSelected(PlannerClassFilter);
		EnsureAllSelected(PlannerFormatFilter);
		EnsureAllSelected(PlannerPremiumFilter);
		EnsureAllSelected(ProtectedExpansionFilter);
		EnsureAllSelected(ProtectedRarityFilter);
		EnsureAllSelected(ProtectedClassFilter);
		EnsureAllSelected(ProtectedFormatFilter);
		EnsureAllSelected(ProtectedPremiumFilter);
		EnsureAllSelected(ProtectionReasonFilter);
	}

	private static void EnsureAllSelected(ComboBox filter)
	{
		if(filter.SelectedItem == null && filter.Items.Contains("All"))
			filter.SelectedItem = "All";
	}

	private void CollectionGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if(DataContext is not MainViewModel viewModel || sender is not DataGrid grid)
			return;
		viewModel.SetSelectedCards(grid.SelectedItems.Cast<CardRowViewModel>());
	}

	private void PlanGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if(DataContext is not MainViewModel viewModel || sender is not DataGrid grid)
			return;
		viewModel.SetSelectedPlanRows(grid.SelectedItems.Cast<PlanRowViewModel>());
	}

	private void DataGrid_OnSorting(object sender, DataGridSortingEventArgs e)
	{
		if(sender is not DataGrid grid || string.IsNullOrWhiteSpace(e.Column.SortMemberPath))
			return;
		e.Handled = true;
		var direction = e.Column.SortDirection == ListSortDirection.Descending
			? ListSortDirection.Ascending
			: ListSortDirection.Descending;
		foreach(var column in grid.Columns)
			column.SortDirection = null;
		var view = CollectionViewSource.GetDefaultView(grid.ItemsSource);
		using(view.DeferRefresh())
		{
			view.SortDescriptions.Clear();
			view.SortDescriptions.Add(new SortDescription(e.Column.SortMemberPath, direction));
		}
		e.Column.SortDirection = direction;
		if(grid.Items.Count > 0)
			grid.ScrollIntoView(grid.Items[0]);
	}
}
