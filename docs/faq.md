# Fast.UndoRedo FAQ

## How do I track changes in my ViewModel?
Attach your object to the UndoRedoService:
```csharp
service.Attach(myViewModel);
```

## How do I track changes in collections?
Attach your collection:
```csharp
service.AttachCollection(myCollection);
```

## How do I undo/redo changes?
Call:
```csharp
service.Undo();
service.Redo();
```

## How do I use with MVVM or ReactiveUI?
Use the provided adapters:
```csharp
var mvvm = new MvvmAdapter(service);
mvvm.Register(myViewModel);

var reactive = new ReactiveAdapter(service);
reactive.Register(myReactiveObject);
```

## How do I add logging?
Implement ICoreLogger and pass it to UndoRedoService.

## Is it thread-safe?
Yes, registration and undo/redo operations are protected against concurrent access.

## How do I publish to NuGet?
Push a tag `vX.Y.Z` to GitHub with `NUGET_API_KEY` configured in secrets.

## How do I get code coverage?
Configure `CODECOV_TOKEN` in GitHub secrets; coverage is uploaded automatically by CI.
