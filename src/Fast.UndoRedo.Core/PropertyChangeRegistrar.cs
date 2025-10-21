using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Fast.UndoRedo.Core.Logging;

namespace Fast.UndoRedo.Core
{
    /// <summary>
    /// Provides methods to register objects for property change undo/redo tracking.
    /// </summary>
    internal static class PropertyChangeRegistrar
    {
        /// <summary>
        /// Registers an object for property change undo/redo tracking by subscribing to property change events.
        /// </summary>
        /// <param name="target">The target object to register.</param>
        /// <param name="service">The UndoRedoService instance.</param>
        /// <param name="valueCache">The cache for storing old property values.</param>
        /// <param name="logger">The logger for error reporting.</param>
        /// <returns>An IDisposable to unsubscribe the registrations.</returns>
        public static IDisposable Register(object target, UndoRedoService service, ConditionalWeakTable<object, Dictionary<string, object>> valueCache, ICoreLogger logger)
        {
            if (target == null || service == null || valueCache == null)
            {
                return null;
            }

            var disposables = new List<IDisposable>();

            if (target is INotifyPropertyChanging inpcChanging)
            {
                PropertyChangingEventHandler changingHandler = (s, e) =>
                {
                    if (service.IsApplying)
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

                    try
                    {
                        if (!valueCache.TryGetValue(s, out var cache))
                        {
                            cache = new Dictionary<string, object>();

                            // add the cache for this instance
                            valueCache.Add(s, cache);
                        }

                        cache[e.PropertyName] = prop.GetValue(s);
                    }
                    catch (Exception ex)
                    {
                        logger.LogException(ex);
                    }
                };

                inpcChanging.PropertyChanging += changingHandler;
                disposables.Add(new DisposableAction(() => inpcChanging.PropertyChanging -= changingHandler));
            }

            if (target is INotifyPropertyChanged inpc)
            {
                PropertyChangedEventHandler changedHandler = (s, e) =>
                {
                    if (service.IsApplying)
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
                    if (valueCache.TryGetValue(s, out var cache) && cache.TryGetValue(e.PropertyName, out var o))
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
                        logger.LogException(ex);
                        newVal = null;
                    }

                    // Create setter delegate
                    var setter = ReflectionHelpers.CreateSetter(s.GetType(), prop, logger);
                    if (setter == null)
                    {
                        return;
                    }

                    try
                    {
                        var action = ActionFactory.CreatePropertyChangeAction(s, prop, setter, oldVal, newVal, $"{s.GetType().Name}.{prop.Name} changed", logger);
                        if (action is IUndoableAction ua)
                        {
                            service.Push(ua);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogException(ex);
                    }

                    // update cache
                    try
                    {
                        if (!valueCache.TryGetValue(s, out var cache2))
                        {
                            cache2 = new Dictionary<string, object>();
                            valueCache.Add(s, cache2);
                        }

                        cache2[e.PropertyName] = newVal;
                    }
                    catch (Exception ex)
                    {
                        logger.LogException(ex);
                    }
                };

                inpc.PropertyChanged += changedHandler;
                disposables.Add(new DisposableAction(() => inpc.PropertyChanged -= changedHandler));
            }

            return new CompositeDisposable(disposables);
        }

        private sealed class CompositeDisposable : IDisposable
        {
            private readonly List<IDisposable> _list;

            public CompositeDisposable(List<IDisposable> list)
            {
                _list = list ?? new List<IDisposable>();
            }

            public void Dispose()
            {
                foreach (var d in _list)
                {
                    try
                    {
                        d.Dispose();
                    }
                    catch
                    {
                        // ignore dispose exceptions
                    }
                }
            }
        }
    }
}
