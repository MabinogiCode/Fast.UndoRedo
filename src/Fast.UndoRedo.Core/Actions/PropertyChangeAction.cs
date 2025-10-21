namespace Fast.UndoRedo.Core
{
    using System;

    public class PropertyChangeAction<TTarget, TValue> : IUndoableAction
    {
        private readonly TTarget target;
        private readonly Action<TTarget, TValue> setter;
        private readonly TValue oldValue;
        private readonly TValue newValue;

        public string Description { get; }

        public PropertyChangeAction(TTarget target, Action<TTarget, TValue> setter, TValue oldValue, TValue newValue, string description = null)
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            this.setter = setter ?? throw new ArgumentNullException(nameof(setter));
            this.oldValue = oldValue;
            this.newValue = newValue;
            this.Description = description ?? $"Property change on {typeof(TTarget).Name}";
        }

        public void Undo() => this.setter(this.target, this.oldValue);

        public void Redo() => this.setter(this.target, this.newValue);
    }
}
