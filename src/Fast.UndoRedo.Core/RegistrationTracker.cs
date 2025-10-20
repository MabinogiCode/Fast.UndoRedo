using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using Fast.UndoRedo.Core.Logging;

namespace Fast.UndoRedo.Core
{
    /// <summary>
    /// Tracks registrations of objects for change notifications and collection subscriptions.
    /// Registers property change and collection change handlers and caches values for undo/redo.
    /// </summary>
    public class RegistrationTracker
    {
        private readonly UndoRedoService _service;
        private readonly ICoreLogger _logger;
        private readonly Dictionary<object, List<IDisposable>> _registrations = new Dictionary<object, List<IDisposable>>();
        private readonly Dictionary<object, Dictionary<string, object>> _valueCache = new Dictionary<object, Dictionary<string, object>>();
        private readonly Dictionary<object, List<object>> _collectionSnapshots = new Dictionary<object, List<object>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="RegistrationTracker"/> class.
        /// </summary>
        /// <param name="service">Service used to push undoable actions.</param>
        public RegistrationTracker(UndoRedoService service)
            : this(service, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegistrationTracker"/> class with a logger.
        /// </summary>
        public RegistrationTracker(UndoRedoService service, ICoreLogger logger)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? new Logging.DebugCoreLogger();
        }

