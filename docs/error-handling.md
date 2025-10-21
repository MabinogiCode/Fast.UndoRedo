# Error Handling in Fast.UndoRedo

## Logging
- Implement `ICoreLogger` and pass it to `UndoRedoService` to capture errors and debug information.
- Example:
```csharp
public class MyLogger : ICoreLogger
{
    public void Log(string message) => Console.WriteLine(message);
    public void LogException(Exception ex) => Console.WriteLine(ex);
}
var service = new UndoRedoService(new MyLogger());
```

## UI Feedback
- In UI applications (WPF, WinForms, etc.), catch exceptions from Undo/Redo and display a message to the user.
- Example (WPF):
```csharp
try
{
    service.Undo();
}
catch (Exception ex)
{
    MessageBox.Show($"Undo failed: {ex.Message}");
}
```

## Best Practices
- Always unregister objects when no longer needed to avoid memory leaks.
- Use logging to monitor unexpected errors during undo/redo operations.
- Validate model state after undo/redo if your business logic requires it.
