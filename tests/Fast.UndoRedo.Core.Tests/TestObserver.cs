using System;
using Fast.UndoRedo.Core;

namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// A simple observer for testing purposes.
    /// </summary>
    internal class TestObserver : IObserver<UndoRedoState>
    {
        private readonly Action<UndoRedoState> _onNext;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestObserver"/> class.
        /// </summary>
        /// <param name="onNext">The action to invoke for OnNext.</param>
        public TestObserver(Action<UndoRedoState> onNext)
        {
            _onNext = onNext;
        }

        /// <summary>
        /// Not used in tests.
        /// </summary>
        public void OnCompleted()
        {
        }

        /// <summary>
        /// Not used in tests.
        /// </summary>
        /// <param name="error">The exception.</param>
        public void OnError(Exception error)
        {
        }

        /// <summary>
        /// Invokes the action provided in the constructor.
        /// </summary>
        /// <param name="value">The state value.</param>
        public void OnNext(UndoRedoState value)
        {
            _onNext(value);
        }
    }
}
