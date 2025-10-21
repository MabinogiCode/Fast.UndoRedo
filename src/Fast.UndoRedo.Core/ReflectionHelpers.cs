using System;
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
        /// <summary>
        /// Creates a setter action for a property.
        /// </summary>
        /// <param name="targetType">The type of the target object.</param>
        /// <param name="prop">The property information.</param>
        /// <returns>The compiled setter action, or null if creation failed.</returns>
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
        /// <returns>The compiled setter action, or null if creation failed.</returns>
        public static object CreateSetter(Type targetType, PropertyInfo prop, ICoreLogger logger)
        {
            try
            {
                var setterMethod = prop.GetSetMethod(true);
                if (setterMethod == null)
                {
                    return null;
                }

                var actionType = typeof(Action<,>).MakeGenericType(targetType, prop.PropertyType);

                var targetParam = Expression.Parameter(targetType, "target");
                var valueParam = Expression.Parameter(prop.PropertyType, "value");
                var call = Expression.Call(targetParam, setterMethod, valueParam);
                var lambda = Expression.Lambda(actionType, call, targetParam, valueParam);
                return lambda.Compile();
            }
            catch (Exception ex)
            {
                logger?.LogException(ex);
                return null;
            }
        }
    }
}
