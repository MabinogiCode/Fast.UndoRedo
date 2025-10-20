namespace Fast.UndoRedo.Core
{
    /// <summary>
    /// Represents an action that can be undone and redone.
    /// Implementations encapsulate the logic required to undo and redo a change.
    /// </summary>
    public interface IUndoableAction
    {
        /// <summary>
        /// Reverts the action.
        /// </summary>
        void Undo();

        /// <summary>
        /// Reapplies the action after an undo.
        /// </summary>
        void Redo();

        /// <summary>
        /// A short human-readable description of the action.
        /// </summary>
        string Description { get; }
    }
}
