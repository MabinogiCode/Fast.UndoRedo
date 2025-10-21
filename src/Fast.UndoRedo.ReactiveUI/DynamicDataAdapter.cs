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

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicDataAdapter"/> class.
        /// </summary>
        /// <param name="service">The undo/redo service used to manage collection subscriptions.</param>
        public DynamicDataAdapter(UndoRedoService service)
            : this(service, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicDataAdapter"/> class with an optional logger.
        /// </summary>
        /// <param name="service">The undo/redo service used to manage collection subscriptions.</param>
        /// <param name="logger">Optional logger used for diagnostic messages.</param>
        public DynamicDataAdapter(UndoRedoService service, ICoreLogger logger)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? new DebugCoreLogger();
        }

        /// <summary>
        /// Register a collection instance (ObservableCollection, ExtendedObservableCollection, or any INotifyCollectionChanged) with the UndoRedo service.
        /// </summary>
        /// <param name="collection">The collection instance to attach to the undo/redo service.</param>
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
