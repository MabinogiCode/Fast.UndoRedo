using System;
using Fast.UndoRedo.Core;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// A simple undoable action for testing purposes.
    /// </summary>
    internal class TestAction : IUndoableAction
    {
        private readonly Action _undo;
        private readonly Action _redo;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestAction"/> class.
        /// </summary>
        /// <param name="undo">The undo action.</param>
        /// <param name="redo">The redo action.</param>
        /// <param name="desc">The action description.</param>
        public TestAction(Action undo, Action redo, string desc)
        {
            _undo = undo;
            _redo = redo;
            Description = desc;
        }

        /// <summary>
        /// Gets the action description.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Executes the undo action.
        /// </summary>
        public void Undo() => _undo();

        /// <summary>
        /// Executes the redo action.
        /// </summary>
        public void Redo() => _redo();
    }
}
