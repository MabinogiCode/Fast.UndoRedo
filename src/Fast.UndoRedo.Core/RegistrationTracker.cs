using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Fast.UndoRedo.Core.Logging;

namespace Fast.UndoRedo.Core
{
    /// <summary>
    /// Tracks registrations of objects for change notifications and collection subscriptions.
    /// </summary>
    public class RegistrationTracker
    {
        private readonly UndoRedoService _service;
        private readonly ICoreLogger _logger;

        private readonly ConditionalWeakTable<object, List<IDisposable>> _registrations = new();
        private readonly ConditionalWeakTable<object, Dictionary<string, object>> _valueCache = new();

        private readonly Dictionary<object, List<object>> _collectionSnapshots = new();
        private readonly object _collectionSnapshotsLock = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="RegistrationTracker"/> class.
        /// </summary>
        /// <param name="service">Service used to push undoable actions.</param>
        public RegistrationTracker(UndoRedoService service)
            : this(service, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegistrationTracker"/> class.
        /// </summary>
        /// <param name="service">Service used to push undoable actions.</param>
        /// <param name="logger">Logger for error reporting.</param>
        public RegistrationTracker(UndoRedoService service, ICoreLogger logger)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? new Logging.DebugCoreLogger();
        }

        /// <summary>
        /// Register an object for property and collection change tracking.
        /// </summary>
        /// <param name="obj">Object to register.</param>
        public void Register(object obj)
        {
            if (obj == null || _registrations.TryGetValue(obj, out _))
            {
                return;
            }

            var disposables = new List<IDisposable>();
            _registrations.Add(obj, disposables);

            if (!_valueCache.TryGetValue(obj, out var propCache))
            {
                propCache = new Dictionary<string, object>();
                _valueCache.Add(obj, propCache);
            }

            foreach (var prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // skip unreadable or explicitly ignored properties
                if (!prop.CanRead || prop.GetCustomAttribute<FastUndoIgnoreAttribute>() != null)
                {
                    continue;
                }

                // skip indexers
                if (prop.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                object val;
                try
                {
                    // Avoid invoking getters for read-only properties that may have side-effects.
                    // For auto-implemented read-only properties we can read the compiler-generated backing field
                    // named "<PropertyName>k__BackingField". If no backing field exists (computed property), skip it.
                    if (prop.GetSetMethod(true) == null)
                    {
                        var backingField = obj.GetType().GetField("<" + prop.Name + ">k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (backingField != null)
                        {
                            val = backingField.GetValue(obj);
                        }
                        else
                        {
                            // do not invoke the getter for computed/read-only properties without a backing field
                            continue;
                        }
                    }
                    else
                    {
                        val = prop.GetValue(obj);
                    }

                    propCache[prop.Name] = val;
                }
                catch (Exception ex)
                {
                    _logger.LogException(ex);
                    propCache[prop.Name] = null;
                    continue;
                }

                if (val == null || prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(string) || prop.PropertyType.GetCustomAttribute<FastUndoIgnoreAttribute>() != null)
                {
                    continue;
                }

                if (val is IEnumerable enumerable && !(val is string))
                {
                    var snapshot = new List<object>();
                    foreach (var item in enumerable)
                    {
                        snapshot.Add(item);
                        Register(item);
                    }

                    lock (_collectionSnapshotsLock)
                    {
                        _collectionSnapshots[val] = snapshot;
                    }
                }

                var collectionReg = CollectionRegistrar.RegisterCollection(val, _service, _collectionSnapshots, _logger);
                if (collectionReg != null)
                {
                    disposables.Add(collectionReg);
                }

                if (val is INotifyPropertyChanged)
                {
                    Register(val);
                }
            }

            var propHandlers = PropertyChangeRegistrar.Register(obj, _service, _valueCache, _logger);
            if (propHandlers != null)
            {
                disposables.Add(propHandlers);
            }
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

            _valueCache.Remove(obj);

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
