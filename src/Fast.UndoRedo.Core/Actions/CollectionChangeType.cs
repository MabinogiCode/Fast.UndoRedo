namespace Fast.UndoRedo.Core
{
    /// <summary>
    /// Enumerates types of collection changes handled by the undo/redo system.
    /// </summary>
    public enum CollectionChangeType
    {
        Add,
        Remove,
        Replace,
        Move,
        Clear,
    }
}
