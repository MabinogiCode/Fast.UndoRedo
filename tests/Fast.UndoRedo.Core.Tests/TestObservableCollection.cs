using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// ObservableCollection derivative that exposes a helper to add a range and raise a single Add event.
    /// </summary>
    /// <typeparam name="T">Element type.</typeparam>
    public class TestObservableCollection<T> : ObservableCollection<T>
    {
        /// <summary>
        /// Adds a range of items and raises a single Add event with the whole list.
        /// </summary>
        /// <param name="items">Items to add.</param>
        public void AddRange(IEnumerable<T> items)
        {
            var list = new List<T>(items);
            int start = Count;
            foreach (var it in list)
            {
                Items.Add(it);
            }

            var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (System.Collections.IList)list, start);
            OnCollectionChanged(args);
        }
    }
}
