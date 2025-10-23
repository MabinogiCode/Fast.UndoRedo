# Fast.UndoRedo

[![Build Status](https://github.com/MabinogiCode/FastUndoRedo/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/MabinogiCode/FastUndoRedo/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/MabinogiCode/FastUndoRedo/branch/master/graph/badge.svg)](https://app.codecov.io/gh/MabinogiCode/FastUndoRedo)

Fast, non-intrusive undo/redo library for .NET applications. Designed to attach to existing objects and collections (`INotifyPropertyChanged`, `INotifyCollectionChanged`) and record undo/redo actions without forcing you to replace your models.

Features
- Attach to existing objects and collections via `UndoRedoService.Attach` / `AttachCollection`.
- Property change actions and collection change actions recorded as `IUndoableAction`.
- Extensible factories to avoid heavy runtime reflection and `Activator.CreateInstance` usage.
- Optional injectable `ICoreLogger` for production logging.

Getting started
1. Create an instance of `UndoRedoService` and pass it around (DI or singleton):

```csharp
var service = new UndoRedoService();
```

2. Attach an object to record property changes:

```csharp
service.Attach(myViewModel);
```

3. Attach an existing collection (e.g. `ObservableCollection<T>` or `DynamicData` `ExtendedObservableCollection<T>`):

```csharp
service.AttachCollection(myCollection);
```

Notes about collections
- The library provides `AttachCollection`/`CollectionSubscription` which observes `INotifyCollectionChanged` events and records changes.
- For collection frameworks that provide range operations (e.g. DynamicData), the `CollectionSubscription` attempts to detect multi-item changes and will record range operations when possible.

Logging
- Implement `ICoreLogger` and pass to `UndoRedoService` ctor to capture logs in production instead of debug output.

CI / Packaging
- GitHub Actions workflow available at `.github/workflows/ci.yml`:
  - Builds the solution, runs tests and collects coverage.
  - Uploads coverage to [Codecov](https://app.codecov.io/gh/MabinogiCode/Fast.UndoRedo) (requires `CODECOV_TOKEN` secret).
  - On tags `vX.Y.Z`, packs a NuGet package and (optionally) signs it and pushes it to NuGet.org if `NUGET_API_KEY` is configured.

Contributing
- Please open issues or PRs.

Roadmap
- Finish ReactiveUI adapter to support DynamicData extended collection operations fully.
- Add more tests and examples.
