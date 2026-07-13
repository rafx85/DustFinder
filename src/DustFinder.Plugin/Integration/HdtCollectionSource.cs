using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DustFinder.Core;
using HearthDb;
using HearthDb.Enums;
using Hearthstone_Deck_Tracker;
using Hearthstone_Deck_Tracker.Hearthstone;
using CorePremiumType = DustFinder.Core.PremiumType;

namespace DustFinder.Plugin.Integration;

public sealed class HdtCollectionSource : IHdtCollectionSource
{
	private static readonly HashSet<string> KnownNonDisenchantableCards = new(StringComparer.OrdinalIgnoreCase)
	{
		"OG_280", // C'Thun grant
		"OG_281"  // Beckoner of Evil grant
	};

	public async Task<CollectionLoadResult> LoadAsync()
	{
		var collection = await CollectionHelpers.Hearthstone.GetCollection().ConfigureAwait(true);
		if(collection == null || collection.Cards.Count == 0)
			throw new InvalidOperationException("HDT could not read a collection. Start Hearthstone, sign in, and open My Collection, then try Refresh.");

		var result = new CollectionLoadResult
		{
			Account = new AccountIdentity
			{
				AccountHi = collection.AccountHi,
				AccountLo = collection.AccountLo,
				Region = DecodeRegion(collection.AccountHi),
				BattleTag = collection.BattleTag
			},
			MaximumDeckCopies = ReadDeckUsage()
		};

		foreach(var pair in collection.Cards)
		{
			if(!Cards.AllByDbfId.TryGetValue(pair.Key, out var card))
				continue;
			var metadata = CreateMetadata(card);
			var counts = pair.Value;
			for(var premiumIndex = 0; premiumIndex < 4; premiumIndex++)
			{
				var count = counts.Length > premiumIndex ? Math.Max(0, counts[premiumIndex]) : 0;
				var trialCount = counts.Length > premiumIndex + 4 ? Math.Max(0, counts[premiumIndex + 4]) : 0;
				if(count == 0 && trialCount == 0)
					continue;
				result.Entries.Add(new CollectionEntry
				{
					Card = metadata,
					Premium = (CorePremiumType)premiumIndex,
					Count = count,
					TrialCount = trialCount
				});
			}
		}

		return result;
	}

	private static CardMetadata CreateMetadata(HearthDb.Card card)
	{
		var rarity = (CardRarity)(int)card.Rarity;
		var isCraftable = card.Collectible
			&& rarity is CardRarity.Common or CardRarity.Rare or CardRarity.Epic or CardRarity.Legendary
			&& card.Set != CardSet.CORE
			&& !KnownNonDisenchantableCards.Contains(card.Id);
		var races = new[] { card.Race, card.SecondaryRace }
			.Where(x => x != Race.INVALID)
			.Select(x => x.ToString())
			.Distinct(StringComparer.OrdinalIgnoreCase);

		return new CardMetadata
		{
			DbfId = card.DbfId,
			CardId = card.Id ?? string.Empty,
			Name = card.Name ?? card.Id ?? card.DbfId.ToString(),
			Expansion = card.Set.ToString(),
			CardClass = card.Class.ToString(),
			CardType = card.Type.ToString(),
			Race = string.Join(", ", races),
			Mechanics = string.Join(", ", card.Mechanics ?? Array.Empty<string>()),
			Text = card.Text ?? string.Empty,
			Format = card.IsClassic ? "Classic" : card.IsWild ? "Wild" : "Standard",
			Rarity = rarity,
			IsCollectible = card.Collectible,
			IsCraftableByMetadata = isCraftable
		};
	}

	private static Dictionary<int, int> ReadDeckUsage()
	{
		var maximum = new Dictionary<int, int>();
		foreach(var deck in DeckList.Instance.Decks.Where(x => !x.Archived))
		{
			var version = deck.GetSelectedDeckVersion();
			var cards = version.Cards
				.Concat(version.Sideboards?.SelectMany(x => x.Cards) ?? Enumerable.Empty<Hearthstone_Deck_Tracker.Hearthstone.Card>())
				.GroupBy(x => x.DbfId)
				.ToDictionary(x => x.Key, x => x.Sum(card => Math.Max(0, card.Count)));
			foreach(var pair in cards)
				maximum[pair.Key] = maximum.TryGetValue(pair.Key, out var current) ? Math.Max(current, pair.Value) : pair.Value;
		}
		return maximum;
	}

	private static string DecodeRegion(ulong accountHi) => ((int)((accountHi >> 32) & 0xFF)) switch
	{
		1 => "US",
		2 => "EU",
		3 => "ASIA",
		5 => "CHINA",
		_ => "UNKNOWN"
	};
}
