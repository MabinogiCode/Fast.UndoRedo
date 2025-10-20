namespace Fast.UndoRedo.Mvvm
{
    using System;
    using Fast.UndoRedo.Core;

    /// <summary>
    /// Lightweight adapter exposing MVVM registration helpers that use the underlying RegistrationTracker.
    /// </summary>
    public class MvvmAdapter
    {
        private readonly UndoRedoService service;
        private readonly RegistrationTracker tracker;

        /// <summary>
        /// Initializes a new instance of the <see cref="MvvmAdapter"/> class.
        /// </summary>
        /// <param name="service">Undo/redo service instance.</param>
        public MvvmAdapter(UndoRedoService service)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            this.tracker = new RegistrationTracker(this.service);
        }

        /// <summary>
        /// Register a view-model or object for change tracking.
        /// </summary>
        /// <param name="vm">Object to register.</param>
        public void Register(object vm) => this.tracker.Register(vm);

        /// <summary>
        /// Unregister a previously registered view-model or object.
        /// </summary>
        /// <param name="vm">Object to unregister.</param>
        public void Unregister(object vm) => this.tracker.Unregister(vm);
    }
}
