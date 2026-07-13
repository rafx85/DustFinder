using System;
using System.Collections.Generic;
using System.Linq;

namespace DustFinder.Core;

public sealed class DustPlanner
{
	private sealed class State
	{
		public int Copies { get; set; }
		public Dictionary<string, int> Selection { get; set; } = new(StringComparer.Ordinal);
	}

	public DustPlan Plan(int targetDust, IEnumerable<PlanCandidate> candidates)
	{
		if(targetDust <= 0)
			return new DustPlan { TargetDust = Math.Max(0, targetDust) };
		if(candidates == null)
			throw new ArgumentNullException(nameof(candidates));

		var usable = candidates
			.Where(x => x.AvailableCopies > 0 && x.DustPerCopy > 0)
			.GroupBy(x => x.Key, StringComparer.Ordinal)
			.Select(x => x.First())
			.OrderBy(x => x.CardName, StringComparer.CurrentCultureIgnoreCase)
			.ThenBy(x => x.Premium)
			.ToList();
		if(usable.Count == 0)
			return new DustPlan { TargetDust = targetDust };

		var upperBound = checked(targetDust + usable.Max(x => x.DustPerCopy) - 1);
		var states = new Dictionary<int, State> { [0] = new State() };

		foreach(var candidate in usable)
		{
			for(var copy = 0; copy < candidate.AvailableCopies; copy++)
			{
				var snapshot = states.ToArray();
				foreach(var pair in snapshot)
				{
					var total = pair.Key + candidate.DustPerCopy;
					if(total > upperBound)
						continue;
					var copies = pair.Value.Copies + 1;
					if(states.TryGetValue(total, out var current) && current.Copies <= copies)
						continue;

					var selection = new Dictionary<string, int>(pair.Value.Selection, StringComparer.Ordinal);
					selection[candidate.Key] = selection.TryGetValue(candidate.Key, out var existing) ? existing + 1 : 1;
					states[total] = new State { Copies = copies, Selection = selection };
				}
			}
		}

		var bestTotal = states.Keys.Where(x => x >= targetDust).DefaultIfEmpty(states.Keys.Max()).Min();
		var best = states[bestTotal];
		var byKey = usable.ToDictionary(x => x.Key, StringComparer.Ordinal);
		return new DustPlan
		{
			TargetDust = targetDust,
			TotalDust = bestTotal,
			Selections = best.Selection
				.Select(x => new PlanSelection
				{
					Key = x.Key,
					CardName = byKey[x.Key].CardName,
					Premium = byKey[x.Key].Premium,
					Copies = x.Value,
					DustPerCopy = byKey[x.Key].DustPerCopy
				})
				.OrderBy(x => x.CardName, StringComparer.CurrentCultureIgnoreCase)
				.ThenBy(x => x.Premium)
				.ToList()
		};
	}
}

