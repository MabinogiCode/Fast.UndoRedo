namespace Fast.UndoRedo.Core
{
    using System;

    /// <summary>
    /// Represents an undoable property change action.
    /// </summary>
    /// <typeparam name="TTarget">The type of the target object.</typeparam>
    /// <typeparam name="TValue">The type of the property value.</typeparam>
    public class PropertyChangeAction<TTarget, TValue> : IUndoableAction
    {
        private readonly TTarget target;
        private readonly Action<TTarget, TValue> setter;
        private readonly TValue oldValue;
        private readonly TValue newValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyChangeAction{TTarget, TValue}"/> class.
        /// </summary>
        /// <param name="target">The target object whose property is being changed.</param>
        /// <param name="setter">The action to set the property value.</param>
        /// <param name="oldValue">The old value of the property.</param>
        /// <param name="newValue">The new value of the property.</param>
        /// <param name="description">The description of the action.</param>
        public PropertyChangeAction(TTarget target, Action<TTarget, TValue> setter, TValue oldValue, TValue newValue, string description = null)
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            this.setter = setter ?? throw new ArgumentNullException(nameof(setter));
            this.oldValue = oldValue;
            this.newValue = newValue;
            this.Description = description ?? $"Property change on {typeof(TTarget).Name}";
        }

        /// <summary>
        /// Gets the description of the action.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Undoes the property change.
        /// </summary>
        public void Undo() => this.setter(this.target, this.oldValue);

        /// <summary>
        /// Redoes the property change.
        /// </summary>
        public void Redo() => this.setter(this.target, this.newValue);
    }
}
