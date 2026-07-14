using System;
using System.Collections.Generic;
using System.Linq;

namespace DustFinder.Core;

public static class PastedDeckUsage
{
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
}
