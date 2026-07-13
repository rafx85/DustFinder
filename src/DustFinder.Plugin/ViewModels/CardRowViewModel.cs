using DustFinder.Core;

namespace DustFinder.Plugin.ViewModels;

public sealed class CardRowViewModel
{
	public CardRowViewModel(AnalysisResult result) => Result = result;
	public AnalysisResult Result { get; }
	public string Key => Result.Entry.Key;
	public int DbfId => Result.Entry.Card.DbfId;
	public string CardId => Result.Entry.Card.CardId;
	public string Name => Result.Entry.Card.Name;
	public string Expansion => Result.Entry.Card.Expansion;
	public string CardClass => Result.Entry.Card.CardClass;
	public string CardType => Result.Entry.Card.CardType;
	public string Race => Result.Entry.Card.Race;
	public string Mechanics => Result.Entry.Card.Mechanics;
	public string Text => Result.Entry.Card.Text;
	public string Format => Result.Entry.Card.Format;
	public CardRarity Rarity => Result.Entry.Card.Rarity;
	public PremiumType Premium => Result.Entry.Premium;
	public int Owned => Result.Entry.Count;
	public int Trial => Result.Entry.TrialCount;
	public int UsedInDeck => Result.UsedByKnownDecks;
	public int Keep => Result.ReservedCopies;
	public int Extra => Result.RecommendedCopies;
	public int DustEach => Result.DustPerCopy;
	public int PotentialDust => Extra * DustEach;
	public string Safety => Result.SafetyLabel;
	public bool IsProtected => Result.IsProtected;
	public bool IsSafeByRules => Result.IsSafeByRules;
}

public sealed class PlanRowViewModel
{
	public string Key { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public PremiumType Premium { get; set; }
	public int Copies { get; set; }
	public int DustEach { get; set; }
	public int Total => Copies * DustEach;
}

public sealed class HistoryRowViewModel
{
	public string Name { get; set; } = string.Empty;
	public PremiumType Premium { get; set; }
	public int Before { get; set; }
	public int After { get; set; }
	public int Delta => After - Before;
}
