using System.Collections.Generic;
using DustFinder.Core;
using Xunit;

namespace DustFinder.Core.Tests;

public sealed class ExpansionProtectionInferenceTests
{
	[Fact]
	public void ExpansionIsInferredWhenEveryEligibleOwnedCardIsProtected()
	{
		var entries = new[]
		{
			Entry("CARD_1", "CATACLYSM"),
			Entry("CARD_2", "CATACLYSM"),
			Entry("OTHER", "TIME_TRAVEL")
		};

		var inferred = ExpansionProtectionInference.GetFullyProtectedExpansions(
			entries,
			new[] { "CARD_1", "CARD_2" });

		Assert.Contains("CATACLYSM", inferred);
		Assert.DoesNotContain("TIME_TRAVEL", inferred);
	}

	[Fact]
	public void ExpansionIsNotInferredWhenOneEligibleOwnedCardIsUnprotected()
	{
		var inferred = ExpansionProtectionInference.GetFullyProtectedExpansions(
			new[] { Entry("CARD_1", "CATACLYSM"), Entry("CARD_2", "CATACLYSM") },
			new[] { "CARD_1" });

		Assert.DoesNotContain("CATACLYSM", inferred);
	}

	[Fact]
	public void UncraftableCardDoesNotBlockInference()
	{
		var uncraftable = Entry("FREE_CARD", "CATACLYSM");
		uncraftable.Card.IsCraftableByMetadata = false;

		var inferred = ExpansionProtectionInference.GetFullyProtectedExpansions(
			new[] { Entry("CARD_1", "CATACLYSM"), uncraftable },
			new[] { "CARD_1" });

		Assert.Contains("CATACLYSM", inferred);
	}

	private static CollectionEntry Entry(string cardId, string expansion) => new()
	{
		Count = 1,
		Premium = PremiumType.Normal,
		Card = new CardMetadata
		{
			CardId = cardId,
			Expansion = expansion,
			IsCollectible = true,
			IsCraftableByMetadata = true
		}
	};
}
