using System;

namespace Fast.UndoRedo.Core
{
    /// <summary>
    /// Attribute to mark properties or classes that should be ignored by the undo/redo system.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class FastUndoIgnoreAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FastUndoIgnoreAttribute"/> class.
        /// </summary>
        public FastUndoIgnoreAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FastUndoIgnoreAttribute"/> class with a reason.
        /// </summary>
        /// <param name="reason">The reason for ignoring this member.</param>
        public FastUndoIgnoreAttribute(string reason)
        {
            Reason = reason;
        }

        /// <summary>
        /// Gets or sets the reason why this member should be ignored by undo/redo tracking.
        /// This is useful for documentation and debugging purposes.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Gets or sets whether to ignore this property only during recursive registration.
        /// If true, the property will be ignored when registering nested objects but can still
        /// be tracked if explicitly attached to the service.
        /// </summary>
        public bool OnlyIgnoreInRecursiveRegistration { get; set; }
    }
}
