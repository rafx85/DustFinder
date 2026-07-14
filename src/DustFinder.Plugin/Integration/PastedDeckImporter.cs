using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DustFinder.Core;
using HearthDb;
using HearthDb.Deckstrings;

namespace DustFinder.Plugin.Integration;

public static class PastedDeckImporter
{
	public static PastedDeckDefinition Import(string pastedText, int sequenceNumber)
	{
		if(string.IsNullOrWhiteSpace(pastedText))
			throw new ArgumentException("Paste a Hearthstone deck code or a complete copied deck first.", nameof(pastedText));

		var decoded = TryDecode(pastedText, out var code)
			? DeckSerializer.Deserialize(code)
			: throw new ArgumentException("No valid Hearthstone deck code was found in the pasted text.", nameof(pastedText));
		var cards = new Dictionary<int, int>();
		AddCards(cards, decoded.CardDbfIds);
		foreach(var sideboard in decoded.Sideboards?.Values ?? Enumerable.Empty<Dictionary<int, int>>())
			AddCards(cards, sideboard);

		var name = ExtractName(pastedText);
		if(string.IsNullOrWhiteSpace(name))
		{
			var heroName = Cards.AllByDbfId.TryGetValue(decoded.HeroDbfId, out var hero) ? hero.Name : null;
			name = string.IsNullOrWhiteSpace(heroName) ? $"Pasted deck {sequenceNumber}" : $"{heroName} deck {sequenceNumber}";
		}

		return new PastedDeckDefinition
		{
			Name = name,
			DeckCode = DeckSerializer.Serialize(decoded, false),
			Format = FormatName(decoded.Format.ToString()),
			HeroDbfId = decoded.HeroDbfId,
			CardDbfIds = cards,
			ImportedAtUtc = DateTime.UtcNow
		};
	}

	private static bool TryDecode(string text, out string code)
	{
		foreach(var candidate in GetCandidates(text))
		{
			try
			{
				DeckSerializer.Deserialize(candidate);
				code = candidate;
				return true;
			}
			catch(Exception)
			{
				// Continue because copied deck exports contain headings and card-list lines around the code.
			}
		}
		code = string.Empty;
		return false;
	}

	private static IEnumerable<string> GetCandidates(string text)
	{
		var trimmed = text.Trim();
		if(trimmed.Length > 0)
			yield return trimmed;
		foreach(var line in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
		{
			var candidate = line.Trim();
			if(candidate.Length >= 20 && candidate.All(IsBase64Character))
				yield return candidate;
		}
	}

	private static bool IsBase64Character(char value) =>
		char.IsLetterOrDigit(value) || value is '+' or '/' or '=';

	private static string ExtractName(string text)
	{
		var line = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
			.Select(x => x.Trim())
			.FirstOrDefault(x => x.StartsWith("### ", StringComparison.Ordinal));
		return line?.Substring(4).Trim() ?? string.Empty;
	}

	private static string FormatName(string value)
	{
		var normalized = value.StartsWith("FT_", StringComparison.OrdinalIgnoreCase) ? value.Substring(3) : value;
		return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(normalized.Replace('_', ' ').ToLowerInvariant());
	}

	private static void AddCards(Dictionary<int, int> target, IReadOnlyDictionary<int, int>? source)
	{
		if(source == null)
			return;
		foreach(var pair in source.Where(x => x.Key > 0 && x.Value > 0))
			target[pair.Key] = target.TryGetValue(pair.Key, out var current) ? current + pair.Value : pair.Value;
	}
}
