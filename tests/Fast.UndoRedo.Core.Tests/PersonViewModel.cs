using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// A simple view model for integration testing.
    /// Implements both INotifyPropertyChanged and INotifyPropertyChanging so the registrar can cache old values.
    /// </summary>
    internal class PersonViewModel : INotifyPropertyChanged, INotifyPropertyChanging
    {
        private string _name = string.Empty;

        /// <summary>
        /// Occurs when a property value is about to change.
        /// </summary>
        public event PropertyChangingEventHandler PropertyChanging;

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
                PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(nameof(Name)));
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
