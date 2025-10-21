using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Reflection;
using Fast.UndoRedo.Core.Logging;

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

        // tracker for Attach/Detach of objects
        private readonly RegistrationTracker _tracker;

        // Centralized store for all collection subscriptions to prevent duplicates.
        private readonly Dictionary<INotifyCollectionChanged, IDisposable> _collectionSubscriptions = new Dictionary<INotifyCollectionChanged, IDisposable>();
        private bool _isApplying;

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
        /// <param name="logger">The logger to use.</param>
        public UndoRedoService(ICoreLogger logger)
        {
            this.Logger = logger ?? new Logging.DebugCoreLogger();
            _tracker = new RegistrationTracker(this, this.Logger);
        }

        /// <summary>
        /// Event raised when the undo/redo state changes.
        /// </summary>
        public event EventHandler<UndoRedoState> StateChanged;

        /// <summary>
        /// Gets the optional logger used by core components.
        /// </summary>
        public ICoreLogger Logger { get; }

        /// <summary>
        /// Gets a value indicating whether the service is currently applying an undo/redo operation.
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

        /// <summary>
        /// Gets a value indicating whether undo is available.
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
        /// Gets a value indicating whether redo is available.
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

        /// <summary>
        /// Small helper to set a property value and register an undo action in one call.
        /// Usage: myService.StackUndo(owner, newValue, ref myObject.PropertyBackingField, nameof(MyProperty));
        /// Requires a readable/writable property with the given name on the owner object.
        /// </summary>
        public T StackUndo<T>(object owner, T newValue, ref T actualValue, string propertyName)
        {
            // compare values
            if (EqualityComparer<T>.Default.Equals(actualValue, newValue))
            {
                return actualValue;
            }

            try
            {
                if (owner != null && !string.IsNullOrEmpty(propertyName))
                {
                    var prop = owner.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                    if (prop != null && prop.CanWrite)
                    {
                        // create setter delegate
                        var setter = ReflectionHelpers.CreateSetter(owner.GetType(), prop, Logger);

                        var action = ActionFactory.CreatePropertyChangeAction(owner, prop, setter, actualValue, newValue, $"{propertyName} Changed", Logger);
                        if (action != null)
                        {
                            Push(action);
                        }
                        else
                        {
                            // fallback: no action created, just assign
                            actualValue = newValue;
                            return newValue;
                        }

                        // apply new value (the action's Redo will reapply if undone)
                        try
                        {
                            // use setter to apply immediately if available
                            if (setter is Delegate d)
                            {
                                d.DynamicInvoke(owner, newValue);
                            }
                            else
                            {
                                prop.SetValue(owner, newValue);
                            }
                        }
                        catch
                        {
                            // ignore apply errors, value will remain changed by direct assignment below
                        }

                        actualValue = newValue;
                        return newValue;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.LogException(ex);
            }

            // fallback: no owner/property found or error -> just assign
            actualValue = newValue;
            return newValue;
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
        /// <param name="obj">The object to attach for tracking.</param>
        public void Attach(object obj)
        {
            _tracker.Register(obj);
        }

        /// <summary>
        /// Detach an object from tracking.
        /// </summary>
        /// <param name="obj">The object to detach from tracking.</param>
        public void Detach(object obj)
        {
            _tracker.Unregister(obj);
        }

        /// <summary>
        /// Attach a collection instance directly (no need to replace with UndoableCollection).
        /// </summary>
        /// <param name="collection">The collection to attach.</param>
        public void AttachCollection(INotifyCollectionChanged collection)
        {
            AttachCollectionInternal(collection, null);
        }

        /// <summary>
        /// Detach a previously attached collection.
        /// </summary>
        /// <param name="collection">The collection to detach.</param>
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
                TopUndoDescription = TopUndoDescription,
            });

            return new UndoRedoServiceUnsubscriber(_observers, observer, _sync);
        }

        /// <summary>
        /// Centralized method to attach a collection and prevent duplicate subscriptions.
        /// </summary>
        /// <param name="collectionInstance">The collection instance to attach.</param>
        /// <param name="snapshots">The dictionary of snapshots, used by the registration tracker.</param>
        /// <returns>A disposable subscription, or null if already subscribed or failed.</returns>
        internal IDisposable AttachCollectionInternal(object collectionInstance, Dictionary<object, List<object>> snapshots)
        {
            if (collectionInstance is not INotifyCollectionChanged incc)
            {
                return null;
            }

            lock (_sync)
            {
                if (_collectionSubscriptions.ContainsKey(incc))
                {
                    return null;
                }

                try
                {
                    var sub = new CollectionSubscription(incc, this, snapshots, Logger);
                    _collectionSubscriptions[incc] = sub;
                    return sub;
                }
                catch (Exception ex)
                {
                    Logger?.LogException(ex);
                    return null;
                }
            }
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
                    sc.Post(
                        _ =>
                        {
                            lock (_sync)
                            {
                                _isApplying = false;
                            }
                        },
                        null);
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

        private void NotifyStateChanged()
        {
            var state = new UndoRedoState
            {
                CanUndo = CanUndo,
                CanRedo = CanRedo,
                TopRedoDescription = TopRedoDescription,
                TopUndoDescription = TopUndoDescription,
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
    }
}
