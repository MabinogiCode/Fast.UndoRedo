using System;
using System.Collections.Specialized;
using Fast.UndoRedo.Core;
using Fast.UndoRedo.Core.Logging;

namespace Fast.UndoRedo.ReactiveUI
{
    /// <summary>
    /// Adapter helper for DynamicData/ExtendedObservableCollection scenarios.
    /// For existing collections that implement INotifyCollectionChanged this will attach the collection to the given service.
    /// DynamicData change sets typically result in NotifyCollectionChanged events with multiple items which are handled by CollectionSubscription.
    /// </summary>
    public sealed class DynamicDataAdapter
    {
        private readonly UndoRedoService _service;
        private readonly ICoreLogger _logger;

        public DynamicDataAdapter(UndoRedoService service)
            : this(service, null)
        {
        }

        public DynamicDataAdapter(UndoRedoService service, ICoreLogger logger)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? new DebugCoreLogger();
        }

        /// <summary>
        /// Register a collection instance (ObservableCollection, ExtendedObservableCollection, or any INotifyCollectionChanged) with the UndoRedo service.
        /// </summary>
        public void RegisterCollection(INotifyCollectionChanged collection)
        {
            if (collection == null)
            {
                return;
            }

            try
            {
                _service.AttachCollection(collection);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }
    }
}
