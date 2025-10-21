using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// A simple view model for integration testing.
    /// </summary>
    internal class PersonViewModel : INotifyPropertyChanged
    {
        private string _name = string.Empty;

        /// <summary>
        /// Occurs when a property value has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the name and raises change notifications.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        /// <summary>
        /// Gets the collection of items.
        /// </summary>
        public ObservableCollection<string> Items { get; } = new ObservableCollection<string>();
    }
}
