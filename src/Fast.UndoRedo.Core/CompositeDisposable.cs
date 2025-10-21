using System;
using System.Collections.Generic;

namespace Fast.UndoRedo.Core
{
    /// <summary>
    /// Represents a group of disposable resources that are disposed together.
    /// </summary>
    internal sealed class CompositeDisposable : IDisposable
    {
        private readonly List<IDisposable> _list;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeDisposable"/> class.
        /// </summary>
        /// <param name="list">The list of disposables to manage.</param>
        public CompositeDisposable(List<IDisposable> list)
        {
            _list = list ?? new List<IDisposable>();
        }

        /// <summary>
        /// Disposes all disposables in the group.
        /// </summary>
        public void Dispose()
        {
            foreach (var d in _list)
            {
                try
                {
                    d.Dispose();
                }
                catch
                {
                    // ignore dispose exceptions
                }
            }
        }
    }
}
