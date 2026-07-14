using System;
using System.Collections.Generic;
using System.Linq;

namespace DustFinder.Core;

public static class ExpansionProtectionInference
{
	public static ISet<string> GetFullyProtectedExpansions(
		IEnumerable<CollectionEntry> entries,
		IEnumerable<string> protectedCardIds)
	{
		if(entries == null)
			throw new ArgumentNullException(nameof(entries));
		if(protectedCardIds == null)
			throw new ArgumentNullException(nameof(protectedCardIds));

		var protectedIds = new HashSet<string>(
			protectedCardIds.Where(x => !string.IsNullOrWhiteSpace(x)),
			StringComparer.OrdinalIgnoreCase);

		return new HashSet<string>(entries
			.Where(x => x.Count > 0
				&& x.Card.IsCollectible
				&& x.Card.IsCraftableByMetadata
				&& !string.IsNullOrWhiteSpace(x.Card.Expansion)
				&& !string.IsNullOrWhiteSpace(x.Card.CardId))
			.GroupBy(x => x.Card.Expansion.Trim(), StringComparer.OrdinalIgnoreCase)
			.Where(group =>
			{
				var cardIds = group
					.Select(x => x.Card.CardId)
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.ToList();
				return cardIds.Count > 0 && cardIds.All(protectedIds.Contains);
			})
			.Select(group => group.Key),
			StringComparer.OrdinalIgnoreCase);
	}
}
