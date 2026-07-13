using System;
using System.Collections.Generic;
using System.Linq;

namespace DustFinder.Core;

public sealed class SnapshotComparer
{
	public IReadOnlyList<SnapshotDifference> Compare(CollectionSnapshot older, CollectionSnapshot newer)
	{
		if(older == null)
			throw new ArgumentNullException(nameof(older));
		if(newer == null)
			throw new ArgumentNullException(nameof(newer));

		var before = older.Entries.ToDictionary(x => x.Key, StringComparer.Ordinal);
		var after = newer.Entries.ToDictionary(x => x.Key, StringComparer.Ordinal);
		var keys = before.Keys.Concat(after.Keys).Distinct(StringComparer.Ordinal);
		var differences = new List<SnapshotDifference>();
		foreach(var key in keys)
		{
			before.TryGetValue(key, out var oldEntry);
			after.TryGetValue(key, out var newEntry);
			var oldCount = oldEntry?.Count ?? 0;
			var newCount = newEntry?.Count ?? 0;
			if(oldCount == newCount)
				continue;
			var entry = newEntry ?? oldEntry!;
			differences.Add(new SnapshotDifference
			{
				DbfId = entry.Card.DbfId,
				CardId = entry.Card.CardId,
				CardName = entry.Card.Name,
				Premium = entry.Premium,
				Before = oldCount,
				After = newCount
			});
		}

		return differences
			.OrderBy(x => x.CardName, StringComparer.CurrentCultureIgnoreCase)
			.ThenBy(x => x.Premium)
			.ToList();
	}

	public bool HasSameCounts(CollectionSnapshot left, CollectionSnapshot right) => Compare(left, right).Count == 0;
}

