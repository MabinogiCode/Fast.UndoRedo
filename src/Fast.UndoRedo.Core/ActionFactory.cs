using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Fast.UndoRedo.Core.Logging;

namespace Fast.UndoRedo.Core
{
    /// <summary>
    /// Provides factory methods for creating undo/redo actions.
    /// </summary>
    public static class ActionFactory
    {
        private static readonly ConcurrentDictionary<string, Func<object, object, object, object, string, IUndoableAction>> _propCtorCache = new ConcurrentDictionary<string, Func<object, object, object, object, string, IUndoableAction>>();
        private static readonly ConcurrentDictionary<string, Func<object, CollectionChangeType, object, object, int, int, IEnumerable<object>, string, IUndoableAction>> _collectionCtorCache = new ConcurrentDictionary<string, Func<object, CollectionChangeType, object, object, int, int, IEnumerable<object>, string, IUndoableAction>>();

        /// <summary>
        /// Creates an undoable action for a property change.
        /// </summary>
        /// <param name="target">The target object whose property is changing.</param>
        /// <param name="prop">The property information.</param>
        /// <param name="setterDelegate">The delegate to set the property value.</param>
        /// <param name="oldValue">The old value of the property.</param>
        /// <param name="newValue">The new value of the property.</param>
        /// <param name="description">A description of the action.</param>
        /// <param name="logger">The logger for error reporting.</param>
        /// <returns>The created undoable action, or null if creation failed.</returns>
        public static IUndoableAction CreatePropertyChangeAction(object target, PropertyInfo prop, object setterDelegate, object oldValue, object newValue, string description, ICoreLogger logger = null)
        {
            if (target == null || prop == null)
            {
                return null;
            }

            var key = target.GetType().FullName + "|" + prop.PropertyType.FullName;
            var ctor = _propCtorCache.GetOrAdd(key, k => BuildPropertyCtor(target.GetType(), prop.PropertyType));

            try
            {
                return ctor(target, setterDelegate, oldValue, newValue, description ?? string.Empty);
            }
            catch (Exception ex)
            {
                logger?.LogException(ex);
                return null;
            }
        }

        /// <summary>
        /// Creates an undoable action for a collection change.
        /// </summary>
        /// <param name="collectionInstance">The collection instance.</param>
        /// <param name="elementType">The type of elements in the collection.</param>
        /// <param name="changeType">The type of collection change.</param>
        /// <param name="itemObj">The item involved in the change.</param>
        /// <param name="oldItemObj">The old item for replace operations.</param>
        /// <param name="index">The index of the change.</param>
        /// <param name="toIndex">The target index for move operations.</param>
        /// <param name="clearedItems">The items cleared in a clear operation.</param>
        /// <param name="description">A description of the action.</param>
        /// <param name="logger">The logger for error reporting.</param>
        /// <returns>The created undoable action, or null if creation failed.</returns>
        public static IUndoableAction CreateCollectionChangeAction(object collectionInstance, Type elementType, CollectionChangeType changeType, object itemObj, object oldItemObj, int index, int toIndex, IEnumerable<object> clearedItems, string description, ICoreLogger logger = null)
        {
            if (collectionInstance == null || elementType == null)
            {
                return null;
            }

            var key = collectionInstance.GetType().FullName + "|" + elementType.FullName;
            var ctor = _collectionCtorCache.GetOrAdd(key, k => BuildCollectionCtor(collectionInstance.GetType(), elementType));

            try
            {
                return ctor(collectionInstance, changeType, itemObj, oldItemObj, index, toIndex, clearedItems, description ?? string.Empty);
            }
            catch (Exception ex)
            {
                logger?.LogException(ex);

                // fallback: try to construct concrete CollectionChangeAction<T> via Activator
                try
                {
                    var actionType = typeof(CollectionChangeAction<>).MakeGenericType(elementType);

                    // convert item/old/cleared
                    object convertedItem = null;
                    object convertedOld = null;
                    object convertedCleared = null;

                    if (itemObj != null)
                    {
                        convertedItem = ConvertTo(itemObj, elementType);
                    }

                    if (oldItemObj != null)
                    {
                        convertedOld = ConvertTo(oldItemObj, elementType);
                    }

                    if (clearedItems != null)
                    {
                        var listType = typeof(List<>).MakeGenericType(elementType);
                        var list = (System.Collections.IList)Activator.CreateInstance(listType);
                        foreach (var it in clearedItems)
                        {
                            list.Add(ConvertTo(it, elementType));
                        }

                        convertedCleared = list;
                    }

                    var ctorArgs = new object[] { collectionInstance, changeType, convertedItem, index, convertedOld, toIndex, convertedCleared, description };
                    var created = Activator.CreateInstance(actionType, ctorArgs);
                    return created as IUndoableAction;
                }
                catch (Exception ex2)
                {
                    logger?.LogException(ex2);
                    return null;
                }
            }
        }

