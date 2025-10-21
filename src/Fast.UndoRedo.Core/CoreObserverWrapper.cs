using System;

namespace Fast.UndoRedo.Core
{
    public sealed class CoreObserverWrapper<T> : IObserver<T>
    {
        private readonly Action<object> onNext;

        public CoreObserverWrapper(Action<object> onNext)
        {
            this.onNext = onNext ?? throw new ArgumentNullException(nameof(onNext));
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

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
