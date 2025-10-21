using System.Collections.Specialized;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// Simple test collection that exposes NotifyCollectionChanged events.
    /// </summary>
    public class DummyCollection : INotifyCollectionChanged
    {
        /// <summary>
        /// Occurs when the collection changes.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Raise a Reset collection changed event.
        /// </summary>
        public void RaiseReset()
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
