using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Fast.UndoRedo.Core.Logging;

namespace Fast.UndoRedo.Core
{
    /// <summary>
    /// Provides helper methods for reflection operations used in the undo/redo system.
    /// </summary>
    public static class ReflectionHelpers
    {
        private static readonly ConcurrentDictionary<string, Delegate> _setterCache = new ConcurrentDictionary<string, Delegate>();
        private static readonly ConcurrentDictionary<string, Delegate> _getterCache = new ConcurrentDictionary<string, Delegate>();
        private static readonly ConcurrentDictionary<string, Action<object, object>> _objectSetterCache = new ConcurrentDictionary<string, Action<object, object>>();
        private static readonly ConcurrentDictionary<string, Func<object, object>> _objectGetterCache = new ConcurrentDictionary<string, Func<object, object>>();
        private static readonly ConcurrentDictionary<string, PropertyInfo> _propertyCache = new ConcurrentDictionary<string, PropertyInfo>();
        private static readonly ConcurrentDictionary<string, PropertyInfo[]> _propertiesCache = new ConcurrentDictionary<string, PropertyInfo[]>();

        /// <summary>
        /// Gets cached public instance properties for a given type.
        /// </summary>
        /// <param name="type">The type to get properties for.</param>
        /// <returns>Array of <see cref="PropertyInfo"/>.</returns>
        public static PropertyInfo[] GetPublicInstanceProperties(Type type)
        {
            var key = type.FullName + ":props";
            return _propertiesCache.GetOrAdd(key, _ => type.GetProperties(BindingFlags.Public | BindingFlags.Instance));
        }

        /// <summary>
        /// Gets a cached PropertyInfo for the given type and property name.
        /// </summary>
        /// <param name="type">The type declaring the property.</param>
        /// <param name="name">The property name.</param>
        /// <returns>The <see cref="PropertyInfo"/> or null if not found.</returns>
        public static PropertyInfo GetProperty(Type type, string name)
        {
            var key = type.FullName + ":prop:" + name;
            return _propertyCache.GetOrAdd(key, _ => type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic));
        }

        /// <summary>
        /// Creates a setter action for a property.
        /// </summary>
        /// <param name="targetType">The type of the target object.</param>
        /// <param name="prop">The property information.</param>
        /// <returns>The compiled setter action (boxed as object), or null if creation failed.</returns>
        public static object CreateSetter(Type targetType, PropertyInfo prop)
        {
            return CreateSetter(targetType, prop, null);
        }

        /// <summary>
        /// Creates a setter action for a property.
        /// </summary>
        /// <param name="targetType">The type of the target object.</param>
        /// <param name="prop">The property information.</param>
        /// <param name="logger">The logger for error reporting.</param>
        /// <returns>The compiled setter action (boxed as object), or null if creation failed.</returns>
        public static object CreateSetter(Type targetType, PropertyInfo prop, ICoreLogger logger)
        {
            try
            {
                var setterMethod = prop.GetSetMethod(true);
                if (setterMethod == null)
                {
                    return null;
                }

                var key = targetType.FullName + "." + prop.Name + ":set";
                if (_setterCache.TryGetValue(key, out var existing))
                {
                    return existing;
                }

                var actionType = typeof(Action<,>).MakeGenericType(targetType, prop.PropertyType);

                var targetParam = Expression.Parameter(targetType, "target");
                var valueParam = Expression.Parameter(prop.PropertyType, "value");
                var call = Expression.Call(targetParam, setterMethod, valueParam);
                var lambda = Expression.Lambda(actionType, call, targetParam, valueParam);
                var compiled = lambda.Compile();

                _setterCache.TryAdd(key, compiled);

                // also create object-based wrapper to avoid DynamicInvoke
                CreateObjectSetter(targetType, prop, logger);

                return compiled;
            }
            catch (Exception ex)
            {
                logger?.LogException(ex);
                return null;
            }
        }

