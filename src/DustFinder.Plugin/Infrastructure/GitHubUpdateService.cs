using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using DustFinder.Core;

namespace DustFinder.Plugin.Infrastructure;

public sealed class GitHubUpdateService
{
	private const string LatestReleaseApi = "https://api.github.com/repos/rafx85/DustFinder/releases/latest";
	private static readonly HttpClient Client = CreateClient();

	public async Task<PluginUpdateCheckResult> CheckAsync(Version installedVersion)
	{
		if(installedVersion == null)
			throw new ArgumentNullException(nameof(installedVersion));

		using var response = await Client.GetAsync(LatestReleaseApi).ConfigureAwait(false);
		if(response.StatusCode == HttpStatusCode.NotFound)
			return PluginUpdateCheckResult.NoPublishedRelease();
		response.EnsureSuccessStatusCode();
		using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
		var serializer = new DataContractJsonSerializer(typeof(GitHubRelease));
		var release = serializer.ReadObject(stream) as GitHubRelease
			?? throw new InvalidOperationException("GitHub returned an unreadable release response.");
		if(release.Draft || release.Prerelease || !PluginUpdateRules.TryParseReleaseVersion(release.TagName, out var releaseVersion))
			return PluginUpdateCheckResult.NoPublishedRelease();

		var assetName = PluginUpdateRules.GetAssetName(releaseVersion);
		var asset = release.Assets.FirstOrDefault(x => string.Equals(x.Name, assetName, StringComparison.OrdinalIgnoreCase));
		var downloadUrl = asset?.DownloadUrl;
		if(string.IsNullOrWhiteSpace(downloadUrl))
			downloadUrl = release.PageUrl;
		return new PluginUpdateCheckResult(releaseVersion, release.PageUrl, downloadUrl ?? string.Empty);
	}

	private static HttpClient CreateClient()
	{
		var client = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
		client.DefaultRequestHeaders.UserAgent.ParseAdd("DustFinder-HDT-Plugin");
		client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
		return client;
	}

	[DataContract]
	private sealed class GitHubRelease
	{
		[DataMember(Name = "tag_name")] public string TagName { get; set; } = string.Empty;
		[DataMember(Name = "html_url")] public string PageUrl { get; set; } = string.Empty;
		[DataMember(Name = "draft")] public bool Draft { get; set; }
		[DataMember(Name = "prerelease")] public bool Prerelease { get; set; }
		[DataMember(Name = "assets")] public List<GitHubAsset> Assets { get; set; } = new();
	}

	[DataContract]
	private sealed class GitHubAsset
	{
		[DataMember(Name = "name")] public string Name { get; set; } = string.Empty;
		[DataMember(Name = "browser_download_url")] public string DownloadUrl { get; set; } = string.Empty;
	}
}

public sealed class PluginUpdateCheckResult
{
	public PluginUpdateCheckResult(Version? latestVersion, string releasePageUrl, string downloadUrl)
	{
		LatestVersion = latestVersion;
		ReleasePageUrl = releasePageUrl ?? string.Empty;
		DownloadUrl = downloadUrl ?? string.Empty;
	}

	public Version? LatestVersion { get; }
	public string ReleasePageUrl { get; }
	public string DownloadUrl { get; }
	public bool HasPublishedRelease => LatestVersion != null;

	public static PluginUpdateCheckResult NoPublishedRelease() => new(null, string.Empty, string.Empty);
}
