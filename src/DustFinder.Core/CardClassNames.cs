using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DustFinder.Core;

public static class CardClassNames
{
	private static readonly IReadOnlyDictionary<string, string> DisplayNames =
		new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["DEATHKNIGHT"] = "Death Knight",
			["DEMONHUNTER"] = "Demon Hunter",
			["INVALID"] = "Unknown"
		};

	private static readonly IReadOnlyDictionary<int, string> ClassCodesByValue =
		new Dictionary<int, string>
		{
			[1] = "DEATHKNIGHT",
			[2] = "DRUID",
			[3] = "HUNTER",
			[4] = "MAGE",
			[5] = "PALADIN",
			[6] = "PRIEST",
			[7] = "ROGUE",
			[8] = "SHAMAN",
			[9] = "WARLOCK",
			[10] = "WARRIOR",
			[12] = "NEUTRAL",
			[14] = "DEMONHUNTER"
		};

	public static string GetDisplayName(string? classCode)
	{
		var normalized = (classCode ?? string.Empty).Trim();
		if(normalized.Length == 0)
			return "Unknown";
		if(DisplayNames.TryGetValue(normalized, out var displayName))
			return displayName;
		return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(normalized.Replace('_', ' ').ToLowerInvariant());
	}

	public static IReadOnlyList<string> GetClassCodes(string? primaryClassCode, int multipleClassesMask)
	{
		if(multipleClassesMask > 0)
		{
			var multipleClasses = ClassCodesByValue
				.Where(x => (multipleClassesMask & (1 << (x.Key - 1))) != 0)
				.Select(x => x.Value)
				.ToList();
			if(multipleClasses.Count > 0)
				return multipleClasses;
		}

		var normalized = (primaryClassCode ?? string.Empty).Trim();
		return new[] { normalized.Length == 0 || normalized.Equals("INVALID", StringComparison.OrdinalIgnoreCase) ? "UNKNOWN" : normalized };
	}
}
