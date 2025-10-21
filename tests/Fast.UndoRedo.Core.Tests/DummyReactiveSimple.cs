using System;
using System.ComponentModel;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// A simple reactive class for testing purposes.
    /// </summary>
    internal class DummyReactiveSimple : INotifyPropertyChanged
    {
        private readonly System.Reactive.Subjects.Subject<object> _changing = new System.Reactive.Subjects.Subject<object>();
        private readonly System.Reactive.Subjects.Subject<object> _changed = new System.Reactive.Subjects.Subject<object>();

        private string _name;

        /// <summary>
        /// Occurs when a property value has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the observable for property changing events.
        /// </summary>
        public IObservable<object> Changing => _changing;

        /// <summary>
        /// Gets the observable for property changed events.
        /// </summary>
        public IObservable<object> Changed => _changed;

        /// <summary>
        /// Gets or sets the name and raises change notifications.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                _changing.OnNext(new { PropertyName = nameof(Name) });
                _name = value;
                _changed.OnNext(new { PropertyName = nameof(Name) });
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }
    }
}
