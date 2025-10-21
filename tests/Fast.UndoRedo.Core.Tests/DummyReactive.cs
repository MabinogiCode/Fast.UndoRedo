using System;
using System.ComponentModel;
using System.Reactive.Subjects;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// A reactive class for testing that implements property change notifications.
    /// </summary>
    internal class DummyReactive : INotifyPropertyChanged, INotifyPropertyChanging
    {
        private readonly Subject<object> _changing = new Subject<object>();
        private readonly Subject<object> _changed = new Subject<object>();
        private string _name;

        /// <summary>
        /// Occurs when a property value has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Occurs when a property value is changing.
        /// </summary>
        public event PropertyChangingEventHandler PropertyChanging;

        /// <summary>
        /// Gets the observable for property changing events.
        /// </summary>
        public IObservable<object> Changing => _changing;

        /// <summary>
        /// Gets the observable for property changed events.
        /// </summary>
        public IObservable<object> Changed => _changed;

        /// <summary>
        /// Gets or sets the name and raises all change notifications.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(nameof(Name)));
                _changing.OnNext(new { PropertyName = nameof(Name) });
                _name = value;
                _changed.OnNext(new { PropertyName = nameof(Name) });
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }
    }
}
