using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Fast.UndoRedo.Core.Logging;
using System.Linq;
using System.Collections.Generic;

namespace Fast.UndoRedo.Core
{
    public static class ActionFactory
    {
        private static readonly ConcurrentDictionary<string, Func<object, object, object, object, string, IUndoableAction>> _propCtorCache = new ConcurrentDictionary<string, Func<object, object, object, object, string, IUndoableAction>>();
        private static readonly ConcurrentDictionary<string, Func<object, CollectionChangeType, object, object, int, int, IEnumerable<object>, string, IUndoableAction>> _collectionCtorCache = new ConcurrentDictionary<string, Func<object, CollectionChangeType, object, object, int, int, IEnumerable<object>, string, IUndoableAction>>();

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

        // Collection factory
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
                return null;
            }
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

            var list = new List<T>();
            foreach (var o in items)
            {
                list.Add((T)o);
            }

            return list;
        }
    }
}
