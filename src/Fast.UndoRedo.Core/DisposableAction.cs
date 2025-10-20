using System;

namespace Fast.UndoRedo.Core
{
    /// <summary>
    /// Disposable wrapper that invokes an action when disposed.
    /// Used to remove event handlers and other cleanup actions.
    /// </summary>
    internal sealed class DisposableAction : IDisposable
    {
        private readonly Action _dispose;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableAction"/> class.
        /// </summary>
        /// <param name="dispose">Action to invoke on dispose.</param>
        public DisposableAction(Action dispose)
        {
            _dispose = dispose ?? throw new ArgumentNullException(nameof(dispose));
        }

        /// <summary>
        /// Dispose and invoke the wrapped action once.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                _dispose();
            }
            catch
            {
            }
        }
    }
}
