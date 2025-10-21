using System;

namespace Fast.UndoRedo.Core
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class FastUndoIgnoreAttribute : Attribute
    {
    }
}
