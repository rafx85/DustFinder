using System;
using System.Collections.Generic;
using System.Linq;
using DustFinder.Core;
using DustFinder.Plugin.Infrastructure;

namespace DustFinder.Plugin.ViewModels;

public sealed class CardRowViewModel
{
	public CardRowViewModel(AnalysisResult result) => Result = result;
	public AnalysisResult Result { get; }
	public string Key => Result.Entry.Key;
	public int DbfId => Result.Entry.Card.DbfId;
	public string CardId => Result.Entry.Card.CardId;
	public string Name => Result.Entry.Card.Name;
	public string Expansion => CardSetNames.GetDisplayName(Result.Entry.Card.Expansion);
	public IReadOnlyList<string> CardClasses
	{
		get
		{
			var codes = Result.Entry.Card.CardClasses?.Count > 0
				? Result.Entry.Card.CardClasses
				: CardClassNames.GetClassCodes(Result.Entry.Card.CardClass, 0);
			return codes
				.Select(CardClassNames.GetDisplayName)
				.Distinct(StringComparer.CurrentCultureIgnoreCase)
				.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase)
				.ToList();
		}
	}
	public string CardClass => string.Join(", ", CardClasses);
	public string CardType => Result.Entry.Card.CardType;
	public string Race => Result.Entry.Card.Race;
	public string Mechanics => Result.Entry.Card.Mechanics;
	public string Text => Result.Entry.Card.Text;
	public string Format => Result.Entry.Card.Format;
	public CardRarity Rarity => Result.Entry.Card.Rarity;
	public PremiumType Premium => Result.Entry.Premium;
	public int PremiumSortOrder => Premium switch
	{
		PremiumType.Diamond => 3,
		PremiumType.Signature => 2,
		PremiumType.Golden => 1,
		PremiumType.Normal => 0,
		_ => -1
	};
	public int Owned => Result.Entry.Count;
	public int DeckMax => Result.DeckCopyLimit;
	public int Keep => Result.ReservedCopies;
	public int Extra => Result.RecommendedCopies;
	public int DustEach => Result.DustPerCopy;
	public int PotentialDust => Extra * DustEach;
	public string Safety => Result.SafetyLabel;
	public bool IsProtected => Result.IsProtected;
	public bool IsInPastedDeck => Result.IsInPastedDeck;
	public bool IsManuallyUncraftable => Result.IsManuallyUncraftable;
	public bool IsPremiumProtected => Result.IsPremiumProtected;
	public bool IsExpansionProtected => Result.IsExpansionProtected;
	public string ProtectionReason
	{
		get
		{
			var reasons = new List<string>();
			if(IsProtected)
				reasons.Add("Protected by you");
			if(IsInPastedDeck)
				reasons.Add("Used in pasted deck");
			if(IsPremiumProtected)
				reasons.Add("Protected premium type");
			if(IsExpansionProtected)
				reasons.Add("Protected expansion");
			return string.Join(" and ", reasons);
		}
	}
	public bool IsSafeByRules => Result.IsSafeByRules;
}

public sealed class ExpansionProtectionOption : BindableBase
{
	private bool _isProtected;
	public string Key { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public bool IsProtected { get => _isProtected; set => Set(ref _isProtected, value); }
}

public sealed class PlanRowViewModel
{
	public CardRowViewModel Card { get; set; } = null!;
	public string Key => Card.Key;
	public string Name => Card.Name;
	public string Expansion => Card.Expansion;
	public CardRarity Rarity => Card.Rarity;
	public string CardClass => Card.CardClass;
	public string CardType => Card.CardType;
	public string Format => Card.Format;
	public PremiumType Premium => Card.Premium;
	public int PremiumSortOrder => Card.PremiumSortOrder;
	public int Owned => Card.Owned;
	public int DeckMax => Card.DeckMax;
	public int Keep => Card.Keep;
	public int Extra => Card.Extra;
	public int DustEach => Card.DustEach;
	public int PotentialDust => Card.PotentialDust;
	public string Safety => Card.Safety;
	public int Copies { get; set; }
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
