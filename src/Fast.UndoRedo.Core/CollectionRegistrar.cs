using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Fast.UndoRedo.Core.Logging;

namespace Fast.UndoRedo.Core
{
    /// <summary>
    /// Provides methods to register collections for undo/redo tracking.
    /// </summary>
    internal static class CollectionRegistrar
    {
        /// <summary>
        /// Registers a collection instance with the UndoRedoService by creating a CollectionSubscription.
        /// Returns an IDisposable that will unsubscribe the subscription when disposed.
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

            if (collectionInstance is INotifyCollectionChanged)
            {
                try
                {
                    var sub = new CollectionSubscription(collectionInstance, service, snapshots, logger);
                    return sub;
                }
                catch (Exception ex)
                {
                    logger?.LogException(ex);
                    return null;
                }
            }

            return null;
        }
    }
}
