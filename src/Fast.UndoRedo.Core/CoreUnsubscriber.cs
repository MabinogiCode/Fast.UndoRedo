using System;

namespace Fast.UndoRedo.Core
{
    /// <summary>
    /// Simple disposable that invokes an action when disposed.
    /// </summary>
    public sealed class CoreUnsubscriber : IDisposable
    {
        private readonly Action _dispose;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoreUnsubscriber"/> class.
        /// </summary>
        /// <param name="dispose">The action to invoke when disposed.</param>
        public CoreUnsubscriber(Action dispose)
        {
            _dispose = dispose ?? throw new ArgumentNullException(nameof(dispose));
        }

        /// <summary>
        /// Disposes the instance and invokes the dispose action.
        /// </summary>
        public void Dispose()
        {
            try
            {
                _dispose();
            }
            catch
            {
                // ignore dispose exceptions
            }
        }
    }
}
