# Fast.UndoRedo Examples

## Basic Undo/Redo
```csharp
using Fast.UndoRedo.Core;

public class Person : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    private string _name;
    public string Name
    {
        get => _name;
        set { _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); }
    }
}

var service = new UndoRedoService();
var person = new Person();
service.Attach(person);
person.Name = "New Name";
service.Undo(); // Reverts the change
service.Redo(); // Reapplies the change
```

## Collection Tracking
```csharp
using Fast.UndoRedo.Core;
using System.Collections.ObjectModel;

var service = new UndoRedoService();
var myCollection = new ObservableCollection<string>();
service.AttachCollection(myCollection);
myCollection.Add("Item");
service.Undo(); // Removes the item
```

## MVVM Adapter
```csharp
using Fast.UndoRedo.Core;
using Fast.UndoRedo.Mvvm;

var service = new UndoRedoService();
var mvvm = new MvvmAdapter(service);
mvvm.Register(myViewModel);
```

## ReactiveUI Adapter
```csharp
using Fast.UndoRedo.Core;
using Fast.UndoRedo.ReactiveUI;

var service = new UndoRedoService();
var reactive = new ReactiveAdapter(service);
reactive.Register(myReactiveObject);
```

## Custom Logger
```csharp
using Fast.UndoRedo.Core.Logging;
using Fast.UndoRedo.Core;

public class MyLogger : ICoreLogger
{
    public void Log(string message) => Console.WriteLine(message);
    public void LogException(Exception ex) => Console.WriteLine(ex);
}

var service = new UndoRedoService(new MyLogger());
```

## Undo/Redo with Nested Objects
```csharp
public class Parent : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    public Child Child { get; } = new Child();
}

public class Child : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    private int _value;
    public int Value
    {
        get => _value;
        set { _value = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value))); }
    }
}

var service = new UndoRedoService();
var parent = new Parent();
service.Attach(parent);
parent.Child.Value = 42;
service.Undo(); // Reverts child value
```

## Advanced: Extending ActionFactory
```csharp
using Fast.UndoRedo.Core;

// You can extend ActionFactory to create custom undoable actions
public class MyCustomAction : IUndoableAction
{
    public string Description { get; }
    public void Undo() { /* ... */ }
    public void Redo() { /* ... */ }
}

// Register your custom action with the service
var service = new UndoRedoService();
service.Push(new MyCustomAction());
