using DustFinder.Core;
using Xunit;

namespace DustFinder.Core.Tests;

public sealed class CardSetNamesTests
{
	[Theory]
	[InlineData("OG", "Whispers of the Old Gods")]
	[InlineData("TIME_TRAVEL", "Across the Timeways")]
	[InlineData("ISLAND_VACATION", "Perils in Paradise")]
	[InlineData("PLACEHOLDER_202204", "Core")]
	[InlineData("VANILLA", "Classic")]
	public void GetDisplayName_UsesFriendlySetNames(string setCode, string expected)
	{
		Assert.Equal(expected, CardSetNames.GetDisplayName(setCode));
	}

	[Fact]
	public void GetDisplayName_HumanizesUnknownFutureSetCodes()
	{
		Assert.Equal("Future Set 2027", CardSetNames.GetDisplayName("FUTURE_SET_2027"));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void GetDisplayName_HandlesMissingSetCodes(string? setCode)
	{
		Assert.Equal("Unknown", CardSetNames.GetDisplayName(setCode));
	}
}
