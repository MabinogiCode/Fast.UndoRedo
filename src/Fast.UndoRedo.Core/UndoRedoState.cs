namespace Fast.UndoRedo.Core
{
    /// <summary>
    /// Represents the current undo/redo state (availability and descriptions).
    /// </summary>
    public class UndoRedoState
    {
        /// <summary>
        /// Indicates whether an undo is available.
        /// </summary>
        public bool CanUndo { get; set; }

        /// <summary>
        /// Indicates whether a redo is available.
        /// </summary>
        public bool CanRedo { get; set; }

        /// <summary>
        /// Description of the top undo action.
        /// </summary>
        public string TopUndoDescription { get; set; }

        /// <summary>
        /// Description of the top redo action.
        /// </summary>
        public string TopRedoDescription { get; set; }
    }
}
