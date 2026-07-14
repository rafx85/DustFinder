using System.Collections.Generic;
using DustFinder.Core;
using Xunit;

namespace DustFinder.Core.Tests;

public sealed class PastedDeckUsageTests
{
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
