using System.Collections.Generic;
using DustFinder.Core;
using Xunit;

namespace DustFinder.Core.Tests;

public sealed class PastedDeckUsageTests
{
	[Fact]
	public void SameCardsIgnoreDeckNameCodeHeroAndInsertionOrder()
	{
		var first = Deck((10, 2), (20, 1));
		first.Name = "Deck one";
		first.DeckCode = "first";
		first.HeroDbfId = 7;
		var second = Deck((20, 1), (10, 2));
		second.Name = "Completely different name";
		second.DeckCode = "second";
		second.HeroDbfId = 99;

		Assert.True(PastedDeckUsage.HaveSameCards(first, second));
	}

	[Fact]
	public void DifferentCopyCountsAreNotTheSameDeck()
	{
		Assert.False(PastedDeckUsage.HaveSameCards(
			Deck((10, 2), (20, 1)),
			Deck((10, 1), (20, 1))));
	}

	[Fact]
	public void SameNameGetsNextAvailableNumber()
	{
		var decks = new[]
		{
			new PastedDeckDefinition { Name = "Herald Warrior" },
			new PastedDeckDefinition { Name = "Herald Warrior (2)" }
		};

		Assert.Equal("Herald Warrior (3)", PastedDeckUsage.GetUniqueName("Herald Warrior", decks));
		Assert.Equal("Different deck", PastedDeckUsage.GetUniqueName("Different deck", decks));
	}

	[Fact]
	public void MergeMaximumCopiesUsesHighestRequirementAcrossHdtAndPastedDecks()
	{
		var hdt = new Dictionary<int, int> { [10] = 1, [20] = 2 };
		var decks = new[]
		{
			Deck((10, 2), (30, 1)),
			Deck((10, 1), (30, 2))
		};

		var result = PastedDeckUsage.MergeMaximumCopies(hdt, decks);

		Assert.Equal(2, result[10]);
		Assert.Equal(2, result[20]);
		Assert.Equal(2, result[30]);
	}

	[Fact]
	public void ProtectedIdsAreUnionOfEveryPastedDeck()
	{
		var result = PastedDeckUsage.GetProtectedCardDbfIds(new[]
		{
			Deck((10, 2), (20, 1)),
			Deck((20, 2), (30, 1))
		});

		Assert.Equal(new HashSet<int> { 10, 20, 30 }, result);
	}

	private static PastedDeckDefinition Deck(params (int DbfId, int Copies)[] cards)
	{
		var deck = new PastedDeckDefinition();
		foreach(var card in cards)
			deck.CardDbfIds[card.DbfId] = card.Copies;
		return deck;
	}
}
