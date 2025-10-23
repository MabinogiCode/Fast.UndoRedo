using System;
using System.ComponentModel;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// View model used by tests that contains a computed read-only property which throws when backing state is null.
    /// </summary>
    internal class FaultyViewModel : INotifyPropertyChanged, INotifyPropertyChanging
    {
        private object _model;
        private int _value;

        /// <summary>
        /// Occurs when a property value is about to change.
        /// </summary>
        public event PropertyChangingEventHandler PropertyChanging;

        /// <summary>
        /// Occurs when a property value has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the underlying model object used by the computed property.
        /// </summary>
        public object Model
        {
            get => _model;
            set
            {
                PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(nameof(Model)));
                _model = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Model)));
            }
        }

        /// <summary>
        /// A get-only computed property that throws when <see cref="Model"/> is null. The registration code must not evaluate it.
        /// </summary>
        public string LevelsCountText
        {
            get
            {
                if (Model == null)
                {
                    throw new NullReferenceException("Model is null");
                }

                return "ok";
            }
        }

        /// <summary>
        /// A normal writable property used to verify undo tracking still works.
        /// </summary>
        public int Value
        {
            get => _value;
            set
            {
                PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(nameof(Value)));
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }
    }
}
