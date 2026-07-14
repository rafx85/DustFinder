using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
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
	private string _plannerSearchText = string.Empty;
	private string _selectedPlannerExpansion = "All";
	private string _selectedPlannerRarity = "All";
	private string _selectedPlannerClass = "All";
	private string _selectedPlannerFormat = "All";
	private string _selectedPlannerPremium = "All";
	private string _protectedSearchText = string.Empty;
	private string _selectedProtectedExpansion = "All";
	private string _selectedProtectedRarity = "All";
	private string _selectedProtectedClass = "All";
	private string _selectedProtectedFormat = "All";
	private string _selectedProtectedPremium = "All";
	private string _selectedProtectionReason = "All";
	private CardRowViewModel? _selectedCard;
	private readonly List<CardRowViewModel> _selectedCards = new();
	private CardRowViewModel? _selectedProtectedCard;
	private ManualUncraftableCard? _selectedManualUncraftableCard;
	private PastedDeckDefinition? _selectedPastedDeck;
	private PlanRowViewModel? _selectedPlanRow;
	private readonly List<PlanRowViewModel> _selectedPlanRows = new();
	private string _pastedDeckText = string.Empty;
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
		if(Settings.SchemaVersion < 4)
		{
			Settings.ProtectNormalPremium = Settings.NormalCountsTowardKeep;
			Settings.ProtectGoldenPremium = Settings.GoldenCountsTowardKeep;
			Settings.ProtectSignaturePremium = Settings.SignatureCountsTowardKeep;
			Settings.ProtectDiamondPremium = Settings.DiamondCountsTowardKeep;
		}
		Settings.SchemaVersion = 5;
		Settings.ProtectedCardIds ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		Settings.ProtectedExpansions = new HashSet<string>(
			Settings.ProtectedExpansions ?? new HashSet<string>(),
			StringComparer.OrdinalIgnoreCase);
		Settings.PastedDecks ??= new List<PastedDeckDefinition>();
		Settings.ManualUncraftableCards ??= new List<ManualUncraftableCard>();
		foreach(var deck in Settings.PastedDecks.Where(x => x != null))
			PastedDecks.Add(deck);
		foreach(var card in Settings.ManualUncraftableCards.Where(x => x != null))
			ManualUncraftableCards.Add(card);
		_snapshots = new SnapshotRepository(Path.Combine(dataDirectory, "snapshots"));

		CardsView = CollectionViewSource.GetDefaultView(Cards);
		CardsView.Filter = FilterCard;
		ProtectedCardsView = CollectionViewSource.GetDefaultView(ProtectedCards);
		ProtectedCardsView.Filter = FilterProtectedCard;
		PlanRowsView = CollectionViewSource.GetDefaultView(PlanRows);
		PlanRowsView.Filter = FilterPlanRow;
		RefreshCommand = new RelayCommand(() => _ = RefreshAsync(), () => !IsBusy);
		PlanCommand = new RelayCommand(BuildPlan, () => !IsBusy && Cards.Count > 0);
		AddSelectedCommand = new RelayCommand(AddSelected, () => GetSelectedCards().Any(x => x.IsSafeByRules));
		RemovePlanCommand = new RelayCommand(RemoveSelectedPlan, () => SelectedPlanRow != null);
		ClearPlanCommand = new RelayCommand(ClearPlan, () => PlanRows.Count > 0);
		ProtectCommand = new RelayCommand(ProtectSelected, () => GetSelectedCards().Any(x => !x.IsProtected));
		CopyNameCommand = new RelayCommand(CopySelectedCardName, () => SelectedCard != null);
		ProtectPlanCommand = new RelayCommand(ProtectSelectedPlanCards, () => GetSelectedPlanRows().Any(x => !x.Card.IsProtected));
		MarkUncraftableCommand = new RelayCommand(MarkSelectedUncraftable, () => GetSelectedCards().Count > 0);
		RemoveUncraftableCommand = new RelayCommand(RemoveSelectedUncraftable, () => SelectedManualUncraftableCard != null);
		CopyUncraftableJsonCommand = new RelayCommand(CopyUncraftableJson, () => ManualUncraftableCards.Count > 0);
		UnprotectCommand = new RelayCommand(UnprotectSelected, () => SelectedProtectedCard?.IsProtected == true && !SelectedProtectedCard.IsInPastedDeck);
		ImportPastedDeckCommand = new RelayCommand(ImportPastedDeck, () => !string.IsNullOrWhiteSpace(PastedDeckText));
		RemovePastedDeckCommand = new RelayCommand(RemoveSelectedPastedDeck, () => SelectedPastedDeck != null);
		SaveSettingsCommand = new RelayCommand(SaveSettings);
	}

	public UserSettings Settings { get; }
	public ObservableCollection<CardRowViewModel> Cards { get; } = new();
	public ObservableCollection<CardRowViewModel> ProtectedCards { get; } = new();
	public ObservableCollection<ManualUncraftableCard> ManualUncraftableCards { get; } = new();
	public ObservableCollection<PastedDeckDefinition> PastedDecks { get; } = new();
	public ObservableCollection<ExpansionProtectionOption> ExpansionProtectionOptions { get; } = new();
	public ObservableCollection<PlanRowViewModel> PlanRows { get; } = new();
	public ObservableCollection<HistoryRowViewModel> HistoryRows { get; } = new();
	public ICollectionView CardsView { get; }
	public ICollectionView ProtectedCardsView { get; }
	public ICollectionView PlanRowsView { get; }
	public string CollectionCountLabel => $"Showing {CardsView.Cast<object>().Count()} of {Cards.Count} card variants";
	public string ProtectedCountLabel => $"Showing {ProtectedCardsView.Cast<object>().Count()} of {ProtectedCards.Count} card variants";
	public ObservableCollection<string> Expansions { get; } = new() { "All" };
	public ObservableCollection<string> Rarities { get; } = new() { "All", "Common", "Rare", "Epic", "Legendary" };
	public ObservableCollection<string> Classes { get; } = new() { "All" };
	public ObservableCollection<string> Formats { get; } = new() { "All", "Standard", "Wild" };
	public ObservableCollection<string> Premiums { get; } = new() { "All", "Normal", "Golden", "Signature", "Diamond" };
	public ObservableCollection<string> ProtectedExpansions { get; } = new() { "All" };
	public ObservableCollection<string> ProtectedRarities { get; } = new() { "All", "Common", "Rare", "Epic", "Legendary" };
	public ObservableCollection<string> ProtectedClasses { get; } = new() { "All" };
	public ObservableCollection<string> ProtectedFormats { get; } = new() { "All", "Standard", "Wild" };
	public ObservableCollection<string> ProtectedPremiums { get; } = new() { "All", "Normal", "Golden", "Signature", "Diamond" };
	public ObservableCollection<string> ProtectionReasons { get; } = new() { "All", "Protected by you", "Pasted deck", "Premium type", "Expansion" };

	public RelayCommand RefreshCommand { get; }
	public RelayCommand PlanCommand { get; }
	public RelayCommand AddSelectedCommand { get; }
	public RelayCommand RemovePlanCommand { get; }
	public RelayCommand ClearPlanCommand { get; }
	public RelayCommand ProtectCommand { get; }
	public RelayCommand CopyNameCommand { get; }
	public RelayCommand ProtectPlanCommand { get; }
	public RelayCommand MarkUncraftableCommand { get; }
	public RelayCommand RemoveUncraftableCommand { get; }
	public RelayCommand CopyUncraftableJsonCommand { get; }
	public RelayCommand UnprotectCommand { get; }
	public RelayCommand ImportPastedDeckCommand { get; }
	public RelayCommand RemovePastedDeckCommand { get; }
	public RelayCommand SaveSettingsCommand { get; }

	public string PastedDeckText
	{
		get => _pastedDeckText;
		set
		{
			if(!Set(ref _pastedDeckText, value ?? string.Empty))
				return;
			ImportPastedDeckCommand.RaiseCanExecuteChanged();
		}
	}

	public string SearchText
	{
		get => _searchText;
		set { if(Set(ref _searchText, value ?? string.Empty)) RefreshCardsView(); }
	}

	public string SelectedExpansion
	{
		get => _selectedExpansion;
		set { if(Set(ref _selectedExpansion, value ?? "All")) RefreshCardsView(); }
	}

	public string SelectedRarity
	{
		get => _selectedRarity;
		set { if(Set(ref _selectedRarity, value ?? "All")) RefreshCardsView(); }
	}

	public string SelectedClass
	{
		get => _selectedClass;
		set { if(Set(ref _selectedClass, value ?? "All")) RefreshCardsView(); }
	}

	public string SelectedFormat
	{
		get => _selectedFormat;
		set { if(Set(ref _selectedFormat, value ?? "All")) RefreshCardsView(); }
	}

	public string SelectedPremium
	{
		get => _selectedPremium;
		set { if(Set(ref _selectedPremium, value ?? "All")) RefreshCardsView(); }
	}

	public string PlannerSearchText
	{
		get => _plannerSearchText;
		set { if(Set(ref _plannerSearchText, value ?? string.Empty)) RefreshPlanRowsView(); }
	}

	public string SelectedPlannerExpansion
	{
		get => _selectedPlannerExpansion;
		set { if(Set(ref _selectedPlannerExpansion, value ?? "All")) RefreshPlanRowsView(); }
	}

	public string SelectedPlannerRarity
	{
		get => _selectedPlannerRarity;
		set { if(Set(ref _selectedPlannerRarity, value ?? "All")) RefreshPlanRowsView(); }
	}

	public string SelectedPlannerClass
	{
		get => _selectedPlannerClass;
		set { if(Set(ref _selectedPlannerClass, value ?? "All")) RefreshPlanRowsView(); }
	}

	public string SelectedPlannerFormat
	{
		get => _selectedPlannerFormat;
		set { if(Set(ref _selectedPlannerFormat, value ?? "All")) RefreshPlanRowsView(); }
	}

	public string SelectedPlannerPremium
	{
		get => _selectedPlannerPremium;
		set { if(Set(ref _selectedPlannerPremium, value ?? "All")) RefreshPlanRowsView(); }
	}

	public string ProtectedSearchText
	{
		get => _protectedSearchText;
		set { if(Set(ref _protectedSearchText, value ?? string.Empty)) RefreshProtectedCardsView(); }
	}

	public string SelectedProtectedExpansion
	{
		get => _selectedProtectedExpansion;
		set { if(Set(ref _selectedProtectedExpansion, value ?? "All")) RefreshProtectedCardsView(); }
	}

	public string SelectedProtectedRarity
	{
		get => _selectedProtectedRarity;
		set { if(Set(ref _selectedProtectedRarity, value ?? "All")) RefreshProtectedCardsView(); }
	}

	public string SelectedProtectedClass
	{
		get => _selectedProtectedClass;
		set { if(Set(ref _selectedProtectedClass, value ?? "All")) RefreshProtectedCardsView(); }
	}

	public string SelectedProtectedFormat
	{
		get => _selectedProtectedFormat;
		set { if(Set(ref _selectedProtectedFormat, value ?? "All")) RefreshProtectedCardsView(); }
	}

	public string SelectedProtectedPremium
	{
		get => _selectedProtectedPremium;
		set { if(Set(ref _selectedProtectedPremium, value ?? "All")) RefreshProtectedCardsView(); }
	}

	public string SelectedProtectionReason
	{
		get => _selectedProtectionReason;
		set { if(Set(ref _selectedProtectionReason, value ?? "All")) RefreshProtectedCardsView(); }
	}

	public CardRowViewModel? SelectedCard
	{
		get => _selectedCard;
		set
		{
			if(!Set(ref _selectedCard, value)) return;
			AddSelectedCommand.RaiseCanExecuteChanged();
			ProtectCommand.RaiseCanExecuteChanged();
			CopyNameCommand.RaiseCanExecuteChanged();
			MarkUncraftableCommand.RaiseCanExecuteChanged();
		}
	}

	public void SetSelectedCards(IEnumerable<CardRowViewModel> cards)
	{
		_selectedCards.Clear();
		_selectedCards.AddRange(cards.Where(x => x != null).Distinct());
		AddSelectedCommand.RaiseCanExecuteChanged();
		ProtectCommand.RaiseCanExecuteChanged();
		MarkUncraftableCommand.RaiseCanExecuteChanged();
	}

	public CardRowViewModel? SelectedProtectedCard
	{
		get => _selectedProtectedCard;
		set { if(Set(ref _selectedProtectedCard, value)) UnprotectCommand.RaiseCanExecuteChanged(); }
	}

	public ManualUncraftableCard? SelectedManualUncraftableCard
	{
		get => _selectedManualUncraftableCard;
		set { if(Set(ref _selectedManualUncraftableCard, value)) RemoveUncraftableCommand.RaiseCanExecuteChanged(); }
	}

	public PastedDeckDefinition? SelectedPastedDeck
	{
		get => _selectedPastedDeck;
		set { if(Set(ref _selectedPastedDeck, value)) RemovePastedDeckCommand.RaiseCanExecuteChanged(); }
	}

	public PlanRowViewModel? SelectedPlanRow
	{
		get => _selectedPlanRow;
		set
		{
			if(!Set(ref _selectedPlanRow, value))
				return;
			RemovePlanCommand.RaiseCanExecuteChanged();
			ProtectPlanCommand.RaiseCanExecuteChanged();
		}
	}

	public void SetSelectedPlanRows(IEnumerable<PlanRowViewModel> rows)
	{
		_selectedPlanRows.Clear();
		_selectedPlanRows.AddRange(rows.Where(x => x != null).Distinct());
		ProtectPlanCommand.RaiseCanExecuteChanged();
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
			StatusMessage = $"Loaded {Cards.Count} unprotected disenchantable card variants. Recommendations are advisory only.";
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
		var pastedDeckCards = PastedDeckUsage.GetProtectedCardDbfIds(PastedDecks);
		var maximumDeckCopies = PastedDeckUsage.MergeMaximumCopies(
			new Dictionary<int, int>(),
			PastedDecks);
		RefreshExpansionProtectionOptions(_lastLoad.Entries);
		var results = _analyzer.Analyze(_lastLoad.Entries, maximumDeckCopies, Settings, pastedDeckCards);
		var ownedRows = results
			.Where(x => x.Entry.Count > 0)
			.Select(x => new CardRowViewModel(x))
			.ToList();

		SelectedProtectedCard = null;
		ProtectedCards.Clear();
		foreach(var row in ownedRows
			.Where(x => x.IsProtected || x.IsInPastedDeck || x.IsPremiumProtected || x.IsExpansionProtected)
			.GroupBy(x => x.IsPremiumProtected ? $"variant:{x.Key}" : $"card:{x.CardId}", StringComparer.OrdinalIgnoreCase)
			.Select(x => x.First()))
			ProtectedCards.Add(row);
		RefreshProtectedFilterOptions();
		RefreshProtectedCardsView();

		SetSelectedCards(Array.Empty<CardRowViewModel>());
		SelectedCard = null;
		Cards.Clear();
		foreach(var row in ownedRows.Where(x => x.Result.IsDisenchantable && !x.IsProtected && !x.IsInPastedDeck && !x.IsPremiumProtected && !x.IsExpansionProtected))
			Cards.Add(row);
		RefreshFilterOptions();
		RefreshCardsView();
		PrunePlanRows();
		PlanCommand.RaiseCanExecuteChanged();
	}

	private void RefreshFilterOptions()
	{
		var selectedExpansion = _selectedExpansion;
		var selectedClass = _selectedClass;
		var selectedPlannerExpansion = _selectedPlannerExpansion;
		var selectedPlannerClass = _selectedPlannerClass;
		ReplaceOptions(Expansions, Cards.Select(x => x.Expansion));
		ReplaceOptions(Classes, Cards.SelectMany(x => x.CardClasses));
		_selectedExpansion = Expansions.Contains(selectedExpansion) ? selectedExpansion : "All";
		_selectedClass = Classes.Contains(selectedClass) ? selectedClass : "All";
		_selectedPlannerExpansion = Expansions.Contains(selectedPlannerExpansion) ? selectedPlannerExpansion : "All";
		_selectedPlannerClass = Classes.Contains(selectedPlannerClass) ? selectedPlannerClass : "All";
		Raise(nameof(SelectedExpansion));
		Raise(nameof(SelectedClass));
		Raise(nameof(SelectedPlannerExpansion));
		Raise(nameof(SelectedPlannerClass));
	}

	private void RefreshExpansionProtectionOptions(IEnumerable<CollectionEntry> entries)
	{
		var entryList = entries.ToList();
		var fullyProtectedExpansions = ExpansionProtectionInference.GetFullyProtectedExpansions(
			entryList,
			Settings.ProtectedCardIds);
		var expansions = entryList
			.Where(x => x.Count > 0
				&& x.Card.IsCollectible
				&& x.Card.IsCraftableByMetadata
				&& !string.IsNullOrWhiteSpace(x.Card.Expansion))
			.Select(x => new
			{
				Key = x.Card.Expansion.Trim(),
				Name = CardSetNames.GetDisplayName(x.Card.Expansion)
			})
			.GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
			.Select(x => x.First())
			.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
			.ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
			.ToList();

		ExpansionProtectionOptions.Clear();
		foreach(var expansion in expansions)
		{
			ExpansionProtectionOptions.Add(new ExpansionProtectionOption
			{
				Key = expansion.Key,
				Name = expansion.Name,
				IsProtected = Settings.ProtectedExpansions.Contains(expansion.Key)
					|| fullyProtectedExpansions.Contains(expansion.Key)
			});
		}
	}

	private void RefreshProtectedFilterOptions()
	{
		var selectedExpansion = _selectedProtectedExpansion;
		var selectedClass = _selectedProtectedClass;
		ReplaceOptions(ProtectedExpansions, ProtectedCards.Select(x => x.Expansion));
		ReplaceOptions(ProtectedClasses, ProtectedCards.SelectMany(x => x.CardClasses));
		_selectedProtectedExpansion = ProtectedExpansions.Contains(selectedExpansion) ? selectedExpansion : "All";
		_selectedProtectedClass = ProtectedClasses.Contains(selectedClass) ? selectedClass : "All";
		Raise(nameof(SelectedProtectedExpansion));
		Raise(nameof(SelectedProtectedClass));
	}

	private static void ReplaceOptions(ObservableCollection<string> target, IEnumerable<string> values)
	{
		PreserveAllOption(target);
		foreach(var value in values.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.CurrentCultureIgnoreCase).OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase))
			target.Add(value);
	}

	private static void PreserveAllOption(ObservableCollection<string> target)
	{
		if(target.Count == 0)
			target.Add("All");
		while(target.Count > 1)
			target.RemoveAt(target.Count - 1);
	}

	private bool FilterCard(object item)
	{
		if(item is not CardRowViewModel card)
			return false;
		if(!Matches(SelectedExpansion, card.Expansion)
			|| !Matches(SelectedRarity, card.Rarity.ToString())
			|| (SelectedClass != "All" && !card.CardClasses.Any(x => string.Equals(x, SelectedClass, StringComparison.CurrentCultureIgnoreCase)))
			|| !Matches(SelectedFormat, card.Format)
			|| !Matches(SelectedPremium, card.Premium.ToString()))
			return false;

		if(string.IsNullOrWhiteSpace(SearchText))
			return true;
		var haystack = string.Join(" ", card.Name, card.CardId, card.Expansion, card.Mechanics, card.Race, card.CardType, card.CardClass, card.Text);
		return haystack.IndexOf(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0;
	}

	private bool FilterPlannerCard(CardRowViewModel card)
	{
		if(!Matches(SelectedPlannerExpansion, card.Expansion)
			|| !Matches(SelectedPlannerRarity, card.Rarity.ToString())
			|| (SelectedPlannerClass != "All" && !card.CardClasses.Any(x => string.Equals(x, SelectedPlannerClass, StringComparison.CurrentCultureIgnoreCase)))
			|| !Matches(SelectedPlannerFormat, card.Format)
			|| !Matches(SelectedPlannerPremium, card.Premium.ToString()))
			return false;

		if(string.IsNullOrWhiteSpace(PlannerSearchText))
			return true;
		var haystack = string.Join(" ", card.Name, card.CardId, card.Expansion, card.Mechanics, card.Race, card.CardType, card.CardClass, card.Text);
		return haystack.IndexOf(PlannerSearchText.Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0;
	}

	private bool FilterPlanRow(object item) => item is PlanRowViewModel row && FilterPlannerCard(row.Card);

	private bool FilterProtectedCard(object item)
	{
		if(item is not CardRowViewModel card)
			return false;
		if(!Matches(SelectedProtectedExpansion, card.Expansion)
			|| !Matches(SelectedProtectedRarity, card.Rarity.ToString())
			|| (SelectedProtectedClass != "All" && !card.CardClasses.Any(x => string.Equals(x, SelectedProtectedClass, StringComparison.CurrentCultureIgnoreCase)))
			|| !Matches(SelectedProtectedFormat, card.Format)
			|| !Matches(SelectedProtectedPremium, card.Premium.ToString()))
			return false;

		var reasonMatches = SelectedProtectionReason switch
		{
			"Protected by you" => card.IsProtected,
			"Pasted deck" => card.IsInPastedDeck,
			"Premium type" => card.IsPremiumProtected,
			"Expansion" => card.IsExpansionProtected,
			_ => true
		};
		if(!reasonMatches)
			return false;

		if(string.IsNullOrWhiteSpace(ProtectedSearchText))
			return true;
		var haystack = string.Join(" ", card.Name, card.CardId, card.Expansion, card.CardClass, card.Premium, card.ProtectionReason);
		return haystack.IndexOf(ProtectedSearchText.Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0;
	}

	private static bool Matches(string filter, string value) => filter == "All" || string.Equals(filter, value, StringComparison.CurrentCultureIgnoreCase);

	private void RefreshCardsView()
	{
		CardsView.Refresh();
		Raise(nameof(CollectionCountLabel));
	}

	private void RefreshProtectedCardsView()
	{
		ProtectedCardsView.Refresh();
		Raise(nameof(ProtectedCountLabel));
	}

	private void RefreshPlanRowsView() => PlanRowsView.Refresh();

	private void BuildPlan()
	{
		var eligibleCards = Cards
			.Where(x => x.IsSafeByRules && FilterPlannerCard(x))
			.ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
		var candidates = eligibleCards.Values.Select(x => new PlanCandidate
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
				Card = eligibleCards[selection.Key],
				Copies = selection.Copies
			});
		}
		RefreshPlanRowsView();
		UpdatePlanTotals();
		StatusMessage = plan.RemainingDust > 0
			? $"The eligible extras are {plan.RemainingDust} dust short of the target."
			: plan.OvershootDust > 0 ? $"Closest plan exceeds the target by {plan.OvershootDust} dust." : "Found an exact dust combination.";
	}

	private void AddSelected()
	{
		var selectedCards = GetSelectedCards()
			.Where(x => x.IsSafeByRules)
			.GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
			.Select(x => x.First())
			.ToList();
		if(selectedCards.Count == 0)
			return;
		var added = 0;
		foreach(var card in selectedCards)
		{
			var row = PlanRows.FirstOrDefault(x => x.Key == card.Key);
			if(row == null)
			{
				row = new PlanRowViewModel { Card = card, Copies = 0 };
				PlanRows.Add(row);
			}
			if(row.Copies >= card.Extra)
				continue;
			row.Copies++;
			added++;
		}
		RefreshPlanRows();
		StatusMessage = added == 0
			? "Every selected card is already at its available-copy limit in the dust plan."
			: added == 1 ? "Added one selected card to the dust plan." : $"Added {added} selected cards to the dust plan.";
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
		=> ProtectCards(GetSelectedCards());

	private void CopySelectedCardName()
	{
		var card = SelectedCard;
		if(card == null)
			return;
		Clipboard.SetText(card.Name);
		StatusMessage = $"Copied {card.Name} to the clipboard.";
	}

	private void ProtectSelectedPlanCards()
		=> ProtectCards(GetSelectedPlanRows().Select(x => x.Card));

	private void ProtectCards(IEnumerable<CardRowViewModel> selectedCards)
	{
		var cards = selectedCards
			.Where(x => !x.IsProtected)
			.GroupBy(x => x.CardId, StringComparer.OrdinalIgnoreCase)
			.Select(x => x.First())
			.ToList();
		if(cards.Count == 0)
			return;
		var message = cards.Count == 1
			? $"Protect {cards[0].Name} in every premium type? DustFinder will stop recommending it."
			: $"Protect {cards.Count} selected cards in every premium type? DustFinder will stop recommending all of them.";
		if(MessageBox.Show(message, "Change protection rule", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
			return;
		foreach(var card in cards)
			Settings.ProtectedCardIds.Add(card.CardId);
		_settingsStore.Save(_settingsPath, Settings);
		_selectedPlanRows.Clear();
		SelectedPlanRow = null;
		Reanalyze();
		StatusMessage = cards.Count == 1 ? $"Protected {cards[0].Name}." : $"Protected {cards.Count} selected cards.";
	}

	private void MarkSelectedUncraftable()
	{
		var cards = GetSelectedCards()
			.GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
			.Select(x => x.First())
			.ToList();
		var existingKeys = new HashSet<string>(ManualUncraftableCards.Select(x => x.Key), StringComparer.OrdinalIgnoreCase);
		var added = 0;
		foreach(var card in cards)
		{
			var key = $"{card.CardId}:{(int)card.Premium}";
			if(!existingKeys.Add(key))
				continue;
			ManualUncraftableCards.Add(new ManualUncraftableCard
			{
				CardId = card.CardId,
				DbfId = card.DbfId,
				Name = card.Name,
				Expansion = card.Expansion,
				CardClass = card.CardClass,
				Rarity = card.Rarity,
				Premium = card.Premium,
				ReportedAtUtc = DateTime.UtcNow
			});
			added++;
		}
		if(added == 0)
		{
			StatusMessage = "The selected card variants are already marked uncraftable.";
			return;
		}
		SaveManualUncraftableCards();
		Reanalyze();
		StatusMessage = added == 1
			? "Marked one card variant uncraftable and removed it from Collection."
			: $"Marked {added} card variants uncraftable and removed them from Collection.";
	}

	private void RemoveSelectedUncraftable()
	{
		var card = SelectedManualUncraftableCard;
		if(card == null)
			return;
		ManualUncraftableCards.Remove(card);
		SelectedManualUncraftableCard = null;
		SaveManualUncraftableCards();
		Reanalyze();
		StatusMessage = $"Removed the manual uncraftable override for {card.Name} ({card.Premium}).";
	}

	private void CopyUncraftableJson()
	{
		var export = new ManualUncraftableExport
		{
			GeneratedAtUtc = DateTime.UtcNow.ToString("o"),
			Cards = ManualUncraftableCards
				.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
				.ThenBy(x => x.Premium)
				.Select(x => new ManualUncraftableExportCard
				{
					CardId = x.CardId,
					DbfId = x.DbfId,
					Name = x.Name,
					Expansion = x.Expansion,
					CardClass = x.CardClass,
					Rarity = x.Rarity.ToString(),
					Premium = x.Premium.ToString(),
					ReportedAtUtc = x.ReportedAtUtc.ToString("o")
				})
				.ToList()
		};
		var serializer = new DataContractJsonSerializer(typeof(ManualUncraftableExport));
		using var stream = new MemoryStream();
		serializer.WriteObject(stream, export);
		Clipboard.SetText(Encoding.UTF8.GetString(stream.ToArray()));
		StatusMessage = $"Copied {export.Cards.Count} manually marked uncraftable card variants as JSON.";
	}

	private void SaveManualUncraftableCards()
	{
		Settings.ManualUncraftableCards = ManualUncraftableCards.ToList();
		_settingsStore.Save(_settingsPath, Settings);
		CopyUncraftableJsonCommand.RaiseCanExecuteChanged();
	}

	private IReadOnlyList<CardRowViewModel> GetSelectedCards()
	{
		if(_selectedCards.Count > 0)
			return _selectedCards;
		return SelectedCard == null ? Array.Empty<CardRowViewModel>() : new[] { SelectedCard };
	}

	private IReadOnlyList<PlanRowViewModel> GetSelectedPlanRows()
	{
		if(_selectedPlanRows.Count > 0)
			return _selectedPlanRows;
		return SelectedPlanRow == null ? Array.Empty<PlanRowViewModel>() : new[] { SelectedPlanRow };
	}

	private void UnprotectSelected()
	{
		var card = SelectedProtectedCard;
		if(card == null || card.IsInPastedDeck)
			return;
		if(MessageBox.Show($"Remove protection from {card.Name}? It may become eligible under your copy rules.", "Change protection rule", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
			return;
		Settings.ProtectedCardIds.Remove(card.CardId);
		_settingsStore.Save(_settingsPath, Settings);
		Reanalyze();
		StatusMessage = $"Removed protection from {card.Name}.";
	}

	private void ImportPastedDeck()
	{
		try
		{
			var deck = PastedDeckImporter.Import(PastedDeckText, PastedDecks.Count + 1);
			if(PastedDecks.Any(x => string.Equals(x.DeckCode, deck.DeckCode, StringComparison.Ordinal)))
			{
				StatusMessage = $"{deck.Name} is already in your pasted decks.";
				return;
			}
			PastedDecks.Add(deck);
			SavePastedDecks();
			PastedDeckText = string.Empty;
			Reanalyze();
			StatusMessage = $"Imported {deck.Name}: {deck.UniqueCards} cards are now excluded from dust recommendations.";
		}
		catch(ArgumentException ex)
		{
			StatusMessage = ex.Message;
		}
	}

	private void RemoveSelectedPastedDeck()
	{
		var deck = SelectedPastedDeck;
		if(deck == null)
			return;
		if(MessageBox.Show($"Remove {deck.Name}? Cards used only by this pasted deck may become eligible for dust recommendations.", "Remove pasted deck", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
			return;
		PastedDecks.Remove(deck);
		SelectedPastedDeck = null;
		SavePastedDecks();
		Reanalyze();
		StatusMessage = $"Removed {deck.Name} from pasted-deck protection.";
	}

	private void SavePastedDecks()
	{
		Settings.PastedDecks = PastedDecks.ToList();
		_settingsStore.Save(_settingsPath, Settings);
	}

	private void SaveSettings()
	{
		if(MessageBox.Show("Apply these copy, premium-type, and expansion-protection rules? Checked premium types and expansions will be removed from Collection and dust recommendations.", "Change protection rules", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
			return;
		Settings.NormalCountsTowardKeep = Settings.ProtectNormalPremium;
		Settings.GoldenCountsTowardKeep = Settings.ProtectGoldenPremium;
		Settings.SignatureCountsTowardKeep = Settings.ProtectSignaturePremium;
		Settings.DiamondCountsTowardKeep = Settings.ProtectDiamondPremium;
		Settings.ProtectedExpansions = new HashSet<string>(
			ExpansionProtectionOptions.Where(x => x.IsProtected).Select(x => x.Key),
			StringComparer.OrdinalIgnoreCase);
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
		RefreshPlanRowsView();
		UpdatePlanTotals();
	}

	private void PrunePlanRows()
	{
		var eligible = Cards.Where(x => x.IsSafeByRules).ToDictionary(x => x.Key, x => x.Extra);
		foreach(var row in PlanRows.ToList())
		{
			if(!eligible.TryGetValue(row.Key, out var available) || available <= 0)
				PlanRows.Remove(row);
			else if(row.Copies > available)
				row.Copies = available;
		}
		RefreshPlanRows();
	}

	private void UpdatePlanTotals()
	{
		PlannedDust = PlanRows.Sum(x => x.Total);
		Raise(nameof(RemainingDust));
		ClearPlanCommand.RaiseCanExecuteChanged();
		RemovePlanCommand.RaiseCanExecuteChanged();
		ProtectPlanCommand.RaiseCanExecuteChanged();
	}
}

[DataContract]
public sealed class ManualUncraftableExport
{
	[DataMember(Order = 1)] public int SchemaVersion { get; set; } = 1;
	[DataMember(Order = 2)] public string GeneratedAtUtc { get; set; } = string.Empty;
	[DataMember(Order = 3)] public List<ManualUncraftableExportCard> Cards { get; set; } = new();
}

[DataContract]
public sealed class ManualUncraftableExportCard
{
	[DataMember(Order = 1)] public string CardId { get; set; } = string.Empty;
	[DataMember(Order = 2)] public int DbfId { get; set; }
	[DataMember(Order = 3)] public string Name { get; set; } = string.Empty;
	[DataMember(Order = 4)] public string Expansion { get; set; } = string.Empty;
	[DataMember(Order = 5)] public string CardClass { get; set; } = string.Empty;
	[DataMember(Order = 6)] public string Rarity { get; set; } = string.Empty;
	[DataMember(Order = 7)] public string Premium { get; set; } = string.Empty;
	[DataMember(Order = 8)] public string ReportedAtUtc { get; set; } = string.Empty;
}
