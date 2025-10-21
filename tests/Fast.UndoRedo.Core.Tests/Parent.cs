namespace Fast.UndoRedo.Core.Tests
{
    /// <summary>
    /// A simple parent class for testing nested object registration.
    /// </summary>
    internal class Parent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Parent"/> class.
        /// </summary>
        public Parent()
        {
            Child = new Child();
        }

        /// <summary>
        /// Gets the child object.
        /// </summary>
        public Child Child { get; }
    }
}
