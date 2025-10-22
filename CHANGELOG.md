# Changelog

All notable changes to this project will be documented in this file.

## [v1.1.0] - YYYY-MM-DD
### Added
- Cached reflection helpers to reduce allocations and improve runtime performance.
- Object-based getter/setter wrappers to avoid DynamicInvoke and reduce GC pressure.
- Additional unit tests and edge-case coverage for collection clears, enum-backed collections, and factory behavior.
- Minimal static-analysis check (skips if Roslyn workspace not available) to guard against analyzer regressions in CI.

### Changed
- Prefer `ActionFactory` constructors for collection actions; stable fallback to concrete `CollectionChangeAction<T>` when necessary to ensure correct undo/redo semantics.
- Always capture an initial snapshot for attached collections so Clear actions can restore items reliably.
- Improve conversions for enum/string/int cases when creating collection actions.

### Fixed
- Several integration issues where undo/redo ordering or snapshot content could become inconsistent.
- StyleCop/Analyzer warnings across modified files.

### Notes
- To publish this release create an annotated Git tag `v1.1.0`. The CI workflow will create a GitHub Release using this changelog as the release body and will build and pack the NuGet package.

## [v1.0.0] - YYYY-MM-DD
### Added
- Initial stable release of `Fast.UndoRedo` providing:
  - Core undo/redo service and action types.
  - Property and collection tracking with non-intrusive adapters.
  - ReactiveUI and MVVM adapters (partial).
  - Unit tests and CI pipeline with NuGet packaging on `v*` tags.

### Fixed
- Packaging issues: ensured XML documentation files are included properly and package icon is set.

### Notes
- To publish a release, a Git tag following `vX.Y.Z` will trigger CI to build, pack and publish the NuGet package (if `NUGET_API_KEY` is configured).
- See `README.md` for usage and integration guidance.
