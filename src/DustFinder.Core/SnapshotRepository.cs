using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DustFinder.Core;

public sealed class SnapshotRepository
{
	private readonly string _root;
	private readonly AtomicJsonStore<CollectionSnapshot> _store = new();
	private readonly SnapshotComparer _comparer = new();

	public SnapshotRepository(string root)
	{
		_root = Path.GetFullPath(root ?? throw new ArgumentNullException(nameof(root)));
	}

	public IReadOnlyList<CollectionSnapshot> Load(AccountIdentity account)
	{
		var directory = GetDirectory(account);
		if(!Directory.Exists(directory))
			return Array.Empty<CollectionSnapshot>();
		var snapshots = new List<CollectionSnapshot>();
		foreach(var file in Directory.GetFiles(directory, "*.json").OrderBy(x => x, StringComparer.Ordinal))
		{
			if(_store.TryLoad(file, out var snapshot) && snapshot != null)
				snapshots.Add(snapshot);
		}
		return snapshots.OrderBy(x => x.CapturedAtUtc).ToList();
	}

	public bool SaveIfChanged(CollectionSnapshot snapshot)
	{
		if(snapshot == null)
			throw new ArgumentNullException(nameof(snapshot));
		var existing = Load(snapshot.Account).LastOrDefault();
		if(existing != null && _comparer.HasSameCounts(existing, snapshot))
			return false;

		var directory = GetDirectory(snapshot.Account);
		Directory.CreateDirectory(directory);
		var fileName = snapshot.CapturedAtUtc.ToString("yyyyMMddTHHmmssfffffffZ") + ".json";
		_store.Save(Path.Combine(directory, fileName), snapshot);
		return true;
	}

	private string GetDirectory(AccountIdentity account)
	{
		var safeKey = new string(account.StorageKey.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch).ToArray());
		return Path.Combine(_root, safeKey);
	}
}
