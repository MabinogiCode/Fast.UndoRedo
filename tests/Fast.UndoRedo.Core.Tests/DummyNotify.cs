using System.ComponentModel;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// A simple class that implements INotifyPropertyChanging and INotifyPropertyChanged for testing.
    /// </summary>
    internal class DummyNotify : INotifyPropertyChanging, INotifyPropertyChanged
    {
        private string _name;

        /// <summary>
        /// Occurs when a property value is changing.
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
    }
}
