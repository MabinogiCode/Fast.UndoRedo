using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Fast.UndoRedo.Core.Logging;

namespace Fast.UndoRedo.Core
{
    /// <summary>
    /// Manages subscription to collection change events and records undo/redo actions for collections.
    /// </summary>
    internal sealed class CollectionSubscription : IDisposable
    {
        private readonly object _collectionInstance;
        private readonly INotifyCollectionChanged _incc;
        private readonly UndoRedoService _service;
        private readonly Type _elementType;
        private readonly Type _actionType;
        private readonly Type _enumType;
        private readonly List<object> _snapshot;
        private readonly ICoreLogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionSubscription"/> class.
        /// </summary>
        /// <param name="collectionInstance">The collection instance to subscribe to.</param>
        /// <param name="service">The undo/redo service.</param>
        /// <param name="snapshots">Dictionary of collection snapshots.</param>
        public CollectionSubscription(object collectionInstance, UndoRedoService service, Dictionary<object, List<object>> snapshots)
            : this(collectionInstance, service, snapshots, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionSubscription"/> class with a logger.
        /// </summary>
        /// <param name="collectionInstance">The collection instance to subscribe to.</param>
        /// <param name="service">The undo/redo service.</param>
        /// <param name="snapshots">Dictionary of collection snapshots.</param>
        /// <param name="logger">Logger for error reporting.</param>
        public CollectionSubscription(object collectionInstance, UndoRedoService service, Dictionary<object, List<object>> snapshots, ICoreLogger logger)
        {
            _logger = logger ?? new DebugCoreLogger();
            _collectionInstance = collectionInstance ?? throw new ArgumentNullException(nameof(collectionInstance));
            _incc = collectionInstance as INotifyCollectionChanged ?? throw new ArgumentException("collectionInstance must implement INotifyCollectionChanged");
            _service = service ?? throw new ArgumentNullException(nameof(service));

            // determine element type
            var collectionType = collectionInstance.GetType();
            Type elementType = null;
            if (collectionType.IsGenericType)
            {
                elementType = collectionType.GetGenericArguments()[0];
            }

            List<object> possibleSnap = null;
            if (snapshots != null)
            {
                snapshots.TryGetValue(collectionInstance, out possibleSnap);
            }

            if (elementType == null && possibleSnap != null && possibleSnap.Count > 0)
            {
                elementType = possibleSnap[0]?.GetType();
            }

            _elementType = elementType ?? typeof(object);

            /* construct the generic action type from a concrete generic type's definition to avoid compiler issues */
            var genericDef = typeof(CollectionChangeAction<object>).GetGenericTypeDefinition();
            _actionType = genericDef.MakeGenericType(_elementType);

            // Use the external CollectionChangeType enum
            _enumType = typeof(CollectionChangeType);

            // ensure snapshot entry
            // Always build a snapshot of current items so AttachCollection without an external snapshots dictionary still captures state.
            _snapshot = new List<object>();
            if (collectionInstance is IEnumerable enumerable2)
            {
                foreach (var it in enumerable2)
                {
                    _snapshot.Add(it);
                }
            }

            if (snapshots != null)
            {
                // if an external snapshots dictionary was provided, ensure it references the same list
                if (!snapshots.TryGetValue(collectionInstance, out var existing))
                {
                    snapshots[collectionInstance] = _snapshot;
                }
                else
                {
                    // prefer existing snapshot if present; otherwise keep ours
                    _snapshot = existing ?? _snapshot;
                }
            }

            _incc.CollectionChanged += OnCollectionChanged;
        }

        /// <summary>
        /// Unsubscribes from collection change events.
        /// </summary>
        public void Dispose()
        {
            try
            {
                _incc.CollectionChanged -= OnCollectionChanged;
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        /// <summary>
        /// Helper that creates and pushes an undoable action for a collection change.
        /// Extracted from the event handler to avoid nested/local functions.
        /// </summary>
        private void CreateAndPush(string changeName, object itemObj = null, object oldItemObj = null, int index = -1, int toIndex = -1, IEnumerable<object> clearedItems = null, string desc = null)
        {
            try
            {
                var changeType = (CollectionChangeType)Enum.Parse(_enumType, changeName);

                // Only push when not applying an undo/redo
                if (!_service.IsApplying)
                {
                    // If this is a Clear action, prefer concrete creation using the provided clearedItems (snapshot) to ensure restore semantics.
                    if (changeType == CollectionChangeType.Clear)
                    {
                        try
                        {
                            object convertedCleared = null;
                            if (clearedItems != null)
                            {
                                var listType = typeof(List<>).MakeGenericType(_elementType);
                                var list = (System.Collections.IList)Activator.CreateInstance(listType);
                                foreach (var it in clearedItems)
                                {
                                    list.Add(ConvertTo(it, _elementType));
                                }

                                convertedCleared = list;
                            }

                            var ctorArgs = new object[] { _collectionInstance, changeType, null, -1, null, -1, convertedCleared, desc };
                            var created = Activator.CreateInstance(_actionType, ctorArgs);
                            if (created is IUndoableAction createdAction)
                            {
                                System.Console.WriteLine($"CreateAndPush: pushing Clear action desc={desc} on {_collectionInstance}");
                                _service.Push(createdAction);
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogException(ex);

                            // fall through to factory fallback
                        }
                    }

                    // Try ActionFactory first for other action types which may have more specific constructors.
                    var action = ActionFactory.CreateCollectionChangeAction(_collectionInstance, _elementType, changeType, itemObj, oldItemObj, index, toIndex, clearedItems, desc, _logger);
                    if (action is IUndoableAction ua)
                    {
                        try
                        {
                            System.Console.WriteLine($"CreateAndPush: ActionFactory created action {ua.Description} change={changeType} item={itemObj} index={index}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogException(ex);
                        }

                        _service.Push(ua);
                        return;
                    }

                    // If factory couldn't produce an action, try concrete Activator creation as fallback.
                    try
                    {
                        object convertedItem = ConvertTo(itemObj, _elementType);
                        object convertedOld = ConvertTo(oldItemObj, _elementType);

                        object convertedCleared = null;
                        if (clearedItems != null)
                        {
                            var listType = typeof(List<>).MakeGenericType(_elementType);
                            var list = (System.Collections.IList)Activator.CreateInstance(listType);
                            foreach (var it in clearedItems)
                            {
                                list.Add(ConvertTo(it, _elementType));
                            }

                            convertedCleared = list;
                        }

                        var ctorArgs = new object[] { _collectionInstance, changeType, convertedItem, index, convertedOld, toIndex, convertedCleared, desc };
                        var created = Activator.CreateInstance(_actionType, ctorArgs);
                        if (created is IUndoableAction createdAction)
                        {
                            try
                            {
                                System.Console.WriteLine($"CreateAndPush: Activator created action {createdAction.Description} change={changeType} item={itemObj} index={index}");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogException(ex);
                            }

                            _service.Push(createdAction);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        private object ConvertTo(object value, Type targetType)
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

                    var underlying = Convert.ChangeType(value, Enum.GetUnderlyingType(targetType));
                    return Enum.ToObject(targetType, underlying);
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

        /// <summary>
        /// Handles collection changed events and records undo/redo actions.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            try
            {
                // Do NOT return when applying: we still need to update internal snapshots even when changes originate from Undo/Redo.
                // We will only skip pushing new actions when service.IsApplying is true.
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        if (e.NewItems != null)
                        {
                            int start = e.NewStartingIndex;

                            /* If multiple items added at once, push a single action with the list */
                            if (e.NewItems.Count > 1)
                            {
                                var list = new List<object>();
                                for (int i = 0; i < e.NewItems.Count; i++)
                                {
                                    list.Add(e.NewItems[i]);
                                }

                                CreateAndPush("Add", list, null, start, -1, null, $"Add range ({e.NewItems.Count})");

                                // update snapshot
                                int insertAt = start >= 0 ? start : (_snapshot?.Count ?? 0);
                                foreach (var it in list)
                                {
                                    _snapshot?.Insert(insertAt, it);
                                    insertAt++;
                                }
                            }
                            else
                            {
                                for (int i = 0; i < e.NewItems.Count; i++)
                                {
                                    var newItem = e.NewItems[i];
                                    int idx = start >= 0 ? start + i : -1;
                                    CreateAndPush("Add", newItem, null, idx, -1, null, $"Add {newItem}");

                                    // Always update snapshot so it stays in sync with actual collection
                                    _snapshot?.Insert(idx >= 0 && idx <= _snapshot.Count ? idx : _snapshot.Count, newItem);
                                }
                            }
                        }

                        break;
                    case NotifyCollectionChangedAction.Remove:
                        if (e.OldItems != null)
                        {
                            int start = e.OldStartingIndex;
                            if (e.OldItems.Count > 1)
                            {
                                var list = new List<object>();
                                for (int i = 0; i < e.OldItems.Count; i++)
                                {
                                    list.Add(e.OldItems[i]);
                                }

                                CreateAndPush("Remove", list, null, start, -1, list, $"Remove range ({e.OldItems.Count})");

                                // update snapshot: remove range
                                if (_snapshot != null && start >= 0)
                                {
                                    for (int i = 0; i < list.Count; i++)
                                    {
                                        if (start < _snapshot.Count)
                                        {
                                            _snapshot.RemoveAt(start);
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (var it in list)
                                    {
                                        _snapshot?.Remove(it);
                                    }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < e.OldItems.Count; i++)
                                {
                                    var oldItem = e.OldItems[i];
                                    int idx = start >= 0 ? start + i : -1;
                                    CreateAndPush("Remove", oldItem, null, idx, -1, null, $"Remove {oldItem}");
                                    if (_snapshot != null && idx >= 0 && idx < _snapshot.Count)
                                    {
                                        _snapshot.RemoveAt(idx);
                                    }
                                    else
                                    {
                                        _snapshot?.Remove(oldItem);
                                    }
                                }
                            }
                        }

                        break;
                    case NotifyCollectionChangedAction.Replace:
                        if (e.NewItems != null && e.OldItems != null)
                        {
                            int start = e.NewStartingIndex >= 0 ? e.NewStartingIndex : e.OldStartingIndex;
                            for (int i = 0; i < e.NewItems.Count; i++)
                            {
                                var newItem = e.NewItems[i];
                                var oldItem = e.OldItems[i];
                                int idx = start >= 0 ? start + i : -1;
                                CreateAndPush("Replace", newItem, oldItem, idx, -1, null, $"Replace at {idx}");
                                if (_snapshot != null && idx >= 0 && idx < _snapshot.Count)
                                {
                                    _snapshot[idx] = newItem;
                                }
                            }
                        }

                        break;
                    case NotifyCollectionChangedAction.Move:
                        if (e.OldItems != null)
                        {
                            int oldStart = e.OldStartingIndex;
                            int newStart = e.NewStartingIndex;
                            for (int i = 0; i < e.OldItems.Count; i++)
                            {
                                var item = e.OldItems[i];
                                int from = oldStart >= 0 ? oldStart + i : -1;
                                int to = newStart >= 0 ? newStart + i : -1;
                                CreateAndPush("Move", item, null, from, to, null, $"Move {item} from {from} to {to}");
                                if (_snapshot != null && from >= 0 && from < _snapshot.Count)
                                {
                                    var it = _snapshot[from];
                                    _snapshot.RemoveAt(from);
                                    if (to >= 0 && to <= _snapshot.Count)
                                    {
                                        _snapshot.Insert(to, it);
                                    }
                                    else
                                    {
                                        _snapshot.Add(it);
                                    }
                                }
                            }
                        }

                        break;
                    case NotifyCollectionChangedAction.Reset:
                        if (_snapshot != null)
                        {
                            CreateAndPush("Clear", null, null, -1, -1, _snapshot.ToList(), "Clear");
                            _snapshot.Clear();
                        }
                        else
                        {
                            CreateAndPush("Clear", null, null, -1, -1, null, "Clear");
                        }

                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }
    }
}
