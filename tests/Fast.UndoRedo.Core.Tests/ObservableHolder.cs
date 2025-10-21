using System.ComponentModel;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// A simple class that holds an observable collection for memory leak testing.
    /// </summary>
    internal class ObservableHolder : INotifyPropertyChanged
    {
        /// <summary>
        /// Occurs when a property value has changed.
        /// </summary>
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { }
            remove { }
        }

        /// <summary>
        /// Gets the collection of items.
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<string> Items { get; } = new System.Collections.ObjectModel.ObservableCollection<string>();
    }
}
