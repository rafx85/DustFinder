# Current HDT integration research

Research date: 2026-07-13. The implementation was checked against HDT `v1.53.8` at commit `b68fcf47a94a0041b5ea9c0fcdd024fdaee3c8fd` and HearthDb `v36.0.0` at commit `c7ae5192775d75933c1debc0c58b3007601894ea`.

## Conclusions

- HDT remains an x64 WPF application targeting .NET Framework 4.7.2 (`net472`). DustFinder therefore targets `net472` and x64 for its HDT-facing assembly. The testable core targets `netstandard2.0`.
- Plugins are public classes implementing `Hearthstone_Deck_Tracker.Plugins.IPlugin`. HDT discovers `.dll` files recursively, loads them with `Assembly.LoadFrom`, and instantiates public non-abstract implementations.
- Current HDT code provides the public `CollectionHelpers.Hearthstone` wrapper. It calls HearthMirror `Reflection.Client.GetFullCollection()` with a 10-second read timeout and caches collections by the account's `(Hi, Lo)` identifiers.
- HDT maps collection records into four premium buckets: normal (`0`), golden (`1`), Diamond (`2`), and Signature (`3`). Four corresponding trial-count buckets are stored separately. DustFinder displays trial copies but never assigns them dust or treats them as owned extras.
- Account region is encoded in bits 32-39 of `AccountHi`, matching HDT's `Helper.GetRegion` implementation. Snapshots are partitioned by region plus both account identifiers.
- HearthDb supplies current card IDs, localized names/text, set, class, rarity, type, races, mechanics, collectibility, and format helpers. It does not expose a definitive live per-copy disenchantability flag. DustFinder therefore uses conservative metadata rules: only collectible normal/golden cards with Common/Rare/Epic/Legendary rarity, excluding Core and known grant-only cards, receive a dust value. Diamond, Signature, Free, Core, trial, unknown, and non-collectible records are always zero.
- Current HDT's drag-and-drop installer accepts DLL or ZIP files. A ZIP is extracted into the plugin directory; plugin discovery is recursive. DustFinder releases contain exactly one top-level `DustFinder/` folder holding `DustFinder.Plugin.dll`, `DustFinder.Core.dll`, and the README. HDT and HearthDb binaries are references only and are never included.

## Primary references

- [HDT project target and dependencies](https://github.com/HearthSim/Hearthstone-Deck-Tracker/blob/b68fcf47a94a0041b5ea9c0fcdd024fdaee3c8fd/Hearthstone%20Deck%20Tracker/Hearthstone%20Deck%20Tracker.csproj)
- [Current `IPlugin` contract](https://github.com/HearthSim/Hearthstone-Deck-Tracker/blob/b68fcf47a94a0041b5ea9c0fcdd024fdaee3c8fd/Hearthstone%20Deck%20Tracker/Plugins/IPlugin.cs)
- [Current plugin discovery and recursive loading](https://github.com/HearthSim/Hearthstone-Deck-Tracker/blob/b68fcf47a94a0041b5ea9c0fcdd024fdaee3c8fd/Hearthstone%20Deck%20Tracker/Plugins/PluginManager.cs)
- [Current DLL/ZIP drag-and-drop installation](https://github.com/HearthSim/Hearthstone-Deck-Tracker/blob/b68fcf47a94a0041b5ea9c0fcdd024fdaee3c8fd/Hearthstone%20Deck%20Tracker/FlyoutControls/Options/Tracker/TrackerPlugins.xaml.cs)
- [Current collection loader backed by HearthMirror](https://github.com/HearthSim/Hearthstone-Deck-Tracker/blob/b68fcf47a94a0041b5ea9c0fcdd024fdaee3c8fd/Hearthstone%20Deck%20Tracker/Hearthstone/CollectionHelper.cs)
- [Current premium and trial bucket mapping](https://github.com/HearthSim/Hearthstone-Deck-Tracker/blob/b68fcf47a94a0041b5ea9c0fcdd024fdaee3c8fd/Hearthstone%20Deck%20Tracker/Hearthstone/Collection.cs)
- [HearthDb card API](https://github.com/HearthSim/HearthDb/blob/c7ae5192775d75933c1debc0c58b3007601894ea/HearthDb/Card.cs)
- [Official plugin creation guidance](https://github.com/HearthSim/Hearthstone-Deck-Tracker/wiki/Creating-Plugins)
- [Official plugin installation guidance](https://github.com/HearthSim/Hearthstone-Deck-Tracker/wiki/Available-Plugins)

## Architecture decisions

1. `DustFinder.Core` has no HDT dependency and owns collection analysis, dust values, bounded optimization, snapshot comparison, and atomic storage.
2. `DustFinder.Plugin` isolates all HDT/HearthDb calls behind `IHdtCollectionSource` and supplies the WPF/MVVM interface.
3. "Used by decks" is the maximum number of copies required by any one saved, non-archived HDT deck (including its sideboard), not the sum across decks. Collection copies are reusable between decks.
4. "Unused" is informational only. Automatic planning uses only copies beyond the configured keep target, and protected cards are excluded.
5. The first release contains no input simulation, mouse automation, or disenchant calls.

## Decisions that do not block implementation

- A public-source license has not been selected. No source was copied from the archived Dust Utility. Choose a license before publishing the repository broadly.
- Temporary full-dust refund windows are not modeled in 0.1.0 because current HDT/HearthDb metadata does not expose an authoritative live refund flag. Standard disenchant values intentionally under-estimate rather than over-promise.

