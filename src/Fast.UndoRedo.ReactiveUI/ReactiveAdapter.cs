using Fast.UndoRedo.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Fast.UndoRedo.Core.Logging;

namespace Fast.UndoRedo.ReactiveUI
{
    /// <summary>
    /// Adapter that integrates Reactive-style observable property change events with the UndoRedo core.
    /// It supports subscribing to ReactiveUI-style Changing/Changed observables as well as INotifyPropertyChanged/Changing.
    /// </summary>
    public class ReactiveAdapter
    {
        private readonly UndoRedoService service;
        private readonly ICoreLogger logger;
        private readonly Dictionary<object, List<IDisposable>> registrations = new Dictionary<object, List<IDisposable>>();
        private readonly Dictionary<object, Dictionary<string, object>> valueCache = new Dictionary<object, Dictionary<string, object>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ReactiveAdapter"/>.
        /// </summary>
        /// <param name="service">The undo/redo service used to push recorded actions.</param>
        public ReactiveAdapter(UndoRedoService service)
            : this(service, null)
        {
        }

        public ReactiveAdapter(UndoRedoService service, ICoreLogger logger)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            this.logger = logger ?? new DebugCoreLogger();
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

            if (this.registrations.ContainsKey(reactiveObject))
            {
                return;
            }

            var disposables = new List<IDisposable>();
            this.registrations[reactiveObject] = disposables;

            // prepare value cache
            var propCache = new Dictionary<string, object>();
            this.valueCache[reactiveObject] = propCache;

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
                        this.valueCache[s][e.PropertyName] = prop.GetValue(s);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogException(ex);
                        this.valueCache[s][e.PropertyName] = null;
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
                    if (this.valueCache.TryGetValue(s, out var cache) && cache.TryGetValue(e.PropertyName, out var o))
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

                    var action = ActionFactory.CreatePropertyChangeAction(s, prop, setter, oldVal, newVal, (string)$"{s.GetType().Name}.{prop.Name} changed", this.logger);
                    if (action != null)
                    {
                        this.service.Push(action);
                    }

                    if (this.valueCache.TryGetValue(s, out var cache2))
                    {
                        cache2[e.PropertyName] = newVal;
                    }
                };

                ipc.PropertyChanged += pc;
                disposables.Add(new CoreUnsubscriber(() => ipc.PropertyChanged -= pc));
            }

            this.registrations[reactiveObject] = disposables;
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

            if (this.registrations.TryGetValue(reactiveObject, out var list))
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

                this.registrations.Remove(reactiveObject);
            }

            if (this.valueCache.ContainsKey(reactiveObject))
            {
                this.valueCache.Remove(reactiveObject);
            }
        }

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
                this.valueCache[sender][propName] = prop.GetValue(sender);
            }
            catch (Exception ex)
            {
                this.logger.LogException(ex);
                this.valueCache[sender][propName] = null;
            }
        }

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
            if (this.valueCache.TryGetValue(sender, out var cache) && cache.TryGetValue(propName, out var o))
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

            var action = ActionFactory.CreatePropertyChangeAction(sender, prop, setter, oldVal, newVal, (string)$"{sender.GetType().Name}.{prop.Name} changed", this.logger);
            if (action != null)
            {
                this.service.Push(action);
            }

            if (this.valueCache.TryGetValue(sender, out var cache2))
            {
                cache2[propName] = newVal;
            }
        }

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
    }
}
