using System;
using System.Collections.Generic;
using System.Linq;

namespace DustFinder.Core;

public sealed class CollectionAnalyzer
{
	private static readonly PremiumType[] KeepOrder =
	{
		PremiumType.Normal,
		PremiumType.Golden,
		PremiumType.Signature,
		PremiumType.Diamond
	};

	public IReadOnlyList<AnalysisResult> Analyze(
		IEnumerable<CollectionEntry> entries,
		IReadOnlyDictionary<int, int> maximumDeckCopies,
		UserSettings settings,
		ISet<int>? pastedDeckCardDbfIds = null)
	{
		if(entries == null)
			throw new ArgumentNullException(nameof(entries));
		if(maximumDeckCopies == null)
			throw new ArgumentNullException(nameof(maximumDeckCopies));
		if(settings == null)
			throw new ArgumentNullException(nameof(settings));

		var results = new List<AnalysisResult>();
		var manualUncraftableKeys = new HashSet<string>(
			(settings.ManualUncraftableCards ?? new List<ManualUncraftableCard>())
				.Where(x => x != null && !string.IsNullOrWhiteSpace(x.CardId))
				.Select(x => x.Key),
			StringComparer.OrdinalIgnoreCase);
		foreach(var group in entries.GroupBy(x => x.Card.DbfId))
		{
			var protectedByPastedDeck = pastedDeckCardDbfIds?.Contains(group.Key) == true;
			var variants = group.ToDictionary(x => x.Premium);
			var first = group.First();
			var baseKeep = first.Card.Rarity == CardRarity.Legendary
				? Math.Max(0, settings.KeepLegendary)
				: Math.Max(0, settings.KeepNonLegendary);
			var deckCopyLimit = first.Card.Rarity == CardRarity.Legendary ? 1 : 2;
			var deckCopies = maximumDeckCopies.TryGetValue(group.Key, out var used)
				? Math.Min(deckCopyLimit, Math.Max(0, used))
				: 0;
			var keepTarget = Math.Max(baseKeep, deckCopies);
			var remainingToReserve = keepTarget;
			var reserved = new Dictionary<PremiumType, int>();

			foreach(var premium in KeepOrder)
			{
				if(!variants.TryGetValue(premium, out var variant) || !settings.CountsTowardKeep(premium))
					continue;
				var count = Math.Max(0, variant.Count);
				var reserve = Math.Min(count, remainingToReserve);
				reserved[premium] = reserve;
				remainingToReserve -= reserve;
			}

			foreach(var entry in group.OrderBy(x => (int)x.Premium))
			{
				var protectedByUser = settings.ProtectedCardIds.Contains(entry.Card.CardId);
				var protectedByPremium = settings.IsPremiumProtected(entry.Premium);
				var manuallyUncraftable = manualUncraftableKeys.Contains($"{entry.Card.CardId}:{(int)entry.Premium}");
				var dust = DustValues.GetDisenchantValue(entry.Card.Rarity, entry.Premium);
				var premiumIsDisenchantable = entry.Premium != PremiumType.Golden
					|| entry.Card.IsGoldenDisenchantableByMetadata != false;
				var disenchantable = !manuallyUncraftable
					&& entry.Card.IsCollectible
					&& entry.Card.IsCraftableByMetadata
					&& premiumIsDisenchantable
					&& dust > 0;
				var reservedCopies = reserved.TryGetValue(entry.Premium, out var value) ? value : 0;
				var recommended = disenchantable && !protectedByUser && !protectedByPastedDeck && !protectedByPremium
					? Math.Max(0, entry.Count - reservedCopies)
					: 0;

				results.Add(new AnalysisResult
				{
					Entry = entry,
					UsedByKnownDecks = deckCopies,
					DeckCopyLimit = deckCopyLimit,
					KeepTarget = keepTarget,
					ReservedCopies = reservedCopies,
					RecommendedCopies = recommended,
					DustPerCopy = dust,
					IsProtected = protectedByUser,
					IsInPastedDeck = protectedByPastedDeck,
					IsManuallyUncraftable = manuallyUncraftable,
					IsPremiumProtected = protectedByPremium,
					IsDisenchantable = disenchantable
				});
			}
		}

		return results
			.OrderBy(x => x.Entry.Card.Name, StringComparer.CurrentCultureIgnoreCase)
			.ThenBy(x => x.Entry.Premium)
			.ToList();
	}
}

public static class DustValues
{
	public static bool IsHighestValueForRarity(CardRarity rarity, int dustPerCopy)
	{
		var highestValue = GetDisenchantValue(rarity, PremiumType.Golden);
		return highestValue > 0 && dustPerCopy >= highestValue;
	}

	public static int GetDisenchantValue(CardRarity rarity, PremiumType premium)
	{
		if(premium == PremiumType.Normal)
		{
			return rarity switch
			{
				CardRarity.Common => 5,
				CardRarity.Rare => 20,
				CardRarity.Epic => 100,
				CardRarity.Legendary => 400,
				_ => 0
			};
		}

		if(premium is PremiumType.Golden or PremiumType.Signature or PremiumType.Diamond)
		{
			return rarity switch
			{
				CardRarity.Common => 50,
				CardRarity.Rare => 100,
				CardRarity.Epic => 400,
				CardRarity.Legendary => 1600,
				_ => 0
			};
		}

		// Disenchantable Signature and Diamond cards return Golden-equivalent dust.
		// Special grants remain excluded when their card metadata marks them uncraftable.
		return 0;
	}
}
