using System;

namespace DustFinder.Core;

public static class PluginUpdateRules
{
	public static bool TryParseReleaseVersion(string? tagName, out Version version)
	{
		version = new Version(0, 0, 0, 0);
		var value = (tagName ?? string.Empty).Trim();
		if(value.StartsWith("v", StringComparison.OrdinalIgnoreCase))
			value = value.Substring(1);
		if(!Version.TryParse(value, out var parsed) || parsed == null)
			return false;
		version = new Version(
			parsed.Major,
			parsed.Minor,
			Math.Max(parsed.Build, 0),
			Math.Max(parsed.Revision, 0));
		return true;
	}

	public static bool IsNewer(string? tagName, Version installedVersion) =>
		installedVersion != null
		&& TryParseReleaseVersion(tagName, out var releaseVersion)
		&& releaseVersion.CompareTo(installedVersion) > 0;

	public static string GetAssetName(Version version)
	{
		if(version == null)
			throw new ArgumentNullException(nameof(version));
		var display = version.Revision > 0
			? $"{version.Major}.{version.Minor}.{Math.Max(version.Build, 0)}.{version.Revision}"
			: $"{version.Major}.{version.Minor}.{Math.Max(version.Build, 0)}";
		return $"DustFinder-{display}.zip";
	}
}
