namespace Sample.WPF
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using Fast.UndoRedo.Core;

    /// <summary>
    /// Example view model used by the sample WPF application.
    /// Demonstrates property notifications and use of UndoableCollection.
    /// </summary>
    public class SampleViewModel : INotifyPropertyChanged, INotifyPropertyChanging
    {
        private string _textField;
        private bool _isChecked;
        private string _selectedItem;

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc />
        public event PropertyChangingEventHandler PropertyChanging;

        /// <summary>
        /// Items collection that records changes.
        /// Uses ObservableCollection so it can be attached to UndoRedoService externally.
        /// </summary>
        public ObservableCollection<string> Items { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Initializes a new instance of the sample view model.
        /// </summary>
        public SampleViewModel()
        {
            // Items will be attached to the service by the host if needed
            Items.Add("Item 1");
            Items.Add("Item 2");
        }

        /// <summary>
        /// Sample text field property.
        /// </summary>
        public string TextField
        {
            get => _textField;
            set
            {
                if (value == _textField)
                {
                    return;
                }

                OnPropertyChanging();
                _textField = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Sample checkbox property.
        /// </summary>
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (value == _isChecked)
                {
                    return;
                }

                OnPropertyChanging();
                _isChecked = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Selected item (ignored by undo tracking via attribute).
        /// </summary>
        [FastUndoIgnore]
        public string SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (value == _selectedItem)
                {
                    return;
                }

                OnPropertyChanging();
                _selectedItem = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Add a new item to the collection.
        /// </summary>
        /// <param name="text">Item text.</param>
        public void AddItem(string text)
        {
            Items.Add(text);
        }

        /// <summary>
        /// Remove the selected item from the collection.
        /// </summary>
        public void RemoveSelected()
        {
            if (SelectedItem != null)
            {
                Items.Remove(SelectedItem);
            }
        }

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="name">Property name.</param>
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// Raises the PropertyChanging event.
        /// </summary>
        /// <param name="name">Property name.</param>
        protected void OnPropertyChanging([CallerMemberName] string name = null)
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(name));
        }
    }
}
