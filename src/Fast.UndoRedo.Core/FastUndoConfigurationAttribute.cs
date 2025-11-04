using System;

namespace Fast.UndoRedo.Core
{
    /// <summary>
    /// Options for configuring undo/redo behavior at the class level.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class FastUndoConfigurationAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether to automatically register nested objects found in properties.
        /// Default is true.
        /// </summary>
        public bool AutoRegisterNestedObjects { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to track changes to collection properties.
        /// Default is true.
        /// </summary>
        public bool TrackCollections { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum depth for recursive registration to prevent infinite loops.
        /// Default is 10.
        /// </summary>
        public int MaxRecursionDepth { get; set; } = 10;

        /// <summary>
        /// Gets or sets whether to use aggressive caching for property getters/setters.
        /// This can improve performance but uses more memory.
        /// Default is false.
        /// </summary>
        public bool UseAggressiveCaching { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to ignore properties that throw exceptions during evaluation.
        /// Default is true.
        /// </summary>
        public bool IgnorePropertiesWithExceptions { get; set; } = true;
    }
}