        /// <summary>
        /// Creates a getter function for a property and caches it.
        /// </summary>
        /// <param name="targetType">The type that declares the property.</param>
        /// <param name="prop">The property info.</param>
        /// <param name="logger">Optional logger for error reporting.</param>
        /// <returns>A compiled Func&lt;TTarget, TProp&gt; boxed as object, or null if getter not available.</returns>
        public static object CreateGetter(Type targetType, PropertyInfo prop, ICoreLogger logger = null)
        {
            try
            {
                var getter = prop.GetGetMethod(true);
                if (getter == null)
                {
                    return null;
                }

                var key = targetType.FullName + "." + prop.Name + ":get";
                if (_getterCache.TryGetValue(key, out var existing))
                {
                    return existing;
                }

                var funcType = typeof(Func<,>).MakeGenericType(targetType, prop.PropertyType);
                var targetParam = Expression.Parameter(targetType, "target");
                var call = Expression.Call(targetParam, getter);
                var lambda = Expression.Lambda(funcType, call, targetParam);
                var compiled = lambda.Compile();
                _getterCache.TryAdd(key, compiled);

                // also create object-based wrapper
                CreateObjectGetter(targetType, prop, logger);

                return compiled;
            }
            catch (Exception ex)
            {
                logger?.LogException(ex);
                return null;
            }
        }

        /// <summary>
        /// Creates a wrapper setter that accepts object parameters to avoid DynamicInvoke allocations.
        /// </summary>
        /// <param name="targetType">The target type that declares the property.</param>
        /// <param name="prop">The property information.</param>
        /// <param name="logger">Optional logger for error reporting.</param>
        /// <returns>A <c>Action&lt;object, object&gt;</c> that sets the property on the target instance, or null if not available.</returns>
        public static Action<object, object> CreateObjectSetter(Type targetType, PropertyInfo prop, ICoreLogger logger = null)
        {
            try
            {
                var key = targetType.FullName + "." + prop.Name + ":set:obj";
                if (_objectSetterCache.TryGetValue(key, out var existing))
                {
                    return existing;
                }

                var targetParam = Expression.Parameter(typeof(object), "target");
                var valueParam = Expression.Parameter(typeof(object), "value");

                var targetCast = Expression.Convert(targetParam, targetType);
                var valueCast = Expression.Convert(valueParam, prop.PropertyType);

                var setterMethod = prop.GetSetMethod(true);
                if (setterMethod == null)
                {
                    return null;
                }

                var call = Expression.Call(targetCast, setterMethod, valueCast);
                var lambda = Expression.Lambda<Action<object, object>>(call, targetParam, valueParam);
                var compiled = lambda.Compile();
                _objectSetterCache.TryAdd(key, compiled);
                return compiled;
            }
            catch (Exception ex)
            {
                logger?.LogException(ex);
                return null;
            }
        }

        /// <summary>
        /// Creates a wrapper getter that returns object to avoid DynamicInvoke allocations.
        /// </summary>
        /// <param name="targetType">The target type that declares the property.</param>
        /// <param name="prop">The property information.</param>
        /// <param name="logger">Optional logger for error reporting.</param>
        /// <returns>A <c>Func&lt;object, object&gt;</c> that returns the property value boxed as object, or null if not available.</returns>
        public static Func<object, object> CreateObjectGetter(Type targetType, PropertyInfo prop, ICoreLogger logger = null)
        {
            try
            {
                var key = targetType.FullName + "." + prop.Name + ":get:obj";
                if (_objectGetterCache.TryGetValue(key, out var existing))
                {
                    return existing;
                }

                var targetParam = Expression.Parameter(typeof(object), "target");
                var targetCast = Expression.Convert(targetParam, targetType);

                var getterMethod = prop.GetGetMethod(true);
                if (getterMethod == null)
                {
                    return null;
                }

                var call = Expression.Call(targetCast, getterMethod);
                var castResult = Expression.Convert(call, typeof(object));
                var lambda = Expression.Lambda<Func<object, object>>(castResult, targetParam);
                var compiled = lambda.Compile();
                _objectGetterCache.TryAdd(key, compiled);
                return compiled;
            }
            catch (Exception ex)
            {
                logger?.LogException(ex);
                return null;
            }
        }
    }
}
