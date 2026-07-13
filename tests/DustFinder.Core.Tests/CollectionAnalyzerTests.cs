using System.Collections.Generic;
using System.Linq;
using DustFinder.Core;
using Xunit;

namespace DustFinder.Core.Tests;

public sealed class CollectionAnalyzerTests
{
	private readonly CollectionAnalyzer _analyzer = new();

	[Fact]
	public void VariantHandling_DefaultKeepsNormalAndTreatsCosmeticsAsZeroDust()
	{
		var entries = new[]
		{
			Entry(PremiumType.Normal, 3),
			Entry(PremiumType.Golden, 1),
			Entry(PremiumType.Signature, 1),
			Entry(PremiumType.Diamond, 1)
		};

		var results = _analyzer.Analyze(entries, new Dictionary<int, int>(), new UserSettings());

		Assert.Equal(1, results.Single(x => x.Entry.Premium == PremiumType.Normal).RecommendedCopies);
		Assert.Equal(1, results.Single(x => x.Entry.Premium == PremiumType.Golden).RecommendedCopies);
		Assert.False(results.Single(x => x.Entry.Premium == PremiumType.Signature).IsDisenchantable);
		Assert.False(results.Single(x => x.Entry.Premium == PremiumType.Diamond).IsDisenchantable);
	}

	[Fact]
	public void GoldenCanCountTowardConfiguredKeepTarget()
	{
		var settings = new UserSettings { GoldenCountsTowardKeep = true };
		var results = _analyzer.Analyze(
			new[] { Entry(PremiumType.Normal, 1), Entry(PremiumType.Golden, 2) },
			new Dictionary<int, int>(),
			settings);

		Assert.Equal(0, results.Single(x => x.Entry.Premium == PremiumType.Normal).RecommendedCopies);
		Assert.Equal(1, results.Single(x => x.Entry.Premium == PremiumType.Golden).RecommendedCopies);
	}

	[Fact]
	public void ProtectedCardHasNoRecommendations()
	{
		var settings = new UserSettings();
		settings.ProtectedCardIds.Add("CARD_001");
		var result = _analyzer.Analyze(new[] { Entry(PremiumType.Normal, 8) }, new Dictionary<int, int>(), settings).Single();

		Assert.True(result.IsProtected);
		Assert.Equal(0, result.RecommendedCopies);
	}

	[Fact]
	public void DeckUsageRaisesKeepTarget()
	{
		var result = _analyzer.Analyze(
			new[] { Entry(PremiumType.Normal, 4) },
			new Dictionary<int, int> { [42] = 3 },
			new UserSettings()).Single();

		Assert.Equal(3, result.KeepTarget);
		Assert.Equal(1, result.RecommendedCopies);
	}

	[Fact]
	public void UncraftableCardIsNeverRecommended()
	{
		var entry = Entry(PremiumType.Normal, 9);
		entry.Card.IsCraftableByMetadata = false;
		var result = _analyzer.Analyze(new[] { entry }, new Dictionary<int, int>(), new UserSettings()).Single();

		Assert.False(result.IsDisenchantable);
		Assert.Equal(0, result.RecommendedCopies);
	}

	[Fact]
	public void UnusedIsNotAutomaticallySafeWhenCopiesAreWithinKeepTarget()
	{
		var result = _analyzer.Analyze(
			new[] { Entry(PremiumType.Normal, 2) },
			new Dictionary<int, int>(),
			new UserSettings()).Single();

		Assert.True(result.IsUnusedByKnownDecks);
		Assert.False(result.IsSafeByRules);
		Assert.Contains("not automatically safe", result.SafetyLabel);
	}

	private static CollectionEntry Entry(PremiumType premium, int count) => new()
	{
		Premium = premium,
		Count = count,
		Card = new CardMetadata
		{
			DbfId = 42,
			CardId = "CARD_001",
			Name = "Test Card",
			Rarity = CardRarity.Common,
			IsCollectible = true,
			IsCraftableByMetadata = true
		}
	};
}

