# WPF Example: Undo/Redo with Fast.UndoRedo

This example demonstrates how to use Fast.UndoRedo in a WPF application to track property and collection changes.

## ViewModel
```csharp
using System.ComponentModel;
using System.Collections.ObjectModel;

public class PersonViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    private string _name;
    public string Name
    {
        get => _name;
        set { _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); }
    }

    public ObservableCollection<string> Items { get; } = new ObservableCollection<string>();
}
```

## MainWindow.xaml
```xml
<Window x:Class="MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Undo/Redo Sample" Height="200" Width="400">
    <StackPanel>
        <TextBox Text="{Binding Name, UpdateSourceTrigger=PropertyChanged}" Margin="5" />
        <Button Content="Add Item" Click="AddItem_Click" Margin="5" />
        <ListBox ItemsSource="{Binding Items}" Margin="5" />
        <StackPanel Orientation="Horizontal">
            <Button Content="Undo" Click="Undo_Click" Margin="5" />
            <Button Content="Redo" Click="Redo_Click" Margin="5" />
        </StackPanel>
    </StackPanel>
</Window>
```

## MainWindow.xaml.cs
```csharp
using System.Windows;
using Fast.UndoRedo.Core;

public partial class MainWindow : Window
{
    private readonly UndoRedoService _service = new UndoRedoService();
    private readonly PersonViewModel _vm = new PersonViewModel();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
        _service.Attach(_vm);
        _service.AttachCollection(_vm.Items);
    }

    private void AddItem_Click(object sender, RoutedEventArgs e)
    {
        _vm.Items.Add("Item " + (_vm.Items.Count + 1));
    }

    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        _service.Undo();
    }

    private void Redo_Click(object sender, RoutedEventArgs e)
    {
        _service.Redo();
    }
}
```

## Notes
- All property and collection changes are tracked automatically.
- Undo/Redo buttons revert or reapply the last change.
- You can extend this pattern for more complex MVVM scenarios.
