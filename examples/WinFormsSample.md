# WinForms Example: Undo/Redo with Fast.UndoRedo

This example shows how to use Fast.UndoRedo in a WinForms application to track property changes and provide Undo/Redo functionality.

## Form Code
```csharp
using System;
using System.ComponentModel;
using System.Windows.Forms;
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

public partial class MainForm : Form
{
    private readonly UndoRedoService _service = new UndoRedoService();
    private readonly Person _person = new Person();

    public MainForm()
    {
        InitializeComponent();
        _service.Attach(_person);
        nameTextBox.DataBindings.Add("Text", _person, "Name", true, DataSourceUpdateMode.OnPropertyChanged);
    }

    private void undoButton_Click(object sender, EventArgs e)
    {
        _service.Undo();
    }

    private void redoButton_Click(object sender, EventArgs e)
    {
        _service.Redo();
    }
}
```

## Notes
- Bind your controls to your model properties.
- Call Undo/Redo on button clicks to revert or reapply changes.
