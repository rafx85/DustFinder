using DustFinder.Core;
using Xunit;

namespace DustFinder.Core.Tests;

public sealed class CardCraftabilityRulesTests
{
	[Fact]
	public void NewCoreSetCardIsNotCraftable()
	{
		Assert.False(CardCraftabilityRules.IsPotentiallyCraftable(
			true,
			CardRarity.Rare,
			"PLACEHOLDER_202204",
			"CORE_CS2_089"));
	}

	[Fact]
	public void CoreCardIdIsNotCraftableEvenIfSetMetadataDrifts()
	{
		Assert.False(CardCraftabilityRules.IsPotentiallyCraftable(
			true,
			CardRarity.Rare,
			"UNKNOWN_FUTURE_CORE_SET",
			"CORE_CS2_089"));
	}

	[Theory]
	[InlineData("CORE")]
	[InlineData("PLACEHOLDER_202204")]
	public void EveryKnownCoreSetIdentifierIsExcluded(string expansionCode)
	{
		Assert.False(CardCraftabilityRules.IsPotentiallyCraftable(
			true,
			CardRarity.Legendary,
			expansionCode,
			"SOME_CARD"));
	}

	[Fact]
	public void ClassicMirrorIsExcludedToAvoidDuplicatingLegacyCopies()
	{
		Assert.False(CardCraftabilityRules.IsPotentiallyCraftable(
			true,
			CardRarity.Epic,
			"VANILLA",
			"VAN_EX1_295"));
	}

	[Fact]
	public void HeroSkinIsNotTreatedAsCraftableCard()
	{
		Assert.True(CardCraftabilityRules.IsCosmeticOnlySet("HERO_SKINS"));
		Assert.False(CardCraftabilityRules.IsPotentiallyCraftable(
			true,
			CardRarity.Epic,
			"HERO_SKINS",
			"HERO_01a"));
	}

	[Fact]
	public void RegularCollectibleCardRemainsPotentiallyCraftable()
	{
		Assert.True(CardCraftabilityRules.IsPotentiallyCraftable(
			true,
			CardRarity.Rare,
			"TIME_TRAVEL",
			"TIME_039"));
	}

	[Theory]
	[InlineData("TIME_EVENT_998")]
	[InlineData("CATA_EVENT_110")]
	public void SpecialEventRewardCardIsNotCraftable(string cardId)
	{
		Assert.False(CardCraftabilityRules.IsPotentiallyCraftable(
			true,
			CardRarity.Legendary,
			"EVENT",
			cardId));
	}

	[Fact]
	public void EventCardIdIsExcludedEvenIfSetMetadataDrifts()
	{
		Assert.False(CardCraftabilityRules.IsPotentiallyCraftable(
			true,
			CardRarity.Epic,
			"UNKNOWN_FUTURE_EVENT_SET",
			"CATA_EVENT_400"));
	}

	[Fact]
	public void HarthStonebrewGiftIsNotCraftableEvenThoughItUsesTheLegacySet()
	{
		Assert.False(CardCraftabilityRules.IsPotentiallyCraftable(
			true,
			CardRarity.Legendary,
			"Legacy",
			"GIFT_01"));
	}

	[Theory]
	[InlineData("Unlocked with \"Location, Location, Location!\" Achievement.")]
	[InlineData("Earnable on the Murder at Castle Nathria Reward Track.")]
	[InlineData("Earnable after purchasing the Murder at Castle Nathria Tavern Pass.")]
	public void SpecialGoldenRewardIsNotDisenchantable(string howToEarnGolden)
	{
		Assert.False(CardCraftabilityRules.IsPotentiallyDisenchantableGolden(true, howToEarnGolden));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("Obtained by crafting.")]
	[InlineData("Crafting unlocked in Uldaman, in the League of Explorers adventure.")]
	[InlineData("Not available in packs, must be crafted.")]
	[InlineData("Can be crafted after starting the League of Explorers adventure.")]
	public void OrdinaryOrExplicitlyCraftableGoldenRemainsDisenchantable(string? howToEarnGolden)
	{
		Assert.True(CardCraftabilityRules.IsPotentiallyDisenchantableGolden(true, howToEarnGolden));
	}

	[Fact]
	public void UncraftableBaseCardHasNoDisenchantableGoldenVersion()
	{
		Assert.False(CardCraftabilityRules.IsPotentiallyDisenchantableGolden(false, null));
	}
}
