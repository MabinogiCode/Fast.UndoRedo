using Fast.UndoRedo.Core;
using Fast.UndoRedo.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Fast.UndoRedo.ReactiveUI
{
    /// <summary>
    /// Provides helper methods for ReactiveAdapter.
    /// </summary>
    internal static class ReactiveAdapterHelpers
    {
        /// <summary>
        /// Tries to extract a property name from a Reactive-style event object.
        /// </summary>
        /// <param name="evt">The event object published by the observable.</param>
        /// <param name="logger">The logger for error reporting.</param>
        /// <returns>The property name if found; otherwise null.</returns>
        internal static string TryGetPropertyName(object evt, ICoreLogger logger)
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
                logger.LogException(ex);
            }

            return null;
        }

        /// <summary>
        /// Gets a public property or field value by name from an object.
        /// </summary>
        /// <param name="obj">Object to inspect.</param>
        /// <param name="name">Member name.</param>
        /// <returns> Value. </returns>
        internal static object GetMemberValue(object obj, string name)
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
        /// <param name="logger">The logger for error reporting.</param>
        internal static void SubscribeObservable(object observable, Action<object> onNext, List<IDisposable> disposables, ICoreLogger logger)
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
                logger.LogException(ex);
            }
        }
    }
}
