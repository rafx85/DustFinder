using DustFinder.Core;
using System.Linq;
using Xunit;

namespace DustFinder.Core.Tests;

public sealed class CardClassNamesTests
{
	[Theory]
	[InlineData("DEATHKNIGHT", "Death Knight")]
	[InlineData("DEMONHUNTER", "Demon Hunter")]
	[InlineData("DRUID", "Druid")]
	[InlineData("NEUTRAL", "Neutral")]
	public void ConvertsClassCodesToReadableNames(string code, string expected)
	{
		Assert.Equal(expected, CardClassNames.GetDisplayName(code));
	}

	[Fact]
	public void DecodesTerranMultiClassMask()
	{
		var result = CardClassNames.GetClassCodes("INVALID", 656);

		Assert.Equal(new[] { "PALADIN", "SHAMAN", "WARRIOR" }, result);
	}

	[Fact]
	public void DecodesCataclysmMultiClassMaskIncludingNewClasses()
	{
		var result = CardClassNames.GetClassCodes("INVALID", 9153);

		Assert.Equal(
			new[] { "DEATHKNIGHT", "ROGUE", "SHAMAN", "WARLOCK", "WARRIOR", "DEMONHUNTER" },
			result);
	}

	[Fact]
	public void InvalidWithoutMultiClassMetadataBecomesUnknown()
	{
		Assert.Equal("UNKNOWN", CardClassNames.GetClassCodes("INVALID", 0).Single());
	}
}
