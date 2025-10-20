using Fast.UndoRedo.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Collections.Specialized;

namespace Fast.UndoRedo.Core
{
    /// <summary>
    /// Service that records undoable actions and provides undo/redo operations.
    /// </summary>
    public class UndoRedoService : IObservable<UndoRedoState>
    {
        private readonly Stack<IUndoableAction> _undo = new Stack<IUndoableAction>();
        private readonly Stack<IUndoableAction> _redo = new Stack<IUndoableAction>();
        private readonly List<IObserver<UndoRedoState>> _observers = new List<IObserver<UndoRedoState>>();
        private readonly object _sync = new object();

        /// <summary>
        /// Optional logger used by core components.
        /// </summary>
        public ICoreLogger Logger { get; }

        /// <summary>
        /// Event raised when the undo/redo state changes.
        /// </summary>
        public event EventHandler<UndoRedoState> StateChanged;

        private bool _isApplying;

        /// <summary>
        /// Gets whether the service is currently applying an undo/redo operation.
        /// </summary>
        public bool IsApplying
        {
            get
            {
                lock (_sync)
                {
                    return _isApplying;
                }
            }
        }

        // tracker for Attach/Detach of objects
        private readonly RegistrationTracker _tracker;

        // explicit collection subscriptions created via AttachCollection
        private readonly Dictionary<INotifyCollectionChanged, IDisposable> _collectionSubscriptions = new Dictionary<INotifyCollectionChanged, IDisposable>();

        /// <summary>
        /// Initializes a new instance of the <see cref="UndoRedoService"/> class.
        /// </summary>
        public UndoRedoService()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UndoRedoService"/> class with an optional logger.
        /// </summary>
        public UndoRedoService(ICoreLogger logger)
        {
            this.Logger = logger ?? new Logging.DebugCoreLogger();
            _tracker = new RegistrationTracker(this, this.Logger);
        }

        /// <summary>
        /// Gets whether undo is available.
        /// </summary>
        public bool CanUndo
        {
            get
            {
                lock (_sync)
                {
                    return _undo.Count > 0;
                }
            }
        }

        /// <summary>
        /// Gets whether redo is available.
        /// </summary>
        public bool CanRedo
        {
            get
            {
                lock (_sync)
                {
                    return _redo.Count > 0;
                }
            }
        }

        /// <summary>
        /// Gets the description of the top undo action if available.
        /// </summary>
        public string TopUndoDescription
        {
            get
            {
                lock (_sync)
                {
                    return _undo.Count > 0 ? _undo.Peek().Description : null;
                }
            }
        }

        /// <summary>
        /// Gets the description of the top redo action if available.
        /// </summary>
        public string TopRedoDescription
        {
            get
            {
                lock (_sync)
                {
                    return _redo.Count > 0 ? _redo.Peek().Description : null;
                }
            }
        }

        /// <summary>
        /// Pushes an undoable action onto the stack. Actions pushed while the service is applying are ignored.
        /// </summary>
        /// <param name="action">The action to record.</param>
        public void Push(IUndoableAction action)
        {
            if (action == null)
            {
                return;
            }

            lock (_sync)
            {
                if (_isApplying)
                {
                    return; // ignore pushes that occur while applying undo/redo
                }

                _undo.Push(action);
                _redo.Clear();
            }

            NotifyStateChanged();
        }

        private void ExecuteAction(Action action)
        {
            if (action == null)
            {
                return;
            }

            // mark applying
            lock (_sync)
            {
                _isApplying = true;
            }

            try
            {
                action();
            }
            finally
            {
                // Instead of clearing _isApplying immediately, post a callback to the current synchronization context (if any)
                // so that any cascading UI events on that context occur while _isApplying is still true and won't be recorded.
                var sc = SynchronizationContext.Current;
                if (sc != null)
                {
                    sc.Post(_ =>
                    {
                        lock (_sync)
                        {
                            _isApplying = false;
                        }
                    }, null);
                }
                else
                {
                    lock (_sync)
                    {
                        _isApplying = false;
                    }
                }
            }
        }