        /// <summary>
        /// Register an object for property and collection change tracking.
        /// The tracker subscribes to change events and caches values required to create undo actions.
        /// </summary>
        /// <param name="obj">Object to register (may implement INotifyPropertyChanged/IObservableCollection, etc.).</param>
        public void Register(object obj)
        {
            if (obj == null)
            {
                return;
            }

            if (_registrations.ContainsKey(obj))
            {
                return;
            }

            var disposables = new List<IDisposable>();
            _registrations[obj] = disposables;

            // Cache current public property values
            var propCache = new Dictionary<string, object>();
            _valueCache[obj] = propCache;

            foreach (var prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.GetCustomAttribute<FastUndoIgnoreAttribute>() != null)
                {
                    continue;
                }

                // skip non-readable properties entirely
                if (!prop.CanRead)
                {
                    continue;
                }

                // record whether property can be written; we still may register nested objects even if property is read-only
                var canWrite = prop.CanWrite;

                try
                {
                    propCache[prop.Name] = prop.GetValue(obj);
                }
                catch (Exception ex)
                {
                    _logger.LogException(ex);
                    propCache[prop.Name] = null;
                }

                // If property is a nested object, register recursively
                var val = prop.GetValue(obj);
                if (val == null)
                {
                    continue;
                }

                // Ignore primitives and strings when considering nested registration
                if (prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(string))
                {
                    continue;
                }

                // If property or its type has FastUndoIgnoreAttribute -> ignore recursively
                if (prop.GetCustomAttribute<FastUndoIgnoreAttribute>() != null || prop.PropertyType.GetCustomAttribute<FastUndoIgnoreAttribute>() != null)
                {
                    continue;
                }

                // If it's an IEnumerable, prepare snapshot and register elements
                if (val is IEnumerable enumerable && !(val is string))
                {
                    var snapshot = new List<object>();
                    foreach (var item in enumerable)
                    {
                        snapshot.Add(item);
                    }

                    _collectionSnapshots[val] = snapshot;

                    foreach (var item in snapshot)
                    {
                        if (item == null)
                        {
                            continue;
                        }

                        if (item is INotifyPropertyChanged || item is INotifyCollectionChanged)
                        {
                            try
                            {
                                Register(item);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogException(ex);
                            }
                        }
                    }
                }

                // If it's INotifyPropertyChanged or INotifyCollectionChanged, register recursively and subscribe to collection changes
                if (val is INotifyCollectionChanged incc)
                {
                    try
                    {
                        var sub = new CollectionSubscription(val, _service, _collectionSnapshots, _logger);
                        disposables.Add(sub);

                        // also register the collection object itself for nested property notifications
                        if (val is INotifyPropertyChanged)
                        {
                            try
                            {
                                Register(val);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogException(ex);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException(ex);
                    }
                }
                else if (val is INotifyPropertyChanged)
                {
                    try
                    {
                        Register(val);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException(ex);
                    }
                }
            }

            if (obj is INotifyPropertyChanging inpcChanging)
            {
                PropertyChangingEventHandler changingHandler = (s, e) =>
                {
                    if (_service.IsApplying)
                    {
                        return; // do not cache old values when applying undo/redo
                    }

                    var prop = s.GetType().GetProperty(e.PropertyName, BindingFlags.Public | BindingFlags.Instance);
                    if (prop == null)
                    {
                        return;
                    }

                    if (prop.GetCustomAttribute<FastUndoIgnoreAttribute>() != null)
                    {
                        return;
                    }

                    // store current value in cache (will be used on PropertyChanged)
                    try
                    {
                        _valueCache[s][e.PropertyName] = prop.GetValue(s);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException(ex);
                        _valueCache[s][e.PropertyName] = null;
                    }
                };

                inpcChanging.PropertyChanging += changingHandler;
                disposables.Add(new DisposableAction(() => inpcChanging.PropertyChanging -= changingHandler));
            }

            if (obj is INotifyPropertyChanged inpc)
            {
                PropertyChangedEventHandler changedHandler = (s, e) =>
                {
                    if (_service.IsApplying)
                    {
                        return; // skip recording when applying undo/redo
                    }

                    var prop = s.GetType().GetProperty(e.PropertyName, BindingFlags.Public | BindingFlags.Instance);
                    if (prop == null)
                    {
                        return;
                    }

                    if (prop.GetCustomAttribute<FastUndoIgnoreAttribute>() != null)
                    {
                        return;
                    }

                    object oldVal = null;
                    if (_valueCache.TryGetValue(s, out var cache) && cache.TryGetValue(e.PropertyName, out var o))
                    {
                        oldVal = o;
                    }

                    object newVal = null;
                    try
                    {
                        newVal = prop.GetValue(s);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException(ex);
                        newVal = null;
                    }

                    // Create setter delegate
                    var setter = ReflectionHelpers.CreateSetter(s.GetType(), prop);
                    if (setter == null)
                    {
                        return;
                    }

                    var actionType = typeof(PropertyChangeAction<,>).MakeGenericType(s.GetType(), prop.PropertyType);
                    try
                    {
                        // use a factory delegate instead of Activator when possible (left for later refactor)
                        var action = Activator.CreateInstance(actionType, s, setter, oldVal, newVal, $"{s.GetType().Name}.{prop.Name} changed");
                        if (action is IUndoableAction ua)
                        {
                            _service.Push(ua);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException(ex);
                    }

                    // update cache
                    if (_valueCache.TryGetValue(s, out var cache2))
                    {
                        cache2[e.PropertyName] = newVal;
                    }
                };

                inpc.PropertyChanged += changedHandler;
                disposables.Add(new DisposableAction(() => inpc.PropertyChanged -= changedHandler));
            }

            // store disposables
            _registrations[obj] = disposables;
        }

        /// <summary>
        /// Unregister an object and remove any subscriptions and cached values.
        /// </summary>
        /// <param name="obj">Object to unregister.</param>
        public void Unregister(object obj)
        {
            if (obj == null)
            {
                return;
            }

            if (_registrations.TryGetValue(obj, out var list))
            {
                foreach (var d in list)
                {
                    try
                    {
                        d.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException(ex);
                    }
                }

                _registrations.Remove(obj);
            }

            if (_valueCache.ContainsKey(obj))
            {
                _valueCache.Remove(obj);
            }

            if (_collectionSnapshots.ContainsKey(obj))
            {
                _collectionSnapshots.Remove(obj);
            }
        }
    }
}
