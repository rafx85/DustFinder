using System;
using System.Collections.Generic;
using System.Linq;

namespace DustFinder.Core;

public static class ExpansionProtectionInference
{
	public static ISet<string> GetFullyProtectedExpansions(
		IEnumerable<AnalysisResult> results)
	{
		if(results == null)
			throw new ArgumentNullException(nameof(results));

		return new HashSet<string>(results
			.Where(x => x.IsDisenchantable
				&& x.Entry.Count > x.ReservedCopies
				&& !string.IsNullOrWhiteSpace(x.Entry.Card.Expansion))
			.GroupBy(x => x.Entry.Card.Expansion.Trim(), StringComparer.OrdinalIgnoreCase)
			.Where(group =>
			{
				var variants = group.ToList();
				return variants.Count > 0 && variants.All(x =>
					x.IsProtected
					|| x.IsInPastedDeck
					|| x.IsPremiumProtected
					|| x.IsExpansionProtected);
			})
			.Select(group => group.Key),
			StringComparer.OrdinalIgnoreCase);
	}
}
