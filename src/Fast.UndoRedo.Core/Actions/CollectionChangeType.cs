namespace Fast.UndoRedo.Core
{
    /// <summary>
    /// Enumerates types of collection changes handled by the undo/redo system.
    /// </summary>
    public enum CollectionChangeType
    {
        /// <summary>
        /// Adds an item to the collection.
        /// </summary>
        Add,

        /// <summary>
        /// Removes an item from the collection.
        /// </summary>
        Remove,

        /// <summary>
        /// Replaces an item in the collection.
        /// </summary>
        Replace,

        /// <summary>
        /// Moves an item within the collection.
        /// </summary>
        Move,

        /// <summary>
        /// Clears all items from the collection.
        /// </summary>
        Clear,
    }
}
