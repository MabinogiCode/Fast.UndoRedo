# Fast.UndoRedo API Reference

## Core Types

### UndoRedoService
```csharp
public class UndoRedoService
{
    public void Attach(object obj);
    public void AttachCollection(INotifyCollectionChanged collection);
    public void Detach(object obj);
    public void DetachCollection(INotifyCollectionChanged collection);
    public void Undo();
    public void Redo();
    public void Clear();
    public bool CanUndo { get; }
    public bool CanRedo { get; }
    public IDisposable Subscribe(IObserver<UndoRedoState> observer);
}
```

### IUndoableAction
```csharp
public interface IUndoableAction
{
    string Description { get; }
    void Undo();
    void Redo();
}
```

### CollectionChangeAction<T>
```csharp
public class CollectionChangeAction<T> : IUndoableAction
{
    public CollectionChangeAction(...);
    public void Undo();
    public void Redo();
    public string Description { get; }
}
```

### PropertyChangeAction
```csharp
public class PropertyChangeAction : IUndoableAction
{
    public PropertyChangeAction(...);
    public void Undo();
    public void Redo();
    public string Description { get; }
}
```

### RegistrationTracker
```csharp
public class RegistrationTracker
{
    public void Register(object obj);
    public void Unregister(object obj);
}
```

### ICoreLogger
```csharp
public interface ICoreLogger
{
    void Log(string message);
    void LogException(Exception ex);
}
```

## Adapters

### MvvmAdapter
```csharp
public class MvvmAdapter
{
    public void Register(object vm);
    public void Unregister(object vm);
}
```

### ReactiveAdapter
```csharp
public class ReactiveAdapter
{
    public void Register(object reactiveObject);
    public void Unregister(object reactiveObject);
}
```

## Documentation Links
- [XML API Documentation](../src/Fast.UndoRedo.Core/Documentation.xml) *(if generated)*
- [GitHub Repository](https://github.com/MabinogiCode/Fast.UndoRedo)
- [Examples](../examples/README.md)

## See Also
- [Getting Started](../src/Fast.UndoRedo.Core/nuget-readme.md)
- [FAQ](faq.md)
- [Migration Guide](migration.md)
