using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Minimal collection implementation that supports raising collection changed events with multiple items.
    /// </summary>
    /// <typeparam name="T">Element type.</typeparam>
    public class MultiNotifyCollection<T> : IList<T>, INotifyCollectionChanged
    {
        /// <summary>
        /// Backing list.
        /// </summary>
        private readonly List<T> list = new List<T>();

        /// <inheritdoc />
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <inheritdoc />
        public int Count => list.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public T this[int index] { get => list[index]; set => list[index] = value; }

        /// <summary>
        /// Adds an item and raises a single-item Add event.
        /// </summary>
        /// <param name="item">Item to add.</param>
        public void Add(T item)
        {
            list.Add(item);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, list.Count - 1));
        }

        /// <summary>
        /// Adds a range of items and raises an Add event with the whole list.
        /// </summary>
        /// <param name="items">Items to add.</param>
        public void AddRange(IEnumerable<T> items)
        {
            var added = new List<T>(items);
            int start = list.Count;
            list.AddRange(added);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (IList)added, start));
        }

        /// <summary>
        /// Removes an item and raises a Remove event.
        /// </summary>
        /// <param name="item">Item to remove.</param>
        /// <returns>True if removed; otherwise false.</returns>
        public bool Remove(T item)
        {
            int idx = list.IndexOf(item);
            if (idx < 0)
            {
                return false;
            }

            list.RemoveAt(idx);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, idx));
            return true;
        }

        /// <summary>
        /// Removes a range and raises a Remove event with the removed list.
        /// </summary>
        /// <param name="index">Start index.</param>
        /// <param name="count">Count to remove.</param>
        public void RemoveRange(int index, int count)
        {
            var removed = list.GetRange(index, count);
            list.RemoveRange(index, count);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, (IList)removed, index));
        }

        /// <summary>
        /// Clears the collection and raises Reset.
        /// </summary>
        public void Clear()
        {
            var snapshot = new List<T>(list);
            list.Clear();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <inheritdoc />
        /// <returns>True if the collection contains the item.</returns>
        public bool Contains(T item) => list.Contains(item);

        /// <inheritdoc />
        /// <param name="array">Destination array.</param>
        /// <param name="arrayIndex">Start index in destination.</param>
        public void CopyTo(T[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);

        /// <inheritdoc />
        /// <returns>An enumerator for the collection.</returns>
        public IEnumerator<T> GetEnumerator() => list.GetEnumerator();

        /// <inheritdoc />
        /// <returns>The index of the item or -1 if not found.</returns>
        public int IndexOf(T item) => list.IndexOf(item);

        /// <inheritdoc />
        public void Insert(int index, T item)
        {
            list.Insert(index, item);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        /// <inheritdoc />
        public void RemoveAt(int index)
        {
            var old = list[index];
            list.RemoveAt(index);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, old, index));
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
    }
}
