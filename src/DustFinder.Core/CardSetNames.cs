using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DustFinder.Core;

public static class CardSetNames
{
	private static readonly IReadOnlyDictionary<string, string> DisplayNames =
		new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["ALTERAC_VALLEY"] = "Fractured in Alterac Valley",
			["BATTLE_OF_THE_BANDS"] = "Festival of Legends",
			["BLACK_TEMPLE"] = "Ashes of Outland",
			["BOOMSDAY"] = "The Boomsday Project",
			["CATACLYSM"] = "Cataclysm",
			["CORE"] = "Core",
			["DALARAN"] = "Rise of Shadows",
			["DARKMOON_FAIRE"] = "Madness at the Darkmoon Faire",
			["DEMON_HUNTER_INITIATE"] = "Demon Hunter Initiate",
			["DRAGONS"] = "Descent of Dragons",
			["EMERALD_DREAM"] = "Into the Emerald Dream",
			["ESCAPEFROM_VIOLET_HOLD"] = "Escape from Violet Hold",
			["EVENT"] = "Special Events",
			["EXPERT1"] = "Legacy",
			["FP2"] = "Blackrock Mountain",
			["GANGS"] = "Mean Streets of Gadgetzan",
			["GILNEAS"] = "The Witchwood",
			["HERO_SKINS"] = "Hero Skins",
			["ICECROWN"] = "Knights of the Frozen Throne",
			["ISLAND_VACATION"] = "Perils in Paradise",
			["KARA"] = "One Night in Karazhan",
			["LEGACY"] = "Legacy",
			["LOE"] = "League of Explorers",
			["LOOTAPALOOZA"] = "Kobolds & Catacombs",
			["NAXX"] = "Curse of Naxxramas",
			["OG"] = "Whispers of the Old Gods",
			["PATH_OF_ARTHAS"] = "Path of Arthas",
			["PE1"] = "Goblins vs Gnomes",
			["PLACEHOLDER_202204"] = "Core",
			["RETURN_OF_THE_LICH_KING"] = "March of the Lich King",
			["REVENDRETH"] = "Murder at Castle Nathria",
			["SCHOLOMANCE"] = "Scholomance Academy",
			["SPACE"] = "The Great Dark Beyond",
			["STORMWIND"] = "United in Stormwind",
			["TEMP1"] = "The Grand Tournament",
			["THE_BARRENS"] = "Forged in the Barrens",
			["THE_LOST_CITY"] = "The Lost City of Un'Goro",
			["THE_SUNKEN_CITY"] = "Voyage to the Sunken City",
			["TIME_TRAVEL"] = "Across the Timeways",
			["TITANS"] = "TITANS",
			["TROLL"] = "Rastakhan's Rumble",
			["ULDUM"] = "Saviors of Uldum",
			["UNGORO"] = "Journey to Un'Goro",
			["VANILLA"] = "Classic",
			["WHIZBANGS_WORKSHOP"] = "Whizbang's Workshop",
			["WILD_WEST"] = "Showdown in the Badlands",
			["WONDERS"] = "Caverns of Time",
			["YEAR_OF_THE_DRAGON"] = "Galakrond's Awakening"
		};

	public static string GetDisplayName(string? setCode)
	{
		if(string.IsNullOrWhiteSpace(setCode))
			return "Unknown";

		var normalized = setCode!.Trim();
		if(DisplayNames.TryGetValue(normalized, out var displayName))
			return displayName;

		var words = normalized.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
		return string.Join(" ", words.Select(FormatWord));
	}

	private static string FormatWord(string word)
	{
		if(word.All(char.IsDigit))
			return word;
		return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(word.ToLowerInvariant());
	}
}
