using System;

namespace Fast.UndoRedo.Core
{
    /// <summary>
    /// Simple disposable that invokes an action when disposed.
    /// </summary>
    public sealed class CoreUnsubscriber : IDisposable
    {
        private readonly Action _dispose;

        public CoreUnsubscriber(Action dispose)
        {
            _dispose = dispose ?? throw new ArgumentNullException(nameof(dispose));
        }

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
