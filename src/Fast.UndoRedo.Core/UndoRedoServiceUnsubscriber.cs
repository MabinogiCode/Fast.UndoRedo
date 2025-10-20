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

        public UndoRedoServiceUnsubscriber(List<IObserver<UndoRedoState>> observers, IObserver<UndoRedoState> observer, object sync)
        {
            _observers = observers;
            _observer = observer;
            _sync = sync;
        }

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
