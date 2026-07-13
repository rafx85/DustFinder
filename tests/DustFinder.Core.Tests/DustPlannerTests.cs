using DustFinder.Core;
using Xunit;

namespace DustFinder.Core.Tests;

public sealed class DustPlannerTests
{
	[Fact]
	public void FindsExactCombinationInsteadOfGreedyOvershoot()
	{
		var plan = new DustPlanner().Plan(120, new[]
		{
			Candidate("epic", 1, 100),
			Candidate("rare", 6, 20),
			Candidate("golden-common", 1, 50)
		});

		Assert.Equal(120, plan.TotalDust);
		Assert.Equal(0, plan.RemainingDust);
	}

	[Fact]
	public void ChoosesSmallestReachableOvershoot()
	{
		var plan = new DustPlanner().Plan(95, new[]
		{
			Candidate("a", 1, 50),
			Candidate("b", 1, 100)
		});

		Assert.Equal(100, plan.TotalDust);
		Assert.Equal(5, plan.OvershootDust);
	}

	[Fact]
	public void ReportsShortfallWhenInventoryCannotReachTarget()
	{
		var plan = new DustPlanner().Plan(500, new[] { Candidate("a", 2, 100) });

		Assert.Equal(200, plan.TotalDust);
		Assert.Equal(300, plan.RemainingDust);
	}

	private static PlanCandidate Candidate(string key, int count, int dust) => new()
	{
		Key = key,
		CardName = key,
		AvailableCopies = count,
		DustPerCopy = dust
	};
}

