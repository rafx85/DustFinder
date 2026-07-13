using System.Collections.Generic;
using System.Threading.Tasks;
using DustFinder.Core;

namespace DustFinder.Plugin.Integration;

public interface IHdtCollectionSource
{
	Task<CollectionLoadResult> LoadAsync();
}

public sealed class CollectionLoadResult
{
	public AccountIdentity Account { get; set; } = new();
	public List<CollectionEntry> Entries { get; set; } = new();
	public Dictionary<int, int> MaximumDeckCopies { get; set; } = new();
}

