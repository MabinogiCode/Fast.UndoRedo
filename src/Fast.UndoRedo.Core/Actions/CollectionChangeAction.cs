using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Fast.UndoRedo.Core
{
    /// <summary>
    /// Represents a concrete change action applied to an <see cref="ObservableCollection{T}"/>.
    /// The action can be undone and redone to restore or reapply the change.
    /// </summary>
    /// <typeparam name="T">Type of elements stored in the collection.</typeparam>
    public class CollectionChangeAction<T> : IUndoableAction
    {
        private readonly ObservableCollection<T> _collection;
        private readonly T _item;
        private readonly T _oldItem;
        private readonly List<T> _clearedItems;
        private readonly int _index;
        private readonly int _toIndex;
        private readonly CollectionChangeType _type;

        /// <inheritdoc />
        public string Description { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionChangeAction{T}"/> class.
        /// </summary>
        /// <param name="collection">The target collection where the change occurred.</param>
        /// <param name="type">Type of collection change (Add/Remove/Replace/Move/Clear).</param>
        /// <param name="item">The item involved in the change (new item for Add/Replace).</param>
        /// <param name="index">Index where the change occurred, if applicable.</param>
        /// <param name="oldItem">Previous item for Replace actions.</param>
        /// <param name="toIndex">Target index for Move actions.</param>
        /// <param name="clearedItems">Snapshot of items cleared for Clear action.</param>
        /// <param name="description">Optional human-readable description.</param>
        public CollectionChangeAction(ObservableCollection<T> collection, CollectionChangeType type, T item = default, int index = -1, T oldItem = default, int toIndex = -1, IEnumerable<T> clearedItems = null, string description = null)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _type = type;
            _item = item;
            _oldItem = oldItem;
            _index = index;
            _toIndex = toIndex;
            _clearedItems = clearedItems != null ? new List<T>(clearedItems) : null;
            Description = description ?? $"Collection change: {_type}";
        }

        /// <inheritdoc />
        public void Undo()
        {
            switch (_type)
            {
                case CollectionChangeType.Add:
                    if (_index >= 0 && _index < _collection.Count)
                    {
                        _collection.RemoveAt(_index);
                    }
                    else
                    {
                        _collection.Remove(_item);
                    }

                    break;
                case CollectionChangeType.Remove:
                    if (_index >= 0 && _index <= _collection.Count)
                    {
                        _collection.Insert(_index, _item);
                    }
                    else
                    {
                        _collection.Add(_item);
                    }

                    break;
                case CollectionChangeType.Replace:
                    // replace new item at index with old item
                    if (_index >= 0 && _index < _collection.Count)
                    {
                        _collection[_index] = _oldItem;
                    }

                    break;
                case CollectionChangeType.Move:
                    // move back from _toIndex to _index
                    if (_toIndex >= 0 && _toIndex < _collection.Count && _index >= 0 && _index <= _collection.Count)
                    {
                        var item = _collection[_toIndex];
                        _collection.RemoveAt(_toIndex);
                        _collection.Insert(_index, item);
                    }

                    break;
                case CollectionChangeType.Clear:
                    if (_clearedItems != null)
                    {
                        for (int i = 0; i < _clearedItems.Count; i++)
                        {
                            _collection.Insert(i, _clearedItems[i]);
                        }
                    }

                    break;
                default:
                    break;
            }
        }

        /// <inheritdoc />
        public void Redo()
        {
            switch (_type)
            {
                case CollectionChangeType.Add:
                    if (_index >= 0 && _index <= _collection.Count)
                    {
                        _collection.Insert(_index, _item);
                    }
                    else
                    {
                        _collection.Add(_item);
                    }

                    break;
                case CollectionChangeType.Remove:
                    if (_index >= 0 && _index < _collection.Count)
                    {
                        _collection.RemoveAt(_index);
                    }
                    else
                    {
                        _collection.Remove(_item);
                    }

                    break;
                case CollectionChangeType.Replace:
                    if (_index >= 0 && _index < _collection.Count)
                    {
                        _collection[_index] = _item; // _item is the new value
                    }

                    break;
                case CollectionChangeType.Move:
                    // move from _index to _toIndex
                    if (_index >= 0 && _index < _collection.Count && _toIndex >= 0 && _toIndex <= _collection.Count)
                    {
                        var item = _collection[_index];
                        _collection.RemoveAt(_index);
                        _collection.Insert(_toIndex, item);
                    }

                    break;
                case CollectionChangeType.Clear:
                    _collection.Clear();
                    break;
                default:
                    break;
            }
        }
    }
}
