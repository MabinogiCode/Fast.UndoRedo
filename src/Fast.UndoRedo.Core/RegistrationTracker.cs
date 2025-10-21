using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Fast.UndoRedo.Core.Logging;

namespace Fast.UndoRedo.Core
{
    /// <summary>
    /// Tracks registrations of objects for change notifications and collection subscriptions.
    /// Registers property change and collection change handlers and caches values for undo/redo.
    /// Improvements applied to follow best practices:
    /// - use ConditionalWeakTable for registrations and value cache to avoid keeping strong references to tracked objects (prevents memory leaks)
    /// - tighten null checks and reduce repeated dictionary-style indexing
    /// </summary>
    public class RegistrationTracker
    {
        private readonly UndoRedoService _service;
        private readonly ICoreLogger _logger;

        // Use ConditionalWeakTable to avoid preventing tracked objects from being garbage collected
        private readonly ConditionalWeakTable<object, List<IDisposable>> _registrations = new ConditionalWeakTable<object, List<IDisposable>>();
        private readonly ConditionalWeakTable<object, Dictionary<string, object>> _valueCache = new ConditionalWeakTable<object, Dictionary<string, object>>();

        // Collection snapshots are kept as a normal dictionary because snapshots are typically long-lived and keyed by collection instances
        private readonly Dictionary<object, List<object>> _collectionSnapshots = new Dictionary<object, List<object>>();
        private readonly object _collectionSnapshotsLock = new object();

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

            // If already registered, skip
            if (_registrations.TryGetValue(obj, out _))
            {
                return;
            }

            var disposables = new List<IDisposable>();

            // Add registrations table entry early so nested registration calls see this object as registered
            try
            {
                _registrations.Add(obj, disposables);
            }
            catch
            {
                // If Add fails because key already exists, just return
                if (_registrations.TryGetValue(obj, out _))
                {
                    return;
                }

                throw;
            }

            // Cache current public property values
            var propCache = new Dictionary<string, object>();
            try
            {
                _valueCache.Add(obj, propCache);
            }
            catch
            {
                // if already present, ignore - we will re-use existing
                if (!_valueCache.TryGetValue(obj, out var existing))
                {
                    // if unexpected, ensure we still have a cache reference
                    _valueCache.Add(obj, propCache);
                }
                else
                {
                    propCache = existing;
                }
            }

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
                object val = null;
                try
                {
                    val = prop.GetValue(obj);
                }
                catch (Exception ex)
                {
                    _logger.LogException(ex);
                    continue;
                }

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

                    lock (_collectionSnapshotsLock)
                    {
                        _collectionSnapshots[val] = snapshot;
                    }

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

                // delegate collection subscription creation
                var collectionReg = CollectionRegistrar.RegisterCollection(val, _service, _collectionSnapshots, _logger);
                if (collectionReg != null)
                {
                    disposables.Add(collectionReg);

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

                    continue;
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

            // Use PropertyChangeRegistrar to register property changing/changed handlers
            var propHandlers = PropertyChangeRegistrar.Register(obj, _service, _valueCache, _logger);
            if (propHandlers != null)
            {
                disposables.Add(propHandlers);
            }

            // No need to set _registrations[obj] because we added earlier
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

            // Remove value cache if present
            try
            {
                _valueCache.Remove(obj);
            }
            catch
            {
                // ignore
            }

            lock (_collectionSnapshotsLock)
            {
                if (_collectionSnapshots.ContainsKey(obj))
                {
                    _collectionSnapshots.Remove(obj);
                }
            }
        }
    }
}