        private static Func<object, object, object, object, string, IUndoableAction> BuildPropertyCtor(Type targetType, Type valueType)
        {
            var actionType = typeof(PropertyChangeAction<,>).MakeGenericType(targetType, valueType);

            var expectedCtor = actionType.GetConstructors()
                .FirstOrDefault(ci =>
                {
                    var ps = ci.GetParameters();
                    return ps.Length >= 4 &&
                           ps[0].ParameterType == targetType &&
                           ps[1].ParameterType.IsGenericType && ps[1].ParameterType.GetGenericTypeDefinition() == typeof(Action<,>) &&
                           ps[2].ParameterType == valueType &&
                           ps[3].ParameterType == valueType;
                }) ?? actionType.GetConstructors().FirstOrDefault();

            if (expectedCtor == null)
            {
                throw new InvalidOperationException($"No suitable constructor found for {actionType}");
            }

            var targetParam = Expression.Parameter(typeof(object), "target");
            var setterParam = Expression.Parameter(typeof(object), "setter");
            var oldParam = Expression.Parameter(typeof(object), "oldValue");
            var newParam = Expression.Parameter(typeof(object), "newValue");
            var descParam = Expression.Parameter(typeof(string), "description");

            var targetCast = Expression.Convert(targetParam, targetType);
            var setterCast = Expression.Convert(setterParam, typeof(Action<,>).MakeGenericType(targetType, valueType));
            var oldCast = Expression.Convert(oldParam, valueType);
            var newCast = Expression.Convert(newParam, valueType);

            var newExpr = Expression.New(expectedCtor, targetCast, setterCast, oldCast, newCast, descParam);
            var castToIUndo = Expression.Convert(newExpr, typeof(IUndoableAction));

            var lambda = Expression.Lambda<Func<object, object, object, object, string, IUndoableAction>>(castToIUndo, targetParam, setterParam, oldParam, newParam, descParam);
            return lambda.Compile();
        }

