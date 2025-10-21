# Migration Guide: Fast.UndoRedo

## Migrating from another undo/redo library
- Replace your undo/redo manager with `UndoRedoService`.
- Attach your models and collections using `Attach` and `AttachCollection`.
- Replace custom undoable actions with implementations of `IUndoableAction` if needed.
- Use adapters for MVVM or ReactiveUI integration.

## Upgrading from older Fast.UndoRedo versions
- Ensure you use the latest API (`UndoRedoService`, adapters, etc.).
- Remove any direct references to internal collections or stacks.
- Update your CI to use the new workflow and secrets (`CODECOV_TOKEN`, `NUGET_API_KEY`).
- Review the README and nuget-readme for new features and usage patterns.

## .NET Framework to .NET Standard
- Fast.UndoRedo supports both .NET Standard 2.0 and .NET Framework 4.8.
- For cross-platform projects, target .NET Standard 2.0.

## Questions?
See [FAQ](faq.md) or open an issue on [GitHub](https://github.com/MabinogiCode/Fast.UndoRedo/issues).
