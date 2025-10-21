Release v1.1.0

Highlights
- Performance improvements: cached reflection helpers and object-based getter/setter wrappers to reduce allocations.
- Robust collection action handling and clearer undo/redo semantics for Clear and range operations.
- Additional unit tests covering enums, cleared-items conversions and edge cases.

How to upgrade
- Install via NuGet: `Install-Package Fast.UndoRedo -Version 1.1.0`

Notes
- Create an annotated git tag `v1.1.0` and push it; CI will create a GitHub Release using `CHANGELOG.md` as the release body and build/package the NuGet artifact.