        private static Func<object, CollectionChangeType, object, object, int, int, IEnumerable<object>, string, IUndoableAction> BuildCollectionCtor(Type collectionRuntimeType, Type elementType)
        {
            var actionType = typeof(CollectionChangeAction<>).MakeGenericType(elementType);

            // select a constructor
            var ctors = actionType.GetConstructors();
            ConstructorInfo chosen = null;
            foreach (var c in ctors)
            {
                var ps = c.GetParameters();
                if (ps.Length >= 2)
                {
                    chosen = c;
                    break;
                }
            }

            if (chosen == null)
            {
                throw new InvalidOperationException($"No constructor for {actionType}");
            }

            // parameter expressions
            var collParam = Expression.Parameter(typeof(object), "collection");
            var changeParam = Expression.Parameter(typeof(CollectionChangeType), "change");
            var itemParam = Expression.Parameter(typeof(object), "item");
            var oldParam = Expression.Parameter(typeof(object), "oldItem");
            var indexParam = Expression.Parameter(typeof(int), "index");
            var toIndexParam = Expression.Parameter(typeof(int), "toIndex");
            var clearedParam = Expression.Parameter(typeof(IEnumerable<object>), "clearedItems");
            var descParam = Expression.Parameter(typeof(string), "desc");

            var ctorParams = chosen.GetParameters();
            var args = new List<Expression>();

            int elementCount = 0;

            foreach (var p in ctorParams)
            {
                if (p.ParameterType == typeof(CollectionChangeType) || p.ParameterType == typeof(CollectionChangeType?))
                {
                    args.Add(Expression.Convert(changeParam, p.ParameterType));
                }
                else if (p.ParameterType.IsAssignableFrom(collectionRuntimeType))
                {
                    args.Add(Expression.Convert(collParam, p.ParameterType));
                }
                else if (p.ParameterType.IsGenericType && p.ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    // create typed list from IEnumerable<object>
                    var method = typeof(ActionFactory).GetMethod(nameof(CreateTypedList), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(elementType);
                    var call = Expression.Call(method, clearedParam);
                    args.Add(Expression.Convert(call, p.ParameterType));
                }
                else if (p.ParameterType == elementType)
                {
                    // distinguish between "item" and "oldItem" by occurrence
                    if (elementCount == 0)
                    {
                        args.Add(Expression.Convert(itemParam, elementType));
                    }
                    else
                    {
                        args.Add(Expression.Convert(oldParam, elementType));
                    }

                    elementCount++;
                }
                else if (p.ParameterType == typeof(int))
                {
                    // decide between index and toIndex by arg position: if args already include index, take toIndex
                    if (!args.Any(a => a.Type == typeof(int)))
                    {
                        args.Add(indexParam);
                    }
                    else
                    {
                        args.Add(toIndexParam);
                    }
                }
                else if (p.ParameterType == typeof(string))
                {
                    args.Add(descParam);
                }
                else
                {
                    // unsupported parameter, pass default
                    args.Add(Expression.Default(p.ParameterType));
                }
            }

            var newExpr = Expression.New(chosen, args);
            var castToIUndo = Expression.Convert(newExpr, typeof(IUndoableAction));

            var lambda = Expression.Lambda<Func<object, CollectionChangeType, object, object, int, int, IEnumerable<object>, string, IUndoableAction>>(castToIUndo, collParam, changeParam, itemParam, oldParam, indexParam, toIndexParam, clearedParam, descParam);
            return lambda.Compile();
        }

        // helper to create typed list
        private static IEnumerable<T> CreateTypedList<T>(IEnumerable<object> items)
        {
            if (items == null)
            {
                return null;
            }

            // Try to get count to preallocate
            int count = -1;
            if (items is ICollection<object> collObj)
            {
                count = collObj.Count;
            }
            else if (items is System.Collections.ICollection coll)
            {
                count = coll.Count;
            }

            var list = count >= 0 ? new List<T>(count) : new List<T>();
            var targetType = typeof(T);
            bool isEnum = targetType.IsEnum;

            foreach (var o in items)
            {
                if (o == null)
                {
                    list.Add(default);
                    continue;
                }

                try
                {
                    if (isEnum)
                    {
                        if (targetType.IsInstanceOfType(o))
                        {
                            list.Add((T)o);
                            continue;
                        }

                        if (o is string s)
                        {
                            var parsed = Enum.Parse(targetType, s, true);
                            list.Add((T)parsed);
                            continue;
                        }

                        var underlying = Convert.ChangeType(o, Enum.GetUnderlyingType(targetType));
                        var enumObj = Enum.ToObject(targetType, underlying);
                        list.Add((T)enumObj);
                        continue;
                    }

                    // non-enum: try direct cast then Convert
                    if (targetType.IsInstanceOfType(o))
                    {
                        list.Add((T)o);
                        continue;
                    }

                    list.Add((T)Convert.ChangeType(o, targetType));
                }
                catch
                {
                    // fallback to default
                    try
                    {
                        list.Add((T)o);
                    }
                    catch
                    {
                        list.Add(default);
                    }
                }
            }

            return list;
        }

        // helper to convert arbitrary object to target type with enum support (shared with CollectionSubscription)
        private static object ConvertTo(object value, Type targetType)
        {
            if (value == null)
            {
                return null;
            }

            if (targetType.IsInstanceOfType(value))
            {
                return value;
            }

            if (targetType.IsEnum)
            {
                try
                {
                    if (value is string s)
                    {
                        return Enum.Parse(targetType, s, true);
                    }

                    var underlyingType = Enum.GetUnderlyingType(targetType);
                    var converted = Convert.ChangeType(value, underlyingType);
                    return Enum.ToObject(targetType, converted);
                }
                catch
                {
                    return value;
                }
            }

            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                return value;
            }
        }
    }
}
