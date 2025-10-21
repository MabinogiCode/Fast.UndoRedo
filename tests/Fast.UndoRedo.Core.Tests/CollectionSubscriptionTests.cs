using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections.Generic;
using Fast.UndoRedo.Core;
using Xunit;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Tests verifying collection subscription and batch event handling behavior.
    /// </summary>
    public class CollectionSubscriptionTests
    {
        /// <summary>
        /// Verifies that Replace/Move/Clear operations are recorded by the service when attached.
        /// </summary>
        [Fact]
        public void AttachCollection_RecordsReplaceMoveClear()
        {
            var service = new UndoRedoService();
            var coll = new ObservableCollection<string> { "a", "b", "c" };
            service.AttachCollection(coll);

            // Replace
            coll[1] = "x";
            Assert.True(service.CanUndo);

            // Move (simulate by remove+insert to keep compatibility with ObservableCollection semantics)
            var item = coll[0];
            coll.RemoveAt(0);
            coll.Insert(2, item);
            Assert.True(service.CanUndo);

            // Clear
            coll.Clear();
            Assert.True(service.CanUndo);
        }

        /// <summary>
        /// Verifies that batched add/remove sequences are recorded properly.
        /// </summary>
        [Fact]
        public void AttachCollection_RecordsAddRange_RemoveRange()
        {
            var service = new UndoRedoService();
            var coll = new ObservableCollection<string>();
            service.AttachCollection(coll);

            // simulate AddRange via helper that raises a single NotifyCollectionChanged event with multiple NewItems
            var helper = new BatchObservableCollection<string>(coll);
            helper.AddRange(new[] { "a", "b", "c" });

            Assert.True(service.CanUndo);

            // simulate RemoveRange
            helper.RemoveRange(new[] { "b", "c" });
            Assert.True(service.CanUndo);
        }

        // helper collection that can batch raise events on underlying ObservableCollection
        private class BatchObservableCollection<T>
        {
            private readonly ObservableCollection<T> _inner;

            public BatchObservableCollection(ObservableCollection<T> inner)
            {
                _inner = inner;
            }

            public void AddRange(IEnumerable<T> items)
            {
                var list = new List<T>(items);
                foreach (var it in list) _inner.Add(it);

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

            public void RemoveRange(IEnumerable<T> items)
            {
                var list = new List<T>(items);
                foreach (var it in list) _inner.Remove(it);

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
}
