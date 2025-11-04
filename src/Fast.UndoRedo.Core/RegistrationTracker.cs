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

        // Enhanced reentrancy protection
        private readonly ConditionalWeakTable<object, object> _registrationInProgress = new();

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
        /// <param name="currentDepth">Current recursion depth to prevent infinite loops.</param>
        public void Register(object obj, int currentDepth = 0)
        {
            if (obj == null || _registrations.TryGetValue(obj, out _))
            {
                return;
            }

            // Enhanced reentrancy protection - prevent circular registration
            if (_registrationInProgress.TryGetValue(obj, out _))
            {
                _logger?.LogWarning($"Circular reference detected during registration of {obj.GetType().Name}. Skipping to prevent infinite recursion.");
                return;
            }

            // Get configuration for this type
            var config = GetConfigurationForType(obj.GetType());

            // Check maximum recursion depth
            if (currentDepth >= config.MaxRecursionDepth)
            {
                _logger?.LogWarning($"Maximum recursion depth ({config.MaxRecursionDepth}) reached for type {obj.GetType().Name}. Stopping registration.");
                return;
            }

            // Mark object as being registered
            _registrationInProgress.Add(obj, new object());

            try
            {
                RegisterInternal(obj, config, currentDepth);
            }
            finally
            {
                // Remove from registration in progress
                _registrationInProgress.Remove(obj);
            }
        }

        private void RegisterInternal(object obj, FastUndoConfigurationAttribute config, int currentDepth)
        {
            var disposables = new List<IDisposable>();
            _registrations.Add(obj, disposables);

            if (!_valueCache.TryGetValue(obj, out var propCache))
            {
                propCache = new Dictionary<string, object>();
                _valueCache.Add(obj, propCache);
            }

            foreach (var prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // Enhanced property filtering
                if (ShouldIgnoreProperty(prop, config, currentDepth))
                {
                    continue;
                }

                object val;
                try
                {
                    val = GetPropertyValueSafely(obj, prop, config);
                    propCache[prop.Name] = val;
                }
                catch (Exception ex)
                {
                    if (config.IgnorePropertiesWithExceptions)
                    {
                        _logger?.LogWarning($"Property {prop.Name} on {obj.GetType().Name} threw exception: {ex.Message}. Ignoring due to configuration.");
                        propCache[prop.Name] = null;
                        continue;
                    }

                    _logger?.LogException(ex);
                    propCache[prop.Name] = null;
                    continue;
                }

                if (val != null && ShouldProcessNestedObject(val, prop, config))
                {
                    ProcessNestedObject(val, prop, config, currentDepth, disposables);
                }
            }

            var propHandlers = PropertyChangeRegistrar.Register(obj, _service, _valueCache, _logger);
            if (propHandlers != null)
            {
                disposables.Add(propHandlers);
            }
        }

        private bool ShouldIgnoreProperty(PropertyInfo prop, FastUndoConfigurationAttribute config, int currentDepth)
        {
            // Check for FastUndoIgnore attribute
            var ignoreAttr = prop.GetCustomAttribute<FastUndoIgnoreAttribute>();
            if (ignoreAttr != null)
            {
                // Check if it should only be ignored in recursive registration
                if (ignoreAttr.OnlyIgnoreInRecursiveRegistration && currentDepth == 0)
                {
                    return false; // Don't ignore at root level
                }

                return true; // Ignore this property
            }

            // skip unreadable or explicitly ignored properties
            if (!prop.CanRead)
            {
                return true;
            }

            // skip indexers
            if (prop.GetIndexParameters().Length > 0)
            {
                return true;
            }

            // Skip properties whose type is marked to ignore
            if (prop.PropertyType.GetCustomAttribute<FastUndoIgnoreAttribute>() != null)
            {
                return true;
            }

            return false;
        }

        private object GetPropertyValueSafely(object obj, PropertyInfo prop, FastUndoConfigurationAttribute config)
        {
            // Avoid invoking getters for read-only properties that may have side-effects.
            // For auto-implemented read-only properties we can read the compiler-generated backing field
            // named "<PropertyName>k__BackingField". If no backing field exists (computed property), skip it.
            if (prop.GetSetMethod(true) == null)
            {
                var backingField = obj.GetType().GetField("<" + prop.Name + ">k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                if (backingField != null)
                {
                    return backingField.GetValue(obj);
                }
                else
                {
                    // do not invoke the getter for computed/read-only properties without a backing field
                    _logger?.LogDebug($"Skipping computed read-only property {prop.Name} on {obj.GetType().Name} to avoid side effects.");
                    return null;
                }
            }
            else
            {
                return prop.GetValue(obj);
            }
        }

        private bool ShouldProcessNestedObject(object val, PropertyInfo prop, FastUndoConfigurationAttribute config)
        {
            if (val == null)
            {
                return false;
            }

            if (prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(string))
            {
                return false;
            }

            if (!config.AutoRegisterNestedObjects)
            {
                return false;
            }

            return true;
        }

        private void ProcessNestedObject(object val, PropertyInfo prop, FastUndoConfigurationAttribute config, int currentDepth, List<IDisposable> disposables)
        {
            if (val is IEnumerable enumerable && !(val is string))
            {
                if (config.TrackCollections)
                {
                    var snapshot = new List<object>();
                    foreach (var item in enumerable)
                    {
                        snapshot.Add(item);

                        // Recursively register collection items with depth control
                        Register(item, currentDepth + 1);
                    }

                    lock (_collectionSnapshotsLock)
                    {
                        _collectionSnapshots[val] = snapshot;
                    }

                    var collectionReg = CollectionRegistrar.RegisterCollection(val, _service, _collectionSnapshots, _logger);
                    if (collectionReg != null)
                    {
                        disposables.Add(collectionReg);
                    }
                }
            }

            if (val is INotifyPropertyChanged)
            {
                // Recursively register with depth tracking
                Register(val, currentDepth + 1);
            }
        }

        private FastUndoConfigurationAttribute GetConfigurationForType(Type type)
        {
            return type.GetCustomAttribute<FastUndoConfigurationAttribute>() ?? new FastUndoConfigurationAttribute();
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
