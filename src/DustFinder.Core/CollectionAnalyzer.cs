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
		UserSettings settings)
	{
		if(entries == null)
			throw new ArgumentNullException(nameof(entries));
		if(maximumDeckCopies == null)
			throw new ArgumentNullException(nameof(maximumDeckCopies));
		if(settings == null)
			throw new ArgumentNullException(nameof(settings));

		var results = new List<AnalysisResult>();
		foreach(var group in entries.GroupBy(x => x.Card.DbfId))
		{
			var variants = group.ToDictionary(x => x.Premium);
			var first = group.First();
			var baseKeep = first.Card.Rarity == CardRarity.Legendary
				? Math.Max(0, settings.KeepLegendary)
				: Math.Max(0, settings.KeepNonLegendary);
			var deckCopies = maximumDeckCopies.TryGetValue(group.Key, out var used) ? Math.Max(0, used) : 0;
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
				var dust = DustValues.GetDisenchantValue(entry.Card.Rarity, entry.Premium);
				var disenchantable = entry.Card.IsCollectible && entry.Card.IsCraftableByMetadata && dust > 0;
				var reservedCopies = reserved.TryGetValue(entry.Premium, out var value) ? value : 0;
				var recommended = disenchantable && !protectedByUser
					? Math.Max(0, entry.Count - reservedCopies)
					: 0;

				results.Add(new AnalysisResult
				{
					Entry = entry,
					UsedByKnownDecks = deckCopies,
					KeepTarget = keepTarget,
					ReservedCopies = reservedCopies,
					RecommendedCopies = recommended,
					DustPerCopy = dust,
					IsProtected = protectedByUser,
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

		if(premium == PremiumType.Golden)
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

		// Current Diamond and Signature cards are intentionally treated as zero.
		// DustFinder must under-promise rather than suggest an uncraftable cosmetic.
		return 0;
	}
}

