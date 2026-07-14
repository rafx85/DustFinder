using System;
using DustFinder.Core;
using Xunit;

namespace DustFinder.Core.Tests;

public sealed class PluginUpdateRulesTests
{
	[Theory]
	[InlineData("v0.1.8", 0, 1, 8)]
	[InlineData(" 1.2.3 ", 1, 2, 3)]
	public void ParsesGitHubReleaseTags(string tag, int major, int minor, int build)
	{
		Assert.True(PluginUpdateRules.TryParseReleaseVersion(tag, out var version));
		Assert.Equal(new Version(major, minor, build, 0), version);
	}

	[Theory]
	[InlineData("")]
	[InlineData("latest")]
	[InlineData("v1")]
	public void RejectsInvalidReleaseTags(string tag)
	{
		Assert.False(PluginUpdateRules.TryParseReleaseVersion(tag, out _));
	}

	[Fact]
	public void DetectsOnlyStrictlyNewerVersions()
	{
		var installed = new Version(0, 1, 8, 0);
		Assert.True(PluginUpdateRules.IsNewer("v0.1.9", installed));
		Assert.False(PluginUpdateRules.IsNewer("v0.1.8", installed));
		Assert.False(PluginUpdateRules.IsNewer("v0.1.7", installed));
	}

	[Fact]
	public void BuildsTheExpectedReleaseAssetName()
	{
		Assert.Equal("DustFinder-0.1.8.zip", PluginUpdateRules.GetAssetName(new Version(0, 1, 8, 0)));
	}
}
