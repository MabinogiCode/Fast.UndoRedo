# Fast.UndoRedo

Fast.UndoRedo is a fast, non-intrusive undo/redo library for .NET applications. Attach to existing objects and collections (`INotifyPropertyChanged`, `INotifyCollectionChanged`) and record undo/redo actions without changing your models.

## Features
- Attach to existing objects and collections via `UndoRedoService.Attach` / `AttachCollection`.
- Records property and collection changes as `IUndoableAction`.
- Extensible factories to avoid heavy runtime reflection.
- Optional injectable `ICoreLogger` for production logging.
- Adapters for MVVM and ReactiveUI.

## Installation

```
Install-Package Fast.UndoRedo.Core
```
Or via .NET CLI:
```
dotnet add package Fast.UndoRedo.Core
```

## Getting Started
```csharp
var service = new UndoRedoService();
service.Attach(myViewModel); // Track property changes
service.AttachCollection(myCollection); // Track collection changes
```

## Documentation
- [GitHub Repository](https://github.com/MabinogiCode/Fast.UndoRedo)
- [Codecov Coverage](https://app.codecov.io/gh/MabinogiCode/Fast.UndoRedo)
- See README.md for more usage examples and roadmap.

## License
MIT
