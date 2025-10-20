using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Fast.UndoRedo.Core;
using Fast.UndoRedo.Mvvm;

namespace Sample.WPF
{
    public class MainWindow : Window
    {
        private readonly UndoRedoService service = new UndoRedoService();
        private readonly MvvmAdapter adapter;
        private readonly SampleViewModel vm;

        private TextBox textBox;
        private CheckBox checkBox;
        private ListBox listBox;
        private Button addButton;
        private Button removeButton;
        private Button undoButton;
        private Button redoButton;

        public MainWindow()
        {
            this.Title = "Sample WPF - Fast.UndoRedo";
            this.Width = 600;
            this.Height = 400;

            this.adapter = new MvvmAdapter(this.service);

            this.vm = new SampleViewModel();
            // attach existing collection to the service
            this.service.AttachCollection(this.vm.Items);

            this.adapter.Register(this.vm);

            this.BuildUi();

            this.KeyDown += this.MainWindow_KeyDown;

            // subscribe to service to update undo/redo buttons
            this.service.StateChanged += (s, state) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    this.undoButton.IsEnabled = state.CanUndo;
                    this.redoButton.IsEnabled = state.CanRedo;
                    this.undoButton.Content = state.CanUndo ? $"Undo: {state.TopUndoDescription}" : "Undo";
                    this.redoButton.Content = state.CanRedo ? $"Redo: {state.TopRedoDescription}" : "Redo";
                });
            };
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.Key == Key.Z)
                {
                    this.service.Undo();
                }

                if (e.Key == Key.Y)
                {
                    this.service.Redo();
                }
            }
        }

        private void BuildUi()
        {
            var grid = new Grid();
            grid.Margin = new Thickness(8);

            var row1 = new RowDefinition() { Height = GridLength.Auto };
            var row2 = new RowDefinition() { Height = GridLength.Auto };
            var row3 = new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) };
            var row4 = new RowDefinition() { Height = GridLength.Auto };
            grid.RowDefinitions.Add(row1);
            grid.RowDefinitions.Add(row2);
            grid.RowDefinitions.Add(row3);
            grid.RowDefinitions.Add(row4);

            var stack = new StackPanel() { Orientation = Orientation.Horizontal };
            this.textBox = new TextBox() { Width = 300, Margin = new Thickness(0, 0, 8, 0) };
            this.textBox.SetBinding(TextBox.TextProperty, new System.Windows.Data.Binding("TextField") { Source = this.vm, Mode = System.Windows.Data.BindingMode.TwoWay, UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged });
            stack.Children.Add(this.textBox);

            this.checkBox = new CheckBox() { Content = "Show list", VerticalAlignment = VerticalAlignment.Center };
            this.checkBox.SetBinding(CheckBox.IsCheckedProperty, new System.Windows.Data.Binding("IsChecked") { Source = this.vm, Mode = System.Windows.Data.BindingMode.TwoWay });
            stack.Children.Add(this.checkBox);

            Grid.SetRow(stack, 0);
            grid.Children.Add(stack);

            this.listBox = new ListBox() { Margin = new Thickness(0, 8, 0, 8) };
            this.listBox.SetBinding(ListBox.ItemsSourceProperty, new System.Windows.Data.Binding("Items") { Source = this.vm });
            this.listBox.SetBinding(ListBox.SelectedItemProperty, new System.Windows.Data.Binding("SelectedItem") { Source = this.vm, Mode = System.Windows.Data.BindingMode.TwoWay });
            Grid.SetRow(this.listBox, 2);
            grid.Children.Add(this.listBox);

            var btnPanel = new StackPanel() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };
            this.addButton = new Button() { Content = "+", Width = 24, Height = 24, Margin = new Thickness(0, 0, 4, 0) };
            this.addButton.Click += (s, e) => this.vm.AddItem("New Item");
            btnPanel.Children.Add(this.addButton);
            this.removeButton = new Button() { Content = "-", Width = 24, Height = 24, Margin = new Thickness(0, 0, 4, 0) };
            this.removeButton.Click += (s, e) => this.vm.RemoveSelected();
            btnPanel.Children.Add(this.removeButton);

            this.undoButton = new Button() { Content = "Undo", Margin = new Thickness(8, 0, 4, 0) };
            this.undoButton.Click += (s, e) => this.service.Undo();
            btnPanel.Children.Add(this.undoButton);
            this.redoButton = new Button() { Content = "Redo", Margin = new Thickness(4, 0, 4, 0) };
            this.redoButton.Click += (s, e) => this.service.Redo();
            btnPanel.Children.Add(this.redoButton);

            Grid.SetRow(btnPanel, 3);
            grid.Children.Add(btnPanel);

            this.Content = grid;

            // Visibility binding for list based on checkbox
            var visBinding = new System.Windows.Data.Binding("IsChecked") { Source = this.vm, Converter = new BooleanToVisibilityConverter() };
            this.listBox.SetBinding(UIElement.VisibilityProperty, visBinding);
        }
    }
}
