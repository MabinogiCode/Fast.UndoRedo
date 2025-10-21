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
            if (snapshots != null)
            {
                if (!snapshots.TryGetValue(collectionInstance, out _snapshot))
                {
                    // build a snapshot of current items if enumerable
                    _snapshot = new List<object>();
                    if (collectionInstance is IEnumerable enumerable)
                    {
                        foreach (var it in enumerable)
                        {
                            _snapshot.Add(it);
                        }
                    }

                    snapshots[collectionInstance] = _snapshot;
                }
                else
                {
                    // if we had a possibleSnap from earlier, use it; otherwise TryGetValue already assigned _snapshot
                    _snapshot = _snapshot ?? possibleSnap ?? new List<object>();
                }
            }
            else
            {
                _snapshot = new List<object>();
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
                    var action = ActionFactory.CreateCollectionChangeAction(_collectionInstance, _elementType, changeType, itemObj, oldItemObj, index, toIndex, clearedItems, desc, _logger);
                    if (action is IUndoableAction ua)
                    {
                        _service.Push(ua);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
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
