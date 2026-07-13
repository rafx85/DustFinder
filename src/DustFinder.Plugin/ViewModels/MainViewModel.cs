using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using DustFinder.Core;
using DustFinder.Plugin.Infrastructure;
using DustFinder.Plugin.Integration;

namespace DustFinder.Plugin.ViewModels;

public sealed class MainViewModel : BindableBase
{
	private readonly IHdtCollectionSource _source;
	private readonly CollectionAnalyzer _analyzer = new();
	private readonly DustPlanner _planner = new();
	private readonly SnapshotComparer _snapshotComparer = new();
	private readonly AtomicJsonStore<UserSettings> _settingsStore = new();
	private readonly SnapshotRepository _snapshots;
	private readonly string _settingsPath;
	private CollectionLoadResult? _lastLoad;
	private string _searchText = string.Empty;
	private string _selectedExpansion = "All";
	private string _selectedRarity = "All";
	private string _selectedClass = "All";
	private string _selectedFormat = "All";
	private string _selectedPremium = "All";
	private string _selectedOwnership = "All";
	private CardRowViewModel? _selectedCard;
	private CardRowViewModel? _selectedProtectedCard;
	private PlanRowViewModel? _selectedPlanRow;
	private string _statusMessage = "Open Hearthstone, then choose Refresh collection.";
	private string _accountLabel = "No collection loaded";
	private int _targetDust = 1600;
	private int _plannedDust;
	private bool _isBusy;

	public MainViewModel(IHdtCollectionSource source, string dataDirectory)
	{
		_source = source ?? throw new ArgumentNullException(nameof(source));
		if(string.IsNullOrWhiteSpace(dataDirectory))
			throw new ArgumentException("A data directory is required.", nameof(dataDirectory));
		Directory.CreateDirectory(dataDirectory);
		_settingsPath = Path.Combine(dataDirectory, "settings.json");
		Settings = _settingsStore.LoadOrRecover(_settingsPath, () => new UserSettings());
		_snapshots = new SnapshotRepository(Path.Combine(dataDirectory, "snapshots"));

		CardsView = CollectionViewSource.GetDefaultView(Cards);
		CardsView.Filter = FilterCard;
		RefreshCommand = new RelayCommand(() => _ = RefreshAsync(), () => !IsBusy);
		PlanCommand = new RelayCommand(BuildPlan, () => !IsBusy && Cards.Count > 0);
		AddSelectedCommand = new RelayCommand(AddSelected, () => SelectedCard?.IsSafeByRules == true);
		RemovePlanCommand = new RelayCommand(RemoveSelectedPlan, () => SelectedPlanRow != null);
		ClearPlanCommand = new RelayCommand(ClearPlan, () => PlanRows.Count > 0);
		ProtectCommand = new RelayCommand(ProtectSelected, () => SelectedCard != null && !SelectedCard.IsProtected);
		UnprotectCommand = new RelayCommand(UnprotectSelected, () => SelectedProtectedCard != null);
		SaveSettingsCommand = new RelayCommand(SaveSettings);
	}

	public UserSettings Settings { get; }
	public ObservableCollection<CardRowViewModel> Cards { get; } = new();
	public ObservableCollection<CardRowViewModel> ProtectedCards { get; } = new();
	public ObservableCollection<PlanRowViewModel> PlanRows { get; } = new();
	public ObservableCollection<HistoryRowViewModel> HistoryRows { get; } = new();
	public ICollectionView CardsView { get; }
	public ObservableCollection<string> Expansions { get; } = new() { "All" };
	public ObservableCollection<string> Rarities { get; } = new() { "All", "Common", "Rare", "Epic", "Legendary", "Free", "Unknown" };
	public ObservableCollection<string> Classes { get; } = new() { "All" };
	public ObservableCollection<string> Formats { get; } = new() { "All", "Standard", "Wild", "Classic" };
	public ObservableCollection<string> Premiums { get; } = new() { "All", "Normal", "Golden", "Signature", "Diamond" };
	public ObservableCollection<string> OwnershipTypes { get; } = new() { "All", "Owned", "Temporary", "Protected", "Rule-safe", "Unused" };

	public RelayCommand RefreshCommand { get; }
	public RelayCommand PlanCommand { get; }
	public RelayCommand AddSelectedCommand { get; }
	public RelayCommand RemovePlanCommand { get; }
	public RelayCommand ClearPlanCommand { get; }
	public RelayCommand ProtectCommand { get; }
	public RelayCommand UnprotectCommand { get; }
	public RelayCommand SaveSettingsCommand { get; }

	public string SearchText
	{
		get => _searchText;
		set { if(Set(ref _searchText, value ?? string.Empty)) CardsView.Refresh(); }
	}

	public string SelectedExpansion
	{
		get => _selectedExpansion;
		set { if(Set(ref _selectedExpansion, value ?? "All")) CardsView.Refresh(); }
	}

	public string SelectedRarity
	{
		get => _selectedRarity;
		set { if(Set(ref _selectedRarity, value ?? "All")) CardsView.Refresh(); }
	}

