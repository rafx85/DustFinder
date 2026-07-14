using System;
using System.Linq;
using System.Threading.Tasks;
using DustFinder.Core;
using HearthDb;
using HearthDb.Enums;
using Hearthstone_Deck_Tracker.Hearthstone;
using CorePremiumType = DustFinder.Core.PremiumType;

namespace DustFinder.Plugin.Integration;

public sealed class HdtCollectionSource : IHdtCollectionSource
{
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
			}
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
		var multipleClassesMask = card.Entity.Tags
			.FirstOrDefault(x => x.EnumId == (int)GameTag.MULTIPLE_CLASSES)?.Value ?? 0;
		var cardClasses = CardClassNames.GetClassCodes(card.Class.ToString(), multipleClassesMask);
		var isCraftable = CardCraftabilityRules.IsPotentiallyCraftable(
			card.Collectible,
			rarity,
			card.Set.ToString(),
			card.Id);
		var howToEarnGolden = card.Entity.GetLocString(GameTag.HOW_TO_EARN_GOLDEN, Locale.enUS);
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
			CardClasses = cardClasses.ToList(),
			CardType = card.Type.ToString(),
			Race = string.Join(", ", races),
			Mechanics = string.Join(", ", card.Mechanics ?? Array.Empty<string>()),
			Text = card.Text ?? string.Empty,
			Format = IsStandardCard(card) ? "Standard" : "Wild",
			Rarity = rarity,
			IsCollectible = card.Collectible,
			IsCraftableByMetadata = isCraftable,
			IsGoldenDisenchantableByMetadata = CardCraftabilityRules.IsPotentiallyDisenchantableGolden(
				isCraftable,
				howToEarnGolden)
		};
	}

	private static bool IsStandardCard(HearthDb.Card card)
	{
		var hdtCard = new Hearthstone_Deck_Tracker.Hearthstone.Card(card, false);
		return CardUtils.FilterCardsByFormat(
			new[] { hdtCard },
			GameType.GT_RANKED,
			FormatType.FT_STANDARD).Any();
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
