using System;
using System.Collections.Generic;

namespace Fast.UndoRedo.Core
{
    /// <summary>
    /// Disposable returned by <see cref="UndoRedoService.Subscribe"/> to unsubscribe observers.
    /// </summary>
    internal sealed class UndoRedoServiceUnsubscriber : IDisposable
    {
        private readonly List<IObserver<UndoRedoState>> _observers;
        private readonly IObserver<UndoRedoState> _observer;
        private readonly object _sync;

        /// <summary>
        /// Initializes a new instance of the <see cref="UndoRedoServiceUnsubscriber"/> class.
        /// </summary>
        /// <param name="observers">The list of observers.</param>
        /// <param name="observer">The observer to remove on dispose.</param>
        /// <param name="sync">The synchronization object.</param>
        public UndoRedoServiceUnsubscriber(List<IObserver<UndoRedoState>> observers, IObserver<UndoRedoState> observer, object sync)
        {
            _observers = observers;
            _observer = observer;
            _sync = sync;
        }

        /// <summary>
        /// Disposes the instance and removes the observer from the list.
        /// </summary>
        public void Dispose()
        {
            lock (_sync)
            {
                if (_observers.Contains(_observer))
                {
                    _observers.Remove(_observer);
                }
            }
        }
    }
}
