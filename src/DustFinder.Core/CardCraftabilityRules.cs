using System;
using System.Collections.Generic;

namespace DustFinder.Core;

public static class CardCraftabilityRules
{
	private static readonly string[] GoldenCraftingDescriptions =
	{
		"Obtained by crafting",
		"Not available in packs, must be crafted",
		"Crafting unlocked",
		"Can be crafted"
	};

	private static readonly HashSet<string> NonCraftableSets = new(StringComparer.OrdinalIgnoreCase)
	{
		"CORE",
		"PLACEHOLDER_202204",
		"EVENT",
		// VANILLA records mirror the same owned copies under their Legacy card IDs.
		// Including both would duplicate cards and dust recommendations.
		"VANILLA"
	};

	private static readonly HashSet<string> KnownNonCraftableCardIds = new(StringComparer.OrdinalIgnoreCase)
	{
		"OG_280", // C'Thun grant
		"OG_281"  // Beckoner of Evil grant
	};

	public static bool IsPotentiallyCraftable(
		bool collectible,
		CardRarity rarity,
		string? expansionCode,
		string? cardId)
	{
		if(!collectible || rarity is not (CardRarity.Common or CardRarity.Rare or CardRarity.Epic or CardRarity.Legendary))
			return false;
		var normalizedExpansion = expansionCode ?? string.Empty;
		var normalizedCardId = cardId ?? string.Empty;
		if(normalizedExpansion.Length > 0 && NonCraftableSets.Contains(normalizedExpansion))
			return false;
		if(normalizedCardId.Length > 0
			&& (normalizedCardId.StartsWith("CORE_", StringComparison.OrdinalIgnoreCase)
				|| normalizedCardId.StartsWith("EVENT_", StringComparison.OrdinalIgnoreCase)
				|| normalizedCardId.IndexOf("_EVENT_", StringComparison.OrdinalIgnoreCase) >= 0
				|| KnownNonCraftableCardIds.Contains(normalizedCardId)))
			return false;
		return true;
	}

	public static bool IsPotentiallyDisenchantableGolden(
		bool baseCardIsCraftable,
		string? howToEarnGolden)
	{
		if(!baseCardIsCraftable)
			return false;
		if(string.IsNullOrWhiteSpace(howToEarnGolden))
			return true;
		var normalizedDescription = howToEarnGolden!;

		return Array.Exists(
			GoldenCraftingDescriptions,
			description => normalizedDescription.IndexOf(description, StringComparison.OrdinalIgnoreCase) >= 0);
	}
}
