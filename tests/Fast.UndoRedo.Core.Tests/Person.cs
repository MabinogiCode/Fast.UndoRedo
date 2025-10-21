namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// A simple Person class for reflection and property change testing.
    /// </summary>
    internal class Person
    {
        /// <summary>
        /// Gets or sets the person's name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets a read-only property.
        /// </summary>
        public string ReadOnly { get; } = "ro";

        /// <summary>
        /// Gets a property with a private setter.
        /// </summary>
        public string PrivateSet { get; private set; } = "priv";
    }
}
