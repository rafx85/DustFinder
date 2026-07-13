using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DustFinder.Core;

public enum PremiumType
{
	Normal = 0,
	Golden = 1,
	Diamond = 2,
	Signature = 3,
	Unknown = 99
}

public enum CardRarity
{
	Unknown = 0,
	Common = 1,
	Free = 2,
	Rare = 3,
	Epic = 4,
	Legendary = 5
}

[DataContract]
public sealed class CardMetadata
{
	[DataMember(Order = 1)] public int DbfId { get; set; }
	[DataMember(Order = 2)] public string CardId { get; set; } = string.Empty;
	[DataMember(Order = 3)] public string Name { get; set; } = string.Empty;
	[DataMember(Order = 4)] public string Expansion { get; set; } = string.Empty;
	[DataMember(Order = 5)] public string CardClass { get; set; } = string.Empty;
	[DataMember(Order = 6)] public string CardType { get; set; } = string.Empty;
	[DataMember(Order = 7)] public string Race { get; set; } = string.Empty;
	[DataMember(Order = 8)] public string Mechanics { get; set; } = string.Empty;
	[DataMember(Order = 9)] public string Text { get; set; } = string.Empty;
	[DataMember(Order = 10)] public string Format { get; set; } = string.Empty;
	[DataMember(Order = 11)] public CardRarity Rarity { get; set; }
	[DataMember(Order = 12)] public bool IsCollectible { get; set; }
	[DataMember(Order = 13)] public bool IsCraftableByMetadata { get; set; }
}

[DataContract]
public sealed class CollectionEntry
{
	[DataMember(Order = 1)] public CardMetadata Card { get; set; } = new();
	[DataMember(Order = 2)] public PremiumType Premium { get; set; }
	[DataMember(Order = 3)] public int Count { get; set; }
	[DataMember(Order = 4)] public int TrialCount { get; set; }

	public string Key => $"{Card.DbfId}:{(int)Premium}";
}

[DataContract]
public sealed class AccountIdentity
{
	[DataMember(Order = 1)] public ulong AccountHi { get; set; }
	[DataMember(Order = 2)] public ulong AccountLo { get; set; }
	[DataMember(Order = 3)] public string Region { get; set; } = "UNKNOWN";
	[DataMember(Order = 4)] public string BattleTag { get; set; } = string.Empty;

	public string StorageKey => $"{Region}-{AccountHi:x16}-{AccountLo:x16}";
}

[DataContract]
public sealed class CollectionSnapshot
{
	[DataMember(Order = 1)] public int SchemaVersion { get; set; } = 1;
	[DataMember(Order = 2)] public DateTime CapturedAtUtc { get; set; }
	[DataMember(Order = 3)] public AccountIdentity Account { get; set; } = new();
	[DataMember(Order = 4)] public List<CollectionEntry> Entries { get; set; } = new();
}

[DataContract]
public sealed class UserSettings
{
	[DataMember(Order = 1)] public int SchemaVersion { get; set; } = 1;
	[DataMember(Order = 2)] public int KeepNonLegendary { get; set; } = 2;
	[DataMember(Order = 3)] public int KeepLegendary { get; set; } = 1;
	[DataMember(Order = 4)] public bool NormalCountsTowardKeep { get; set; } = true;
	[DataMember(Order = 5)] public bool GoldenCountsTowardKeep { get; set; }
	[DataMember(Order = 6)] public bool DiamondCountsTowardKeep { get; set; }
	[DataMember(Order = 7)] public bool SignatureCountsTowardKeep { get; set; }
	[DataMember(Order = 8)] public HashSet<string> ProtectedCardIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);

	public bool CountsTowardKeep(PremiumType premium) => premium switch
	{
		PremiumType.Normal => NormalCountsTowardKeep,
		PremiumType.Golden => GoldenCountsTowardKeep,
		PremiumType.Diamond => DiamondCountsTowardKeep,
		PremiumType.Signature => SignatureCountsTowardKeep,
		_ => false
	};
}

public sealed class AnalysisResult
{
	public CollectionEntry Entry { get; set; } = new();
	public int UsedByKnownDecks { get; set; }
	public int KeepTarget { get; set; }
	public int ReservedCopies { get; set; }
	public int RecommendedCopies { get; set; }
	public int DustPerCopy { get; set; }
	public bool IsProtected { get; set; }
	public bool IsDisenchantable { get; set; }
	public bool IsSafeByRules => IsDisenchantable && !IsProtected && RecommendedCopies > 0;
	public bool IsUnusedByKnownDecks => UsedByKnownDecks == 0;

	public string SafetyLabel
	{
		get
		{
			if(IsProtected)
				return "Protected by you";
			if(!IsDisenchantable)
				return "Cannot be disenchanted";
			if(RecommendedCopies > 0)
				return "Extra by configured rules";
			if(IsUnusedByKnownDecks)
				return "Unused (not automatically safe)";
			return "Kept for copies/decks";
		}
	}
}

public sealed class SnapshotDifference
{
	public int DbfId { get; set; }
	public string CardId { get; set; } = string.Empty;
	public string CardName { get; set; } = string.Empty;
	public PremiumType Premium { get; set; }
	public int Before { get; set; }
	public int After { get; set; }
	public int Delta => After - Before;
}

public sealed class PlanCandidate
{
	public string Key { get; set; } = string.Empty;
	public string CardName { get; set; } = string.Empty;
	public PremiumType Premium { get; set; }
	public int AvailableCopies { get; set; }
	public int DustPerCopy { get; set; }
}

public sealed class PlanSelection
{
	public string Key { get; set; } = string.Empty;
	public string CardName { get; set; } = string.Empty;
	public PremiumType Premium { get; set; }
	public int Copies { get; set; }
	public int DustPerCopy { get; set; }
	public int TotalDust => Copies * DustPerCopy;
}

public sealed class DustPlan
{
	public int TargetDust { get; set; }
	public int TotalDust { get; set; }
	public int RemainingDust => Math.Max(0, TargetDust - TotalDust);
	public int OvershootDust => Math.Max(0, TotalDust - TargetDust);
	public List<PlanSelection> Selections { get; set; } = new();
}
