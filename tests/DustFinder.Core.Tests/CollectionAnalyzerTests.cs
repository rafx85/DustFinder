using System.Collections.Generic;
using System.Linq;
using DustFinder.Core;
using Xunit;

namespace DustFinder.Core.Tests;

public sealed class CollectionAnalyzerTests
{
	private readonly CollectionAnalyzer _analyzer = new();

	[Fact]
	public void VariantHandling_DefaultKeepsNormalAndValuesDisenchantableCosmeticsAsGolden()
	{
		var entries = new[]
		{
			Entry(PremiumType.Normal, 3),
			Entry(PremiumType.Golden, 1),
			Entry(PremiumType.Signature, 1),
			Entry(PremiumType.Diamond, 1)
		};

		var results = _analyzer.Analyze(entries, new Dictionary<int, int>(), UnprotectedSettings());

		Assert.Equal(1, results.Single(x => x.Entry.Premium == PremiumType.Normal).RecommendedCopies);
		Assert.Equal(1, results.Single(x => x.Entry.Premium == PremiumType.Golden).RecommendedCopies);
		var signature = results.Single(x => x.Entry.Premium == PremiumType.Signature);
		var diamond = results.Single(x => x.Entry.Premium == PremiumType.Diamond);
		Assert.True(signature.IsDisenchantable);
		Assert.Equal(50, signature.DustPerCopy);
		Assert.True(diamond.IsDisenchantable);
		Assert.Equal(50, diamond.DustPerCopy);
	}

	[Fact]
	public void PremiumVariantsShareConfiguredKeepTarget()
	{
		var settings = UnprotectedSettings();
		settings.GoldenCountsTowardKeep = true;
		var results = _analyzer.Analyze(
			new[] { Entry(PremiumType.Normal, 1), Entry(PremiumType.Golden, 2) },
			new Dictionary<int, int>(),
			settings);

		Assert.Equal(0, results.Single(x => x.Entry.Premium == PremiumType.Normal).RecommendedCopies);
		Assert.Equal(1, results.Single(x => x.Entry.Premium == PremiumType.Golden).RecommendedCopies);
	}

	[Fact]
	public void CheckedPremiumTypeIsProtectedAndNotRecommended()
	{
		var settings = new UserSettings
		{
			ProtectNormalPremium = false,
			SignatureCountsTowardKeep = true,
			ProtectSignaturePremium = true
		};
		var result = _analyzer.Analyze(
			new[] { Entry(PremiumType.Signature, 2) },
			new Dictionary<int, int>(),
			settings).Single();

		Assert.True(result.IsPremiumProtected);
		Assert.False(result.IsSafeByRules);
		Assert.Equal(0, result.RecommendedCopies);
		Assert.Equal("Protected premium type", result.SafetyLabel);
	}

	[Fact]
	public void UncheckedNormalLegendaryIsRuleSafeWhenItDoesNotCountTowardKeep()
	{
		var settings = UnprotectedSettings();
		settings.NormalCountsTowardKeep = false;
		var entry = Entry(PremiumType.Normal, 1);
		entry.Card.Rarity = CardRarity.Legendary;

		var result = _analyzer.Analyze(
			new[] { entry },
			new Dictionary<int, int>(),
			settings).Single();

		Assert.Equal(0, result.ReservedCopies);
		Assert.Equal(1, result.RecommendedCopies);
		Assert.True(result.IsSafeByRules);
		Assert.Equal("Extra by configured rules", result.SafetyLabel);
	}

	[Fact]
	public void ProtectedCardHasNoRecommendations()
	{
		var settings = UnprotectedSettings();
		settings.ProtectedCardIds.Add("CARD_001");
		var result = _analyzer.Analyze(new[] { Entry(PremiumType.Normal, 8) }, new Dictionary<int, int>(), settings).Single();

		Assert.True(result.IsProtected);
		Assert.Equal(0, result.RecommendedCopies);
	}

	[Fact]
	public void ProtectedExpansionHasNoRecommendations()
	{
		var settings = UnprotectedSettings();
		settings.ProtectedExpansions.Add("CATACLYSM");
		var entry = Entry(PremiumType.Normal, 8);
		entry.Card.Expansion = "CATACLYSM";

		var result = _analyzer.Analyze(new[] { entry }, new Dictionary<int, int>(), settings).Single();

		Assert.True(result.IsExpansionProtected);
		Assert.False(result.IsSafeByRules);
		Assert.Equal(0, result.RecommendedCopies);
		Assert.Equal("Protected expansion", result.SafetyLabel);
	}

	[Fact]
	public void LegacyProtectionAlsoCoversExpert1Alias()
	{
		var settings = UnprotectedSettings();
		settings.ProtectedExpansions.Add("LEGACY");
		var entry = Entry(PremiumType.Normal, 2);
		entry.Card.Expansion = "EXPERT1";
		var result = _analyzer.Analyze(
			new[] { entry },
			new Dictionary<int, int>(),
			settings).Single();

		Assert.True(result.IsExpansionProtected);
		Assert.Equal(0, result.RecommendedCopies);
	}

