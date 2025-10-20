using System;
using System.Linq.Expressions;
using System.Reflection;
using Fast.UndoRedo.Core.Logging;

namespace Fast.UndoRedo.Core
{
    public static class ReflectionHelpers
    {
        // Build Action<TTarget, TValue> dynamically using expression trees.
        public static object CreateSetter(Type targetType, PropertyInfo prop)
        {
            return CreateSetter(targetType, prop, null);
        }

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
