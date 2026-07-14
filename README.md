# DustFinder

DustFinder is a separate, recommendation-only Hearthstone Deck Tracker plugin for collection and dust planning. It does not modify or depend on PackTracker and it never clicks Hearthstone or disenchants cards.

## Current features

- Reads the signed-in collection through HDT's current `CollectionHelpers.Hearthstone` / HearthMirror path.
- Keeps accounts and regions separate and stores inferred collection snapshots per account.
- Handles normal, golden, Diamond, Signature, and temporary/trial counts conservatively.
- Searches name, expansion, mechanic, tribe/race, type, class, card ID, and card text.
- Combines expansion, rarity, class, format, premium, and ownership/safety filters.
- Imports pasted Hearthstone deck codes and excludes every card in those decks from dust recommendations across all premium variants. Saved HDT decks are intentionally ignored.
- Supports variant-specific manual uncraftable overrides with a review tab and clipboard-ready JSON export for correcting recognition rules.
- Distinguishes configured extras, protected cards, and cards that cannot be recommended.
- Uses bounded optimization for exact or closest dust combinations, with manual add/remove controls.
- Uses atomic settings writes, backups, corruption recovery, and explicit confirmation for destructive/protection-rule actions.

The snapshot history is inferred from collection reads. It is not a guaranteed record of pack openings or disenchant actions.

## Requirements

- Windows and Hearthstone Deck Tracker 1.53.8 or compatible newer `net472` build.
- .NET 8 SDK for development.
- Hearthstone Deck Tracker itself continues to run on .NET Framework 4.7.2 x64.

The repository does not contain HDT executables. Build references are resolved from a local HDT install or a pinned official release package. See [the research notes](docs/hdt-research.md).

## Build and test

From PowerShell:

```powershell
.\scripts\build.ps1
```

The script discovers the newest `%LOCALAPPDATA%\HearthstoneDeckTracker\app-*` installation. To use another installation:

```powershell
.\scripts\build.ps1 -HdtInstallDir 'C:\path\to\HDT\app-1.53.8'
```

Core tests can also run independently:

```powershell
dotnet test .\tests\DustFinder.Core.Tests\DustFinder.Core.Tests.csproj
```

## Create a release ZIP

```powershell
.\scripts\package.ps1 -Version 0.1.4
```

The version is stamped into the plugin DLL and shown beside the DustFinder name in the plugin window. The result is `dist\DustFinder-0.1.4.zip` with this HDT-compatible layout:

```text
DustFinder/
  DustFinder.Plugin.dll
  DustFinder.Core.dll
```

No HDT, HearthMirror, HearthDb, MahApps, or Newtonsoft binaries are bundled.

## Install in HDT

1. In HDT, open `Options > Tracker > Plugins`.
2. Drag `DustFinder-0.1.4.zip` into the plugin list, or extract its `DustFinder` folder under `%APPDATA%\HearthstoneDeckTracker\Plugins`.
3. Restart HDT if requested and enable DustFinder.
4. Click `Open DustFinder` or use the `Plugins > DustFinder` menu.
5. Start Hearthstone, sign in, open My Collection, and click `Refresh collection`.

Plugin-owned data is stored under `%APPDATA%\HearthstoneDeckTracker\DustFinder`.

## Development layout

```text
src/DustFinder.Core/       HDT-independent rules, optimization, history, storage
src/DustFinder.Plugin/     HDT adapter, MVVM view models, WPF window, IPlugin entry
tests/DustFinder.Core.Tests/
docs/hdt-research.md
scripts/                   build, official-HDT resolver, packaging, verification
```

## Safety model

- Normal variants receive normal disenchant values; disenchantable Golden, Diamond, and Signature variants receive Golden values.
- Free, Core, Special Events, temporary, unknown, non-collectible, and conservatively classified grant-only cards are excluded.
- Only pasted decks provide automatic deck protection; saved HDT decks are ignored.
- The planner only uses copies classified as extra by the active keep rules.
- Protection applies to every premium type of a card and requires confirmation to change.
- Cards in pasted decks remain excluded until the corresponding pasted decks are removed.
- Temporary full-refund windows are deliberately not assumed.

## CI

GitHub Actions runs on Windows, downloads the pinned official HDT `v1.53.8` release package without committing it, builds the plugin, runs tests, creates the release ZIP, and uploads it as an artifact.