	[Fact]
	public void CardInPastedDeckHasNoRecommendationsAcrossVariants()
	{
		var results = _analyzer.Analyze(
			new[] { Entry(PremiumType.Normal, 4), Entry(PremiumType.Golden, 2) },
			new Dictionary<int, int> { [42] = 2 },
			UnprotectedSettings(),
			new HashSet<int> { 42 });

		Assert.All(results, result =>
		{
			Assert.True(result.IsInPastedDeck);
			Assert.False(result.IsSafeByRules);
			Assert.Equal(0, result.RecommendedCopies);
			Assert.Equal("Used in pasted deck", result.SafetyLabel);
		});
	}

	[Fact]
	public void DeckUsageIsCappedAtLegalCopyLimit()
	{
		var result = _analyzer.Analyze(
			new[] { Entry(PremiumType.Normal, 4) },
			new Dictionary<int, int> { [42] = 3 },
			UnprotectedSettings()).Single();

		Assert.Equal(2, result.DeckCopyLimit);
		Assert.Equal(2, result.UsedByKnownDecks);
		Assert.Equal(2, result.KeepTarget);
		Assert.Equal(2, result.RecommendedCopies);
	}

	[Fact]
	public void LegendaryDeckLimitIsOne()
	{
		var entry = Entry(PremiumType.Normal, 2);
		entry.Card.Rarity = CardRarity.Legendary;
		var result = _analyzer.Analyze(
			new[] { entry },
			new Dictionary<int, int> { [42] = 5 },
			UnprotectedSettings()).Single();

		Assert.Equal(1, result.DeckCopyLimit);
		Assert.Equal(1, result.UsedByKnownDecks);
		Assert.Equal(1, result.KeepTarget);
		Assert.Equal(1, result.RecommendedCopies);
	}

	[Fact]
	public void UncraftableCardIsNeverRecommended()
	{
		var entry = Entry(PremiumType.Normal, 9);
		entry.Card.IsCraftableByMetadata = false;
		var result = _analyzer.Analyze(new[] { entry }, new Dictionary<int, int>(), UnprotectedSettings()).Single();

		Assert.False(result.IsDisenchantable);
		Assert.Equal(0, result.RecommendedCopies);
	}

	[Fact]
	public void ManualUncraftableOverrideAppliesOnlyToSelectedPremiumVariant()
	{
		var settings = new UserSettings
		{
			ProtectNormalPremium = false,
			ManualUncraftableCards = new List<ManualUncraftableCard>
			{
				new() { CardId = "CARD_001", Premium = PremiumType.Golden }
			}
		};
		var results = _analyzer.Analyze(
			new[] { Entry(PremiumType.Normal, 3), Entry(PremiumType.Golden, 1) },
			new Dictionary<int, int>(),
			settings);

		var normal = results.Single(x => x.Entry.Premium == PremiumType.Normal);
		var golden = results.Single(x => x.Entry.Premium == PremiumType.Golden);
		Assert.True(normal.IsDisenchantable);
		Assert.False(normal.IsManuallyUncraftable);
		Assert.False(golden.IsDisenchantable);
		Assert.True(golden.IsManuallyUncraftable);
		Assert.Equal("Marked uncraftable by you", golden.SafetyLabel);
	}

	[Fact]
	public void AchievementGoldenIsExcludedWithoutHidingCraftableNormalVersion()
	{
		var normal = Entry(PremiumType.Normal, 3);
		var golden = Entry(PremiumType.Golden, 2);
		normal.Card.IsGoldenDisenchantableByMetadata = false;
		golden.Card.IsGoldenDisenchantableByMetadata = false;

		var results = _analyzer.Analyze(
			new[] { normal, golden },
			new Dictionary<int, int>(),
			UnprotectedSettings());

		Assert.True(results.Single(x => x.Entry.Premium == PremiumType.Normal).IsDisenchantable);
		var goldenResult = results.Single(x => x.Entry.Premium == PremiumType.Golden);
		Assert.False(goldenResult.IsDisenchantable);
		Assert.Equal(0, goldenResult.RecommendedCopies);
	}

	[Fact]
	public void CopiesWithinKeepTargetAreNotRuleSafe()
	{
		var result = _analyzer.Analyze(
			new[] { Entry(PremiumType.Normal, 2) },
			new Dictionary<int, int>(),
			UnprotectedSettings()).Single();

		Assert.True(result.IsUnusedByKnownDecks);
		Assert.False(result.IsSafeByRules);
		Assert.Equal("Kept by configured copy rules", result.SafetyLabel);
	}

	[Theory]
	[InlineData(CardRarity.Common, 5, false)]
	[InlineData(CardRarity.Common, 50, true)]
	[InlineData(CardRarity.Rare, 20, false)]
	[InlineData(CardRarity.Rare, 100, true)]
	[InlineData(CardRarity.Epic, 100, false)]
	[InlineData(CardRarity.Epic, 400, true)]
	[InlineData(CardRarity.Legendary, 400, false)]
	[InlineData(CardRarity.Legendary, 1600, true)]
	public void HighestValueFilterUsesRaritySpecificThreshold(CardRarity rarity, int dust, bool expected)
	{
		Assert.Equal(expected, DustValues.IsHighestValueForRarity(rarity, dust));
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

	private static UserSettings UnprotectedSettings() => new()
	{
		ProtectNormalPremium = false,
		ProtectGoldenPremium = false,
		ProtectSignaturePremium = false,
		ProtectDiamondPremium = false
	};
}