        /// <summary>
        /// Undoes the most recent action, if any.
        /// </summary>
        public void Undo()
        {
            IUndoableAction act = null;
            lock (_sync)
            {
                if (_undo.Count == 0)
                {
                    return;
                }

                act = _undo.Pop();
            }

            ExecuteAction(() => act.Undo());
            lock (_sync)
            {
                _redo.Push(act);
            }

            NotifyStateChanged();
        }

        /// <summary>
        /// Redoes the most recently undone action, if any.
        /// </summary>
        public void Redo()
        {
            IUndoableAction act = null;
            lock (_sync)
            {
                if (_redo.Count == 0)
                {
                    return;
                }

                act = _redo.Pop();
            }

            ExecuteAction(() => act.Redo());
            lock (_sync)
            {
                _undo.Push(act);
            }

            NotifyStateChanged();
        }

        /// <summary>
        /// Clears the undo and redo stacks.
        /// </summary>
        public void Clear()
        {
            lock (_sync)
            {
                _undo.Clear();
                _redo.Clear();
            }

            NotifyStateChanged();
        }

        /// <summary>
        /// Attach an arbitrary object to be tracked (properties + collections recursively).
        /// </summary>
        public void Attach(object obj)
        {
            _tracker.Register(obj);
        }

        /// <summary>
        /// Detach an object from tracking.
        /// </summary>
        public void Detach(object obj)
        {
            _tracker.Unregister(obj);
        }

        /// <summary>
        /// Attach a collection instance directly (no need to replace with UndoableCollection).
        /// </summary>
        public void AttachCollection(INotifyCollectionChanged collection)
        {
            if (collection == null)
            {
                return;
            }

            lock (_sync)
            {
                if (_collectionSubscriptions.ContainsKey(collection))
                {
                    return;
                }

                try
                {
                    var sub = new CollectionSubscription(collection, this, null);
                    _collectionSubscriptions[collection] = sub;
                }
                catch
                {
                    // ignore subscription creation errors
                }
            }
        }

        /// <summary>
        /// Detach a previously attached collection.
        /// </summary>
        public void DetachCollection(INotifyCollectionChanged collection)
        {
            if (collection == null)
            {
                return;
            }

            lock (_sync)
            {
                if (_collectionSubscriptions.TryGetValue(collection, out var sub))
                {
                    try
                    {
                        sub.Dispose();
                    }
                    catch
                    {
                        // ignore dispose errors
                    }

                    _collectionSubscriptions.Remove(collection);
                }
            }
        }

        private void NotifyStateChanged()
        {
            var state = new UndoRedoState
            {
                CanUndo = CanUndo,
                CanRedo = CanRedo,
                TopRedoDescription = TopRedoDescription,
                TopUndoDescription = TopUndoDescription
            };

            StateChanged?.Invoke(this, state);

            List<IObserver<UndoRedoState>> copy;
            lock (_sync)
            {
                copy = _observers.ToList();
            }

            foreach (var o in copy)
            {
                try
                {
                    o.OnNext(state);
                }
                catch
                {
                    // ignore observer exceptions
                }
            }
        }

        /// <summary>
        /// Subscribe to state notifications.
        /// </summary>
        /// <param name="observer">Observer that receives state updates.</param>
        /// <returns>A disposable handle to unsubscribe.</returns>
        public IDisposable Subscribe(IObserver<UndoRedoState> observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer));
            }

            lock (_sync)
            {
                _observers.Add(observer);
            }

            observer.OnNext(new UndoRedoState
            {
                CanUndo = CanUndo,
                CanRedo = CanRedo,
                TopRedoDescription = TopRedoDescription,
                TopUndoDescription = TopUndoDescription
            });

            return new UndoRedoServiceUnsubscriber(_observers, observer, _sync);
        }
    }
}
