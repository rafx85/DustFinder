using System.Collections.Generic;
using DustFinder.Core;
using Xunit;

namespace DustFinder.Core.Tests;

public sealed class ExpansionProtectionInferenceTests
{
	[Fact]
	public void ExpansionIsInferredWhenEveryEligibleOwnedCardIsProtected()
	{
		var results = new[]
		{
			Result("CARD_1", "CATACLYSM", isProtected: true),
			Result("CARD_2", "CATACLYSM", isProtected: true),
			Result("OTHER", "TIME_TRAVEL", isProtected: false)
		};

		var inferred = ExpansionProtectionInference.GetFullyProtectedExpansions(results);

		Assert.Contains("CATACLYSM", inferred);
		Assert.DoesNotContain("TIME_TRAVEL", inferred);
	}

	[Fact]
	public void ExpansionIsNotInferredWhenOneEligibleOwnedCardIsUnprotected()
	{
		var inferred = ExpansionProtectionInference.GetFullyProtectedExpansions(
			new[] { Result("CARD_1", "CATACLYSM", true), Result("CARD_2", "CATACLYSM", false) });

		Assert.DoesNotContain("CATACLYSM", inferred);
	}

	[Fact]
	public void KeptCopyDoesNotBlockInference()
	{
		var kept = Result("KEPT_CARD", "CATACLYSM", false);
		kept.ReservedCopies = kept.Entry.Count;

		var inferred = ExpansionProtectionInference.GetFullyProtectedExpansions(
			new[] { Result("CARD_1", "CATACLYSM", true), kept });

		Assert.Contains("CATACLYSM", inferred);
	}

	[Fact]
	public void OtherProtectionSourcesCountAsProtected()
	{
		var pastedDeck = Result("CARD_1", "CATACLYSM", false);
		pastedDeck.IsInPastedDeck = true;
		var premiumRule = Result("CARD_2", "CATACLYSM", false);
		premiumRule.IsPremiumProtected = true;

		var inferred = ExpansionProtectionInference.GetFullyProtectedExpansions(
			new[] { pastedDeck, premiumRule });

		Assert.Contains("CATACLYSM", inferred);
	}

	private static AnalysisResult Result(string cardId, string expansion, bool isProtected) => new()
	{
		IsProtected = isProtected,
		IsDisenchantable = true,
		Entry = new CollectionEntry
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
		}
	};
}
