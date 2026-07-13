using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace DustFinder.Core;

public sealed class AtomicJsonStore<T> where T : class
{
	private readonly DataContractJsonSerializer _serializer = new(typeof(T));

	public void Save(string path, T value)
	{
		if(string.IsNullOrWhiteSpace(path))
			throw new ArgumentException("A path is required.", nameof(path));
		if(value == null)
			throw new ArgumentNullException(nameof(value));

		var fullPath = Path.GetFullPath(path);
		var directory = Path.GetDirectoryName(fullPath) ?? throw new InvalidOperationException("The path has no directory.");
		Directory.CreateDirectory(directory);
		var temporary = fullPath + ".tmp-" + Guid.NewGuid().ToString("N");
		var backup = fullPath + ".bak";

		try
		{
			using(var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
			{
				_serializer.WriteObject(stream, value);
				stream.Flush(true);
			}

			if(File.Exists(fullPath))
			{
				try
				{
					File.Replace(temporary, fullPath, backup, true);
				}
				catch(PlatformNotSupportedException)
				{
					File.Copy(fullPath, backup, true);
					File.Delete(fullPath);
					File.Move(temporary, fullPath);
				}
			}
			else
			{
				File.Move(temporary, fullPath);
			}
		}
		finally
		{
			if(File.Exists(temporary))
				File.Delete(temporary);
		}
	}

	public T LoadOrRecover(string path, Func<T> createDefault)
	{
		if(createDefault == null)
			throw new ArgumentNullException(nameof(createDefault));
		var fullPath = Path.GetFullPath(path);
		if(TryLoad(fullPath, out var value))
			return value!;

		var backup = fullPath + ".bak";
		if(TryLoad(backup, out value))
		{
			Quarantine(fullPath);
			Save(fullPath, value!);
			return value!;
		}

		Quarantine(fullPath);
		return createDefault();
	}

	public bool TryLoad(string path, out T? value)
	{
		value = null;
		if(!File.Exists(path))
			return false;
		try
		{
			using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			value = _serializer.ReadObject(stream) as T;
			return value != null;
		}
		catch(SerializationException)
		{
			return false;
		}
		catch(IOException)
		{
			return false;
		}
	}

	private static void Quarantine(string path)
	{
		if(!File.Exists(path))
			return;
		var corruptPath = path + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
		File.Move(path, corruptPath);
	}
}
