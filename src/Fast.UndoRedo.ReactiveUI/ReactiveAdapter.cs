using Fast.UndoRedo.Core;
using Fast.UndoRedo.Core.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Fast.UndoRedo.ReactiveUI
{
    /// <summary>
    /// Adapter that integrates Reactive-style observable property change events with the UndoRedo core.
    /// It supports subscribing to ReactiveUI-style Changing/Changed observables as well as INotifyPropertyChanged/Changing.
    /// </summary>
    public class ReactiveAdapter
    {
        // instance fields
        private readonly UndoRedoService service;
        private readonly ICoreLogger logger;

        // Use ConditionalWeakTable to avoid keeping strong references to registered objects
        private readonly ConditionalWeakTable<object, List<IDisposable>> _registrations = new();
        private readonly ConditionalWeakTable<object, Dictionary<string, object>> _valueCache = new();

        private readonly object _sync = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ReactiveAdapter"/> class.
        /// </summary>
        /// <param name="service">The undo/redo service used to push recorded actions.</param>
        public ReactiveAdapter(UndoRedoService service)
            : this(service, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReactiveAdapter"/> class with an optional logger.
        /// </summary>
        /// <param name="service">The undo/redo service used to push recorded actions.</param>
        /// <param name="logger">Optional logger used by the adapter for diagnostics.</param>
        public ReactiveAdapter(UndoRedoService service, ICoreLogger logger)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            this.logger = logger ?? new DebugCoreLogger();
        }

        /// <summary>
        /// Tries to extract a property name from a Reactive-style event object.
        /// </summary>
        /// <param name="evt">The event object published by the observable.</param>
        /// <returns>The property name if found; otherwise null.</returns>
        private static string TryGetPropertyName(object evt)
        {
            try
            {
                var t = evt.GetType();
                var pn = t.GetProperty("PropertyName") ?? t.GetProperty("PropertyChangingEventArgs") ?? t.GetProperty("PropertyChangedEventArgs");
                if (pn != null)
                {
                    var val = pn.GetValue(evt);
                    if (val is string s)
                    {
                        return s;
                    }

                    var inner = val?.GetType().GetProperty("PropertyName");
                    if (inner != null)
                    {
                        return inner.GetValue(val) as string;
                    }
                }

                var fn = t.GetField("PropertyName");
                if (fn != null)
                {
                    return fn.GetValue(evt) as string;
                }
            }
            catch (Exception ex)
            {
                new DebugCoreLogger().LogException(ex);
            }

            return null;
        }

        /// <summary>
        /// Gets a public property or field value by name from an object.
        /// </summary>
        /// <param name="obj">Object to inspect.</param>
        /// <param name="name">Member name.</param>
        private static object GetMemberValue(object obj, string name)
        {
            var t = obj.GetType();
            var prop = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null)
            {
                return prop.GetValue(obj);
            }

            var field = t.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                return field.GetValue(obj);
            }

            return null;
        }

        /// <summary>
        /// Subscribes to an IObservable&lt;T&gt; discovered via reflection and captures the disposable.
        /// </summary>
        /// <param name="observable">The observable instance.</param>
        /// <param name="onNext">Action invoked for each observable notification.</param>
        /// <param name="disposables">List to capture the returned disposable.</param>
        private static void SubscribeObservable(object observable, Action<object> onNext, List<IDisposable> disposables)
        {
            if (observable == null)
            {
                return;
            }

            var obsType = observable.GetType();
            var iobs = obsType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IObservable<>));
            if (iobs == null)
            {
                return;
            }

            var eventType = iobs.GetGenericArguments()[0];

            var observerType = typeof(CoreObserverWrapper<>).MakeGenericType(eventType);
            var observer = Activator.CreateInstance(observerType, onNext);

            // find Subscribe method
            var mi = obsType.GetMethods().FirstOrDefault(m => m.Name == "Subscribe" && m.GetParameters().Length == 1);
            if (mi == null)
            {
                return;
            }

            try
            {
                var disp = mi.Invoke(observable, new object[] { observer }) as IDisposable;
                if (disp != null)
                {
                    disposables.Add(disp);
                }
            }
            catch (Exception ex)
            {
                new DebugCoreLogger().LogException(ex);
            }
        }

        /// <summary>
        /// Register a Reactive or INotify object to have its property changes recorded.
        /// </summary>
        /// <param name="reactiveObject">Object implementing Reactive observables or INotifyPropertyChanged/Changing.</param>
        public void Register(object reactiveObject)
        {
            if (reactiveObject == null)
            {
                return;
            }

            lock (_sync)
            {
                if (_registrations.TryGetValue(reactiveObject, out _))
                {
                    return;
                }
            }

            var disposables = new List<IDisposable>();

            // prepare value cache
            var propCache = new Dictionary<string, object>();

            // initialize cache for public properties
            foreach (var p in reactiveObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!p.CanRead || !p.CanWrite)
                {
                    continue;
                }

                try
                {
                    propCache[p.Name] = p.GetValue(reactiveObject);
                }
                catch (Exception ex)
                {
                    this.logger.LogException(ex);
                    propCache[p.Name] = null;
                }
            }

            try
            {
                _valueCache.Add(reactiveObject, propCache);
            }
            catch
            {
                // if already present, ignore
            }

            try
            {
                _registrations.Add(reactiveObject, disposables);
            }
            catch
            {
                // already registered concurrently
                if (!_registrations.TryGetValue(reactiveObject, out _))
                {
                    throw;
                }
            }

            // Try to find ReactiveUI observables named "Changing" and "Changed"
            object changingObs = GetMemberValue(reactiveObject, "Changing");
            object changedObs = GetMemberValue(reactiveObject, "Changed");

            if (changingObs != null)
            {
                try
                {
                    SubscribeObservable(changingObs, evt => this.OnChanging(reactiveObject, evt), disposables);
                }
                catch (Exception ex)
                {
                    this.logger.LogException(ex);
                }
            }

            if (changedObs != null)
            {
                try
                {
                    SubscribeObservable(changedObs, evt => this.OnChanged(reactiveObject, evt), disposables);
                }
                catch (Exception ex)
                {
                    this.logger.LogException(ex);
                }
            }

            // Also fall back to INotifyPropertyChanging/Changed if object implements them
            if (reactiveObject is INotifyPropertyChanging inpc)
            {
                PropertyChangingEventHandler ph = (s, e) =>
                {
                    if (this.service.IsApplying)
                    {
                        return;
                    }

                    var prop = s.GetType().GetProperty(e.PropertyName, BindingFlags.Public | BindingFlags.Instance);
                    if (prop == null)
                    {
                        return;
                    }

                    try
                    {
                        if (!_valueCache.TryGetValue(s, out var cache))
                        {
                            cache = new Dictionary<string, object>();
                            _valueCache.Add(s, cache);
                        }

                        cache[e.PropertyName] = prop.GetValue(s);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogException(ex);
                        if (_valueCache.TryGetValue(s, out var cache2))
                        {
                            cache2[e.PropertyName] = null;
                        }
                    }
                };

                inpc.PropertyChanging += ph;
                disposables.Add(new CoreUnsubscriber(() => inpc.PropertyChanging -= ph));
            }

            if (reactiveObject is INotifyPropertyChanged ipc)
            {
                PropertyChangedEventHandler pc = (s, e) =>
                {
                    if (this.service.IsApplying)
                    {
                        return;
                    }

                    var prop = s.GetType().GetProperty(e.PropertyName, BindingFlags.Public | BindingFlags.Instance);
                    if (prop == null)
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
                        this.logger.LogException(ex);
                        newVal = null;
                    }

                    var setter = ReflectionHelpers.CreateSetter(s.GetType(), prop, this.logger);
                    if (setter == null)
                    {
                        return;
                    }

                    var action = ActionFactory.CreatePropertyChangeAction(s, prop, setter, oldVal, newVal, $"{s.GetType().Name}.{prop.Name} changed", this.logger);
                    if (action != null)
                    {
                        this.service.Push(action);
                    }

                    try
                    {
                        if (!_valueCache.TryGetValue(s, out var cache2))
                        {
                            cache2 = new Dictionary<string, object>();
                            _valueCache.Add(s, cache2);
                        }

                        cache2[e.PropertyName] = newVal;
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogException(ex);
                    }
                };

                ipc.PropertyChanged += pc;
                disposables.Add(new CoreUnsubscriber(() => ipc.PropertyChanged -= pc));
            }

            // final assign already added above
        }

        /// <summary>
        /// Unregister a previously registered object and dispose its subscriptions.
        /// </summary>
        /// <param name="reactiveObject">Registered object to unregister.</param>
        public void Unregister(object reactiveObject)
        {
            if (reactiveObject == null)
            {
                return;
            }

            if (_registrations.TryGetValue(reactiveObject, out var list))
            {
                foreach (var d in list)
                {
                    try
                    {
                        d.Dispose();
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogException(ex);
                    }
                }

                _registrations.Remove(reactiveObject);
            }

            try
            {
                _valueCache.Remove(reactiveObject);
            }
            catch
            {
                // ignore
            }
        }

        /// <summary>
        /// Handle a Reactive 'Changing' notification by recording the old value in the cache.
        /// </summary>
        /// <param name="sender">Source object that raised the notification.</param>
        /// <param name="evt">Event payload containing property information.</param>
        private void OnChanging(object sender, object evt)
        {
            if (sender == null || evt == null)
            {
                return;
            }

            if (this.service.IsApplying)
            {
                return;
            }

            string propName = TryGetPropertyName(evt);
            if (propName == null)
            {
                return;
            }

            var prop = sender.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            if (prop == null)
            {
                return;
            }

            try
            {
                if (!_valueCache.TryGetValue(sender, out var cache))
                {
                    cache = new Dictionary<string, object>();
                    _valueCache.Add(sender, cache);
                }

                cache[propName] = prop.GetValue(sender);
            }
            catch (Exception ex)
            {
                this.logger.LogException(ex);
                if (_valueCache.TryGetValue(sender, out var cache2))
                {
                    cache2[propName] = null;
                }
            }
        }

        /// <summary>
        /// Handle a Reactive 'Changed' notification by creating an undo action for the property change.
        /// </summary>
        /// <param name="sender">Source object that raised the notification.</param>
        /// <param name="evt">Event payload containing property information.</param>
        private void OnChanged(object sender, object evt)
        {
            if (sender == null || evt == null)
            {
                return;
            }

            if (this.service.IsApplying)
            {
                return;
            }

            string propName = TryGetPropertyName(evt);
            if (propName == null)
            {
                return;
            }

            var prop = sender.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            if (prop == null)
            {
                return;
            }

            object oldVal = null;
            if (_valueCache.TryGetValue(sender, out var cache) && cache.TryGetValue(propName, out var o))
            {
                oldVal = o;
            }

            object newVal = null;
            try
            {
                newVal = prop.GetValue(sender);
            }
            catch (Exception ex)
            {
                this.logger.LogException(ex);
                newVal = null;
            }

            var setter = ReflectionHelpers.CreateSetter(sender.GetType(), prop, this.logger);
            if (setter == null)
            {
                return;
            }

            var action = ActionFactory.CreatePropertyChangeAction(sender, prop, setter, oldVal, newVal, $"{sender.GetType().Name}.{prop.Name} changed", this.logger);
            if (action != null)
            {
                this.service.Push(action);
            }

            try
            {
                if (!_valueCache.TryGetValue(sender, out var cache2))
                {
                    cache2 = new Dictionary<string, object>();
                    _valueCache.Add(sender, cache2);
                }

                cache2[propName] = newVal;
            }
            catch (Exception ex)
            {
                this.logger.LogException(ex);
            }
        }
    }
}
