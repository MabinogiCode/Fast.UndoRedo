namespace Fast.UndoRedo.Core
{
    /// <summary>
    /// Represents an action that can be undone and redone.
    /// Implementations encapsulate the logic required to undo and redo a change.
    /// </summary>
    public interface IUndoableAction
    {
        /// <summary>
        /// Gets a short human-readable description of the action.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Reverts the action.
        /// </summary>
        void Undo();

        /// <summary>
        /// Reapplies the action after an undo.
        /// </summary>
        void Redo();
    }
}