	public string SelectedClass
	{
		get => _selectedClass;
		set { if(Set(ref _selectedClass, value ?? "All")) CardsView.Refresh(); }
	}

	public string SelectedFormat
	{
		get => _selectedFormat;
		set { if(Set(ref _selectedFormat, value ?? "All")) CardsView.Refresh(); }
	}

	public string SelectedPremium
	{
		get => _selectedPremium;
		set { if(Set(ref _selectedPremium, value ?? "All")) CardsView.Refresh(); }
	}

	public string SelectedOwnership
	{
		get => _selectedOwnership;
		set { if(Set(ref _selectedOwnership, value ?? "All")) CardsView.Refresh(); }
	}

	public CardRowViewModel? SelectedCard
	{
		get => _selectedCard;
		set
		{
			if(!Set(ref _selectedCard, value)) return;
			AddSelectedCommand.RaiseCanExecuteChanged();
			ProtectCommand.RaiseCanExecuteChanged();
		}
	}

	public CardRowViewModel? SelectedProtectedCard
	{
		get => _selectedProtectedCard;
		set { if(Set(ref _selectedProtectedCard, value)) UnprotectCommand.RaiseCanExecuteChanged(); }
	}

	public PlanRowViewModel? SelectedPlanRow
	{
		get => _selectedPlanRow;
		set { if(Set(ref _selectedPlanRow, value)) RemovePlanCommand.RaiseCanExecuteChanged(); }
	}

	public int TargetDust { get => _targetDust; set => Set(ref _targetDust, Math.Max(0, value)); }
	public int PlannedDust { get => _plannedDust; private set { if(Set(ref _plannedDust, value)) Raise(nameof(RemainingDust)); } }
	public int RemainingDust => Math.Max(0, TargetDust - PlannedDust);
	public string StatusMessage { get => _statusMessage; private set => Set(ref _statusMessage, value); }
	public string AccountLabel { get => _accountLabel; private set => Set(ref _accountLabel, value); }
	public bool IsBusy
	{
		get => _isBusy;
		private set
		{
			if(!Set(ref _isBusy, value)) return;
			RefreshCommand.RaiseCanExecuteChanged();
			PlanCommand.RaiseCanExecuteChanged();
		}
	}

	public async Task RefreshAsync()
	{
		if(IsBusy)
			return;
		IsBusy = true;
		StatusMessage = "Reading the current account collection through HDT...";
		try
		{
			_lastLoad = await _source.LoadAsync();
			Reanalyze();
			AccountLabel = $"{_lastLoad.Account.BattleTag} · {_lastLoad.Account.Region} · {_lastLoad.Entries.Sum(x => x.Count)} owned copies";
			UpdateHistory(_lastLoad);
			StatusMessage = $"Loaded {Cards.Count} card variants. Recommendations are advisory only.";
		}
		catch(Exception ex)
		{
			StatusMessage = ex.Message;
		}
		finally
		{
			IsBusy = false;
		}
	}

	private void Reanalyze()
	{
		if(_lastLoad == null)
			return;
		var results = _analyzer.Analyze(_lastLoad.Entries, _lastLoad.MaximumDeckCopies, Settings);
		Cards.Clear();
		foreach(var result in results)
			Cards.Add(new CardRowViewModel(result));

		ProtectedCards.Clear();
		foreach(var row in Cards.Where(x => x.IsProtected).GroupBy(x => x.CardId, StringComparer.OrdinalIgnoreCase).Select(x => x.First()))
			ProtectedCards.Add(row);
		RefreshFilterOptions();
		CardsView.Refresh();
		PlanCommand.RaiseCanExecuteChanged();
	}

	private void RefreshFilterOptions()
	{
		ReplaceOptions(Expansions, Cards.Select(x => x.Expansion));
		ReplaceOptions(Classes, Cards.Select(x => x.CardClass));
	}

