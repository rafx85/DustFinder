using System;
using System.IO;
using System.Linq;
using DustFinder.Core;
using Xunit;

namespace DustFinder.Core.Tests;

public sealed class SnapshotTests
{
	[Fact]
	public void ComparisonShowsAddedAndRemovedCopies()
	{
		var older = Snapshot(Entry("A", 1, PremiumType.Normal), Entry("B", 2, PremiumType.Golden));
		var newer = Snapshot(Entry("A", 3, PremiumType.Normal), Entry("B", 1, PremiumType.Golden));

		var differences = new SnapshotComparer().Compare(older, newer);

		Assert.Equal(2, differences.Single(x => x.CardId == "A").Delta);
		Assert.Equal(-1, differences.Single(x => x.CardId == "B").Delta);
	}

	[Fact]
	public void AtomicStoreRecoversCorruptedPrimaryFromBackup()
	{
		var directory = Path.Combine(Path.GetTempPath(), "DustFinder.Tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		try
		{
			var path = Path.Combine(directory, "settings.json");
			var store = new AtomicJsonStore<UserSettings>();
			store.Save(path, new UserSettings { KeepNonLegendary = 3 });
			store.Save(path, new UserSettings { KeepNonLegendary = 4 });
			File.WriteAllText(path, "{ definitely not valid json");

			var recovered = store.LoadOrRecover(path, () => new UserSettings());

			Assert.Equal(3, recovered.KeepNonLegendary);
			Assert.Contains(Directory.GetFiles(directory), x => x.Contains(".corrupt-", StringComparison.Ordinal));
		}
		finally
		{
			Directory.Delete(directory, true);
		}
	}

	private static CollectionSnapshot Snapshot(params CollectionEntry[] entries) => new()
	{
		CapturedAtUtc = DateTime.UtcNow,
		Account = new AccountIdentity { AccountHi = 1, AccountLo = 2, Region = "EU" },
		Entries = entries.ToList()
	};

	private static CollectionEntry Entry(string id, int count, PremiumType premium) => new()
	{
		Count = count,
		Premium = premium,
		Card = new CardMetadata { DbfId = id[0], CardId = id, Name = id }
	};
}
