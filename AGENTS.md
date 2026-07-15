# DustFinder repository guidance

## Verification

- Run `.\scripts\verify.ps1` after code, build, packaging, UI, or workflow changes.
- The command builds Release, runs all tests, packages the plugin, verifies the HDT plugin contract, and checks the DLL and ZIP versions.
- Core-only changes may use `dotnet test .\tests\DustFinder.Core.Tests\DustFinder.Core.Tests.csproj` during iteration, but complete work with the full verification command.

## Versions and releases

- `src\DustFinder.Plugin\DustFinder.Plugin.csproj` is the only release-version source.
- Do not add current-version literals to scripts, tests, or documentation.
- Pushes to `main` and pull requests verify only. A matching `v<version>` tag publishes the release.
- Do not create or push a release tag unless the user explicitly requests publication.

## Dependencies and generated files

- Do not commit HDT executables or other third-party binaries.
- CI resolves the pinned official HDT `v1.53.8` package; local verification may use the installed HDT copy.
- Keep `bin`, `obj`, `artifacts`, `dist`, `.hdt`, `.dotnet-home`, and `.nuget` generated and untracked.

## Git safety

- Preserve unrelated local changes.
- Do not commit, tag, push, or publish a release unless the user explicitly asks.
