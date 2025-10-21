using System;

namespace Fast.UndoRedo.Core
{
    /// <summary>
    /// Attribute to mark properties or classes that should be ignored by the undo/redo system.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class FastUndoIgnoreAttribute : Attribute
    {
    }
}