	private static void ReplaceOptions(ObservableCollection<string> target, IEnumerable<string> values)
	{
		target.Clear();
		target.Add("All");
		foreach(var value in values.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.CurrentCultureIgnoreCase).OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase))
			target.Add(value);
	}

	private bool FilterCard(object item)
	{
		if(item is not CardRowViewModel card)
			return false;
		if(!Matches(SelectedExpansion, card.Expansion)
			|| !Matches(SelectedRarity, card.Rarity.ToString())
			|| !Matches(SelectedClass, card.CardClass)
			|| !Matches(SelectedFormat, card.Format)
			|| !Matches(SelectedPremium, card.Premium.ToString()))
			return false;

		var ownershipMatch = SelectedOwnership switch
		{
			"Owned" => card.Owned > 0,
			"Temporary" => card.Trial > 0,
			"Protected" => card.IsProtected,
			"Rule-safe" => card.IsSafeByRules,
			"Unused" => card.UsedInDeck == 0,
			_ => true
		};
		if(!ownershipMatch)
			return false;

		if(string.IsNullOrWhiteSpace(SearchText))
			return true;
		var haystack = string.Join(" ", card.Name, card.CardId, card.Expansion, card.Mechanics, card.Race, card.CardType, card.CardClass, card.Text);
		return haystack.IndexOf(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0;
	}

	private static bool Matches(string filter, string value) => filter == "All" || string.Equals(filter, value, StringComparison.CurrentCultureIgnoreCase);

	private void BuildPlan()
	{
		var candidates = Cards.Where(x => x.IsSafeByRules).Select(x => new PlanCandidate
		{
			Key = x.Key,
			CardName = x.Name,
			Premium = x.Premium,
			AvailableCopies = x.Extra,
			DustPerCopy = x.DustEach
		});
		var plan = _planner.Plan(TargetDust, candidates);
		PlanRows.Clear();
		foreach(var selection in plan.Selections)
		{
			PlanRows.Add(new PlanRowViewModel
			{
				Key = selection.Key,
				Name = selection.CardName,
				Premium = selection.Premium,
				Copies = selection.Copies,
				DustEach = selection.DustPerCopy
			});
		}
		UpdatePlanTotals();
		StatusMessage = plan.RemainingDust > 0
			? $"The eligible extras are {plan.RemainingDust} dust short of the target."
			: plan.OvershootDust > 0 ? $"Closest plan exceeds the target by {plan.OvershootDust} dust." : "Found an exact dust combination.";
	}

	private void AddSelected()
	{
		var card = SelectedCard;
		if(card == null || !card.IsSafeByRules)
			return;
		var row = PlanRows.FirstOrDefault(x => x.Key == card.Key);
		if(row == null)
		{
			row = new PlanRowViewModel { Key = card.Key, Name = card.Name, Premium = card.Premium, Copies = 0, DustEach = card.DustEach };
			PlanRows.Add(row);
		}
		if(row.Copies < card.Extra)
			row.Copies++;
		RefreshPlanRows();
	}

	private void RemoveSelectedPlan()
	{
		var row = SelectedPlanRow;
		if(row == null)
			return;
		if(row.Copies > 1)
			row.Copies--;
		else
			PlanRows.Remove(row);
		RefreshPlanRows();
	}

	private void ClearPlan()
	{
		if(MessageBox.Show("Clear every selected card from the dust plan?", "DustFinder confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
			return;
		PlanRows.Clear();
		RefreshPlanRows();
	}

	private void ProtectSelected()
	{
		var card = SelectedCard;
		if(card == null)
			return;
		if(MessageBox.Show($"Protect {card.Name} in every premium type? DustFinder will stop recommending it.", "Change protection rule", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
			return;
		Settings.ProtectedCardIds.Add(card.CardId);
		_settingsStore.Save(_settingsPath, Settings);
		Reanalyze();
	}

	private void UnprotectSelected()
	{
		var card = SelectedProtectedCard;
		if(card == null)
			return;
		if(MessageBox.Show($"Remove protection from {card.Name}? It may become eligible under your copy rules.", "Change protection rule", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
			return;
		Settings.ProtectedCardIds.Remove(card.CardId);
		_settingsStore.Save(_settingsPath, Settings);
		Reanalyze();
	}

	private void SaveSettings()
	{
		if(!Settings.NormalCountsTowardKeep && !Settings.GoldenCountsTowardKeep && !Settings.SignatureCountsTowardKeep && !Settings.DiamondCountsTowardKeep)
		{
			MessageBox.Show("At least one premium type must count toward copies to keep.", "DustFinder", MessageBoxButton.OK, MessageBoxImage.Warning);
			return;
		}
		if(MessageBox.Show("Apply these copy-protection rules? This can change which cards are recommended.", "Change protection rules", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
			return;
		Settings.KeepNonLegendary = Math.Max(0, Settings.KeepNonLegendary);
		Settings.KeepLegendary = Math.Max(0, Settings.KeepLegendary);
		_settingsStore.Save(_settingsPath, Settings);
		Reanalyze();
		StatusMessage = "Settings saved atomically; the previous file is retained as a backup.";
	}

	private void UpdateHistory(CollectionLoadResult load)
	{
		var snapshot = new CollectionSnapshot
		{
			CapturedAtUtc = DateTime.UtcNow,
			Account = load.Account,
			Entries = load.Entries
		};
		_snapshots.SaveIfChanged(snapshot);
		var history = _snapshots.Load(load.Account);
		HistoryRows.Clear();
		if(history.Count < 2)
			return;
		foreach(var difference in _snapshotComparer.Compare(history[history.Count - 2], history[history.Count - 1]))
		{
			HistoryRows.Add(new HistoryRowViewModel
			{
				Name = difference.CardName,
				Premium = difference.Premium,
				Before = difference.Before,
				After = difference.After
			});
		}
	}

	private void RefreshPlanRows()
	{
		var rows = PlanRows.ToList();
		PlanRows.Clear();
		foreach(var row in rows)
			PlanRows.Add(row);
		UpdatePlanTotals();
	}

	private void UpdatePlanTotals()
	{
		PlannedDust = PlanRows.Sum(x => x.Total);
		Raise(nameof(RemainingDust));
		ClearPlanCommand.RaiseCanExecuteChanged();
		RemovePlanCommand.RaiseCanExecuteChanged();
	}
}
