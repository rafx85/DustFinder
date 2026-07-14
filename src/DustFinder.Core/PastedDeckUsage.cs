using System;
using System.Collections.Generic;
using System.Linq;

namespace DustFinder.Core;

public static class PastedDeckUsage
{
	public static bool HaveSameCards(PastedDeckDefinition? first, PastedDeckDefinition? second)
	{
		if(first == null || second == null)
			return false;

		var firstCards = ComparableCards(first);
		var secondCards = ComparableCards(second);
		return firstCards.Count == secondCards.Count
			&& firstCards.All(x => secondCards.TryGetValue(x.Key, out var copies) && copies == x.Value);
	}

	public static string GetUniqueName(string? preferredName, IEnumerable<PastedDeckDefinition> existingDecks)
	{
		if(existingDecks == null)
			throw new ArgumentNullException(nameof(existingDecks));

		var baseName = string.IsNullOrWhiteSpace(preferredName) ? "Pasted deck" : preferredName!.Trim();
		var names = new HashSet<string>(existingDecks
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Name))
			.Select(x => x.Name.Trim()), StringComparer.CurrentCultureIgnoreCase);
		if(!names.Contains(baseName))
			return baseName;

		for(var suffix = 2; ; suffix++)
		{
			var candidate = $"{baseName} ({suffix})";
			if(!names.Contains(candidate))
				return candidate;
		}
	}

	public static List<PastedDeckDefinition> NormalizeDeckList(IEnumerable<PastedDeckDefinition> decks)
	{
		if(decks == null)
			throw new ArgumentNullException(nameof(decks));

		var normalized = new List<PastedDeckDefinition>();
		foreach(var deck in decks.Where(x => x != null))
		{
			if(normalized.Any(existing => HaveSameCards(existing, deck)))
				continue;
			deck.Name = GetUniqueName(deck.Name, normalized);
			normalized.Add(deck);
		}
		return normalized;
	}

	public static HashSet<int> GetProtectedCardDbfIds(IEnumerable<PastedDeckDefinition> decks) =>
		new(decks
			.Where(x => x?.CardDbfIds != null)
			.SelectMany(x => x.CardDbfIds.Keys)
			.Where(x => x > 0));

	public static Dictionary<int, int> MergeMaximumCopies(
		IReadOnlyDictionary<int, int> hdtMaximumCopies,
		IEnumerable<PastedDeckDefinition> pastedDecks)
	{
		if(hdtMaximumCopies == null)
			throw new ArgumentNullException(nameof(hdtMaximumCopies));
		if(pastedDecks == null)
			throw new ArgumentNullException(nameof(pastedDecks));

		var maximum = hdtMaximumCopies.ToDictionary(x => x.Key, x => Math.Max(0, x.Value));
		foreach(var deck in pastedDecks.Where(x => x?.CardDbfIds != null))
		{
			foreach(var pair in deck.CardDbfIds)
			{
				if(pair.Key <= 0 || pair.Value <= 0)
					continue;
				maximum[pair.Key] = maximum.TryGetValue(pair.Key, out var current)
					? Math.Max(current, pair.Value)
					: pair.Value;
			}
		}
		return maximum;
	}

	private static Dictionary<int, int> ComparableCards(PastedDeckDefinition deck) =>
		(deck.CardDbfIds ?? new Dictionary<int, int>())
			.Where(x => x.Key > 0 && x.Value > 0)
			.ToDictionary(x => x.Key, x => x.Value);
}
