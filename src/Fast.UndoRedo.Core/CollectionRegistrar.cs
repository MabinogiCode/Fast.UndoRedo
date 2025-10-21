using System;
using System.Collections.Generic;
using Fast.UndoRedo.Core.Logging;

namespace Fast.UndoRedo.Core
{
    /// <summary>
    /// Provides methods to register collections for undo/redo tracking.
    /// </summary>
    internal static class CollectionRegistrar
    {
        /// <summary>
        /// Registers a collection instance with the UndoRedoService by delegating to the service's internal attachment method.
        /// </summary>
        /// <param name="collectionInstance">The collection instance to register for undo/redo tracking.</param>
        /// <param name="service">The UndoRedoService instance.</param>
        /// <param name="snapshots">The snapshots dictionary for tracking changes.</param>
        /// <param name="logger">The logger for error reporting.</param>
        /// <returns>The disposable subscription, or null if registration failed.</returns>
        public static IDisposable RegisterCollection(object collectionInstance, UndoRedoService service, Dictionary<object, List<object>> snapshots, ICoreLogger logger)
        {
            if (collectionInstance == null || service == null)
            {
                return null;
            }

            // Delegate to the centralized subscription logic in UndoRedoService
            return service.AttachCollectionInternal(collectionInstance, snapshots);
        }
    }
}
