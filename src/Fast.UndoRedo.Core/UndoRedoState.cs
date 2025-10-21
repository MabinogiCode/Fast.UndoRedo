namespace Fast.UndoRedo.Core
{
    /// <summary>
    /// Represents the current undo/redo state (availability and descriptions).
    /// </summary>
    public class UndoRedoState
    {
        /// <summary>
        /// Gets or sets a value indicating whether an undo is available.
        /// </summary>
        public bool CanUndo { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a redo is available.
        /// </summary>
        public bool CanRedo { get; set; }

        /// <summary>
        /// Gets or sets the description of the top undo action.
        /// </summary>
        public string TopUndoDescription { get; set; }

        /// <summary>
        /// Gets or sets the description of the top redo action.
        /// </summary>
        public string TopRedoDescription { get; set; }
    }
}
