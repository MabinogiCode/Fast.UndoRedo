using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Fast.UndoRedo.Core.Logging;

namespace Fast.UndoRedo.Core
{
    internal static class CollectionRegistrar
    {
        /// <summary>
        /// Registers a collection instance with the UndoRedoService by creating a CollectionSubscription.
        /// Returns an IDisposable that will unsubscribe the subscription when disposed.
        /// </summary>
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
