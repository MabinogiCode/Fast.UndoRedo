using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Helper collection that can batch raise events on an underlying ObservableCollection for testing.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    internal class BatchObservableCollection<T>
    {
        private readonly ObservableCollection<T> _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchObservableCollection{T}"/> class.
        /// </summary>
        /// <param name="inner">The inner collection to wrap.</param>
        public BatchObservableCollection(ObservableCollection<T> inner)
        {
            _inner = inner;
        }

        /// <summary>
        /// Adds a range of items and raises a single collection changed event.
        /// </summary>
        /// <param name="items">The items to add.</param>
        public void AddRange(IEnumerable<T> items)
        {
            var list = new List<T>(items);
            foreach (var it in list)
            {
                _inner.Add(it);
            }

            // raise a Reset event (CollectionSubscription treats Reset as Clear) - to simulate a single multi-add event we use reflection to raise NotifyCollectionChanged with Add action
            var incc = _inner as INotifyCollectionChanged;
            var handlers = typeof(ObservableCollection<T>).GetField("CollectionChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(_inner) as System.Delegate;
            if (handlers != null)
            {
                foreach (NotifyCollectionChangedEventHandler h in handlers.GetInvocationList())
                {
                    h.Invoke(_inner, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<T>(list)));
                }
            }
        }

        /// <summary>
        /// Removes a range of items and raises a single collection changed event.
        /// </summary>
        /// <param name="items">The items to remove.</param>
        public void RemoveRange(IEnumerable<T> items)
        {
            var list = new List<T>(items);
            foreach (var it in list)
            {
                _inner.Remove(it);
            }

            var incc = _inner as INotifyCollectionChanged;
            var handlers = typeof(ObservableCollection<T>).GetField("CollectionChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(_inner) as System.Delegate;
            if (handlers != null)
            {
                foreach (NotifyCollectionChangedEventHandler h in handlers.GetInvocationList())
                {
                    h.Invoke(_inner, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new List<T>(list)));
                }
            }
        }
    }
}
