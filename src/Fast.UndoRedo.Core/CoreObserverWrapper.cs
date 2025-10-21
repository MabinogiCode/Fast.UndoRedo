using System;

namespace Fast.UndoRedo.Core
{
    /// <summary>
    /// Wraps an observer action for core functionality.
    /// </summary>
    /// <typeparam name="T">The type of the observed value.</typeparam>
    public sealed class CoreObserverWrapper<T> : IObserver<T>
    {
        private readonly Action<object> onNext;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoreObserverWrapper{T}"/> class.
        /// </summary>
        /// <param name="onNext">The action to invoke when a value is observed.</param>
        public CoreObserverWrapper(Action<object> onNext)
        {
            this.onNext = onNext ?? throw new ArgumentNullException(nameof(onNext));
        }

        /// <summary>
        /// Called when the observable sequence completes.
        /// </summary>
        public void OnCompleted()
        {
        }

        /// <summary>
        /// Called when the observable sequence encounters an error.
        /// </summary>
        /// <param name="error">The exception that occurred.</param>
        public void OnError(Exception error)
        {
        }

        /// <summary>
        /// Called when the observable sequence produces a value.
        /// </summary>
        /// <param name="value">The value produced.</param>
        public void OnNext(T value)
        {
            try
            {
                this.onNext(value);
            }
            catch
            {
                // ignore observer exceptions
            }
        }
    }
}
